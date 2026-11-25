using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Evently.Api.DTOs;

namespace Evently.Api.Services
{
    public sealed class PaymentWebhookService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly byte[] _secret;

        public PaymentWebhookService(SecuritySettings security)
        {
            _secret = security.WebhookSecretBytes;
        }

        public bool TryVerifySignature(string rawBody, string? signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader))
                return false;

            var value = signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
                ? signatureHeader[7..]
                : signatureHeader;

            byte[] provided;
            try
            {
                provided = Convert.FromHexString(value.Trim());
            }
            catch (FormatException)
            {
                return false;
            }

            var expected = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(rawBody));
            return CryptographicOperations.FixedTimeEquals(expected, provided);
        }

        public PaymentWebhookDto? Parse(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
                return null;

            try
            {
                return JsonSerializer.Deserialize<PaymentWebhookDto>(rawBody, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
