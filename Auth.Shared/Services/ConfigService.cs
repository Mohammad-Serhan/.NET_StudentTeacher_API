

using Microsoft.Extensions.Configuration;

namespace Auth.Shared.Services
{
    public interface IConfigService
    {
        string GetConnectionString(string name);
        string GetJwtSecret();
    }

    public class ConfigService : IConfigService
    {
        private readonly IConfiguration _config;
        private readonly ISimpleEncryptionService _encrypt;

        public ConfigService(IConfiguration config, ISimpleEncryptionService encrypt)
        {
            _config = config;
            _encrypt = encrypt;
        }

        public string GetConnectionString(string name)
        {
            try
            {
                var value = _config.GetConnectionString(name);

                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                // Check if the value is already decrypted or not encrypted
                if (!IsLikelyEncrypted(value))
                {
                    return value;
                }

                try
                {
                    var decrypted = _encrypt.Decrypt(value);
                    return decrypted;
                }
                catch (Exception decryptEx)
                {
                    return value; // Return the encrypted value as fallback
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public string GetJwtSecret()
        {
            try
            {
                var value = _config["Jwt:SecretKey"];

                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                if (!IsLikelyEncrypted(value))
                {
                    return value;
                }

                try
                {
                    return _encrypt.Decrypt(value);
                }
                catch
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private bool IsLikelyEncrypted(string value)
        {
            // Simple heuristic to check if string might be encrypted
            // Encrypted strings often have specific Base64 characteristics
            if (string.IsNullOrEmpty(value)) return false;

            // Check for Base64 pattern (ends with =, contains + or /)
            return value.Contains("==") || value.Contains("+") || value.Contains("/");
        }
    }
}