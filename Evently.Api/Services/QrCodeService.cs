using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace Evently.Api.Services
{
    public class QrCodeService
    {
        private readonly byte[] _signingKey;

        public QrCodeService(SecuritySettings security)
        {
            _signingKey = security.JwtKeyBytes;
        }

        public byte[] GenerateQrCode(string data)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(20);
            return qrBytes;
        }

        public string GenerateQrCodeBase64(string data)
        {
            var bytes = GenerateQrCode(data);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Gera um código de ingresso assinado (anti-fraude): {idBase64Url}.{assinaturaHmac}
        /// Mantém-se curto (&lt; 50 caracteres) para caber na coluna QrCode.
        /// </summary>
        public string CreateTicketCode(Guid ticketId)
        {
            var id = Base64UrlEncode(ticketId.ToByteArray());
            return $"{id}.{Sign(id)}";
        }

        /// <summary>
        /// Verifica a assinatura do código e devolve o id do ingresso quando válido.
        /// </summary>
        public bool TryVerifyTicketCode(string code, out Guid ticketId)
        {
            ticketId = Guid.Empty;
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var parts = code.Split('.');
            if (parts.Length != 2)
                return false;

            var id = parts[0];
            var signature = parts[1];

            var expected = Sign(id);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(signature),
                    Encoding.UTF8.GetBytes(expected)))
                return false;

            try
            {
                ticketId = new Guid(Base64UrlDecode(id));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string Sign(string data)
        {
            using var hmac = new HMACSHA256(_signingKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            // 8 bytes (64 bits) de MAC — suficiente para anti-fraude e mantém o código curto
            return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }

        private static string Base64UrlEncode(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string value)
        {
            var s = value.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
