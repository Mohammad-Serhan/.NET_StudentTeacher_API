using Auth.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentApi.Services;

[Route("api/[controller]/[action]")]
[ApiController]
[EnableRateLimiting("ModerateApiPolicy")]
public class EncryptionController : ControllerBase
{
    private readonly ISimpleEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;

    public EncryptionController(ISimpleEncryptionService encryptionService, IConfiguration configuration)
    {
        _encryptionService = encryptionService;
        _configuration = configuration;
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult EncryptAppSettings()
    {
        try
        {
            var appSettingsPath = "appsettings.json";

            if (!System.IO.File.Exists(appSettingsPath))
            {
                return NotFound(new { error = "appsettings.json file not found" });
            }

            var json = System.IO.File.ReadAllText(appSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);

            // Fix for Warning 1: Add null checks for config properties
            if (config?.ConnectionStrings?.DefaultConnection == null)
                return BadRequest(new { error = "DefaultConnection not found in appsettings" });

            if (config?.ConnectionStrings?.ODBCConnectionString == null)
                return BadRequest(new { error = "ODBCConnectionString not found in appsettings" });

            if (config?.Jwt?.SecretKey == null)
                return BadRequest(new { error = "Jwt:SecretKey not found in appsettings" });

            config.ConnectionStrings.DefaultConnection = _encryptionService.Encrypt(config.ConnectionStrings.DefaultConnection.ToString());
            config.ConnectionStrings.ODBCConnectionString = _encryptionService.Encrypt(config.ConnectionStrings.ODBCConnectionString.ToString());
            config.Jwt.SecretKey = _encryptionService.Encrypt(config.Jwt.SecretKey.ToString());

            var encryptedJson = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("appsettings.encrypted.json", encryptedJson);
            
            return Ok(new
            {
                message = "AppSettings encrypted successfully!",
                file = "appsettings.encrypted.json"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // --- NEW DECRYPTION TOOL ---
    [HttpPost]
    [AllowAnonymous]
    //[IgnoreAntiforgeryToken]
    public IActionResult DecryptText([FromBody] EncryptRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text))
            {
                return BadRequest(new { error = "Text to decrypt is required" });
            }

            // Attempt to decrypt the provided text
            var decryptedText = _encryptionService.Decrypt(request.Text);

            return Ok(new
            {
                encrypted_input = request.Text,
                decrypted_output = decryptedText,
                message = "Decryption successful!"
            });
        }
        catch (Exception ex)
        {
            // If the key is wrong, this will return the "Padding is invalid" error
            return BadRequest(new { error = "Decryption Failed: " + ex.Message });
        }
    }


    [HttpGet]
    [AllowAnonymous]
    public IActionResult ConfigStatus()
    {
        var status = new
        {
            HasEncryptionKey = !string.IsNullOrEmpty(_configuration["Encryption:Key"]),
            EncryptionKeyLength = _configuration["Encryption:Key"]?.Length ?? 0,
            HasDefaultConnection = !string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection")),
            DefaultConnectionLength = _configuration.GetConnectionString("DefaultConnection")?.Length ?? 0,
            HasODBCConnection = !string.IsNullOrEmpty(_configuration.GetConnectionString("ODBCConnectionString")),
            ODBCConnectionLength = _configuration.GetConnectionString("ODBCConnectionString")?.Length ?? 0,
            HasJwtSecret = !string.IsNullOrEmpty(_configuration["Jwt:SecretKey"]),
            JwtSecretLength = _configuration["Jwt:SecretKey"]?.Length ?? 0
        };

        return Ok(status);
    }


    [HttpPost]
    [AllowAnonymous]
    //[IgnoreAntiforgeryToken]
    public IActionResult EncryptText([FromBody] EncryptRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text))
            {
                return BadRequest(new { error = "Text to encrypt is required" });
            }

            var encryptedText = _encryptionService.Encrypt(request.Text);

            return Ok(new
            {
                original = request.Text,
                encrypted = encryptedText,
                message = "Text encrypted successfully!",
                instructions = "Copy the 'encrypted' value to your appsettings.json"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public class EncryptRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}