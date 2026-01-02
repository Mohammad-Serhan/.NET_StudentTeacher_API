

//namespace StudentApi.Services
//{
//    public interface IConfigService
//    {
//        string GetConnectionString(string name);
//        string GetJwtSecret();
//    }

//    public class ConfigService : IConfigService
//    {
//        private readonly IConfiguration _config;
//        private readonly ISimpleEncryptionService _encrypt;
//        private readonly ILogger<ConfigService> _logger;

//        public ConfigService(IConfiguration config, ISimpleEncryptionService encrypt, ILogger<ConfigService> logger)
//        {
//            _config = config;
//            _encrypt = encrypt;
//            _logger = logger;
//        }

//        public string GetConnectionString(string name)
//        {
//            try
//            {
//                var value = _config.GetConnectionString(name);

//                if (string.IsNullOrEmpty(value))
//                {
//                    _logger.LogWarning("Connection string '{Name}' is null or empty", name);
//                    return string.Empty;
//                }

//                // Check if the value is already decrypted or not encrypted
//                if (!IsLikelyEncrypted(value))
//                {
//                    _logger.LogDebug("Connection string '{Name}' appears to be unencrypted", name);
//                    return value;
//                }

//                try
//                {
//                    var decrypted = _encrypt.Decrypt(value);
//                    _logger.LogDebug("Successfully decrypted connection string '{Name}'", name);
//                    return decrypted;
//                }
//                catch (Exception decryptEx)
//                {
//                    _logger.LogWarning(decryptEx, "Failed to decrypt connection string '{Name}', using encrypted value", name);
//                    return value; // Return the encrypted value as fallback
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error retrieving connection string '{Name}'", name);
//                return string.Empty;
//            }
//        }

//        public string GetJwtSecret()
//        {
//            try
//            {
//                var value = _config["Jwt:SecretKey"];

//                if (string.IsNullOrEmpty(value))
//                {
//                    _logger.LogWarning("JWT Secret Key is null or empty");
//                    return string.Empty;
//                }

//                if (!IsLikelyEncrypted(value))
//                {
//                    return value;
//                }

//                try
//                {
//                    return _encrypt.Decrypt(value);
//                }
//                catch
//                {
//                    _logger.LogWarning("Failed to decrypt JWT secret, using encrypted value");
//                    return value;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error retrieving JWT secret");
//                return string.Empty;
//            }
//        }

//        private bool IsLikelyEncrypted(string value)
//        {
//            // Simple heuristic to check if string might be encrypted
//            // Encrypted strings often have specific Base64 characteristics
//            if (string.IsNullOrEmpty(value)) return false;

//            // Check for Base64 pattern (ends with =, contains + or /)
//            return value.Contains("==") || value.Contains("+") || value.Contains("/");
//        }
//    }
//}