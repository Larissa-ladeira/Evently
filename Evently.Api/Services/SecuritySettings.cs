using System.Text;

namespace Evently.Api.Services
{
    public sealed class SecuritySettings
    {
        private const int MinimumSecretLength = 32;

        public string JwtKey { get; }
        public string JwtIssuer { get; }
        public string JwtAudience { get; }
        public string WebhookSecret { get; }

        public byte[] JwtKeyBytes => Encoding.UTF8.GetBytes(JwtKey);
        public byte[] WebhookSecretBytes => Encoding.UTF8.GetBytes(WebhookSecret);

        public SecuritySettings(string jwtKey, string jwtIssuer, string jwtAudience, string webhookSecret)
        {
            JwtKey = jwtKey;
            JwtIssuer = jwtIssuer;
            JwtAudience = jwtAudience;
            WebhookSecret = webhookSecret;
        }

        public static SecuritySettings FromConfiguration(IConfiguration configuration)
        {
            return new SecuritySettings(
                Require(configuration, "Jwt:Key"),
                Require(configuration, "Jwt:Issuer", 1),
                Require(configuration, "Jwt:Audience", 1),
                Require(configuration, "Payment:WebhookSecret"));
        }

        private static string Require(IConfiguration configuration, string key, int minimumLength = MinimumSecretLength)
        {
            var value = configuration[key];

            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minimumLength || IsPlaceholder(value))
            {
                throw new InvalidOperationException(
                    $"A configuração '{key}' é obrigatória e inválida. " +
                    $"Defina-a com pelo menos {minimumLength} caracteres via " +
                    $"dotnet user-secrets set \"{key}\" \"<valor>\" ou variável de ambiente.");
            }

            return value.Trim();
        }

        private static bool IsPlaceholder(string value) =>
            value.Contains("AUSENTE", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("CHANGE", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("ALTERAR", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("DEFAULT", StringComparison.OrdinalIgnoreCase);
    }
}
