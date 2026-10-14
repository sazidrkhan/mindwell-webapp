// Purpose: Time-based one-time password utilities for MindWell MFA enrollment,
// QR provisioning, verification, and startup sanity checks.
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using QRCoder;

namespace MindWell
{
    public static class TotpHelper
    {
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static string GenerateSecret()
        {
            byte[] secretBytes = new byte[20];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(secretBytes);
            }

            return Base32Encode(secretBytes);
        }

        public static string Base32Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            StringBuilder result = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;

            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[next++] & 0xff;
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = (buffer >> (bitsLeft - 5)) & 0x1f;
                bitsLeft -= 5;
                result.Append(Base32Alphabet[index]);
            }

            return result.ToString();
        }

        public static byte[] Base32Decode(string input)
        {
            string normalized = Regex.Replace(input ?? string.Empty, "[=\\s-]", string.Empty).ToUpperInvariant();
            if (normalized.Length == 0)
                return new byte[0];

            int buffer = 0;
            int bitsLeft = 0;
            var bytes = new System.Collections.Generic.List<byte>();

            foreach (char c in normalized)
            {
                int value = Base32Alphabet.IndexOf(c);
                if (value < 0)
                    throw new FormatException("TOTP secret is not valid Base32.");

                buffer <<= 5;
                buffer |= value & 0x1f;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                    bitsLeft -= 8;
                }
            }

            return bytes.ToArray();
        }

        public static string GenerateProvisioningUri(string username, string secret, string issuer = "MindWell")
        {
            string cleanIssuer = string.IsNullOrWhiteSpace(issuer) ? "MindWell" : issuer.Trim();
            string encodedUsername = HttpUtility.UrlEncode(username ?? string.Empty);
            string encodedIssuer = HttpUtility.UrlEncode(cleanIssuer);
            return "otpauth://totp/" + encodedIssuer + ":" + encodedUsername + "?secret=" + secret + "&issuer=" + encodedIssuer + "&algorithm=SHA1&digits=6&period=30";
        }

        public static string GenerateQrCodeSvg(string provisioningUri)
        {
            try
            {
                using (QRCodeGenerator generator = new QRCodeGenerator())
                using (QRCodeData data = generator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q))
                {
                    SvgQRCode qrCode = new SvgQRCode(data);
                    string svg = qrCode.GetGraphic(4, "#204D3D", "#F6F9F2", true, SvgQRCode.SizingMode.ViewBoxAttribute);
                    return "<div class='totp-qr' role='img' aria-label='Authenticator QR code for MindWell'>" + svg + "</div>";
                }
            }
            catch (Exception ex)
            {
                MindWellServices.LogDiagnostic("QRCoder SVG generation failed; showing manual TOTP provisioning URI instead.", ex);
                return "<div class='totp-qr totp-qr-fallback'><code>" + HttpUtility.HtmlEncode(provisioningUri) + "</code></div>";
            }
        }

        public static string GenerateCode(string secret, DateTime? utcNow = null)
        {
            DateTime now = (utcNow ?? DateTime.UtcNow).ToUniversalTime();
            long counter = GetTimeCounter(now);
            return GenerateCodeForCounter(secret, counter, 6);
        }

        public static bool VerifyCode(string secret, string userCode, int windowSteps = 1)
        {
            string candidate = (userCode ?? string.Empty).Trim();
            if (!Regex.IsMatch(candidate, "^[0-9]{6}$"))
                return false;

            long currentCounter = GetTimeCounter(DateTime.UtcNow);
            int window = Math.Max(0, windowSteps);
            for (int offset = -window; offset <= window; offset++)
            {
                string expected = GenerateCodeForCounter(secret, currentCounter + offset, 6);
                if (ConstantTimeEquals(expected, candidate))
                    return true;
            }

            return false;
        }

        public static string GenerateCurrentCodeForSeed(string secret)
        {
            HttpContext context = HttpContext.Current;
            if (context == null || !context.IsDebuggingEnabled)
                throw new InvalidOperationException("Current TOTP codes are available only while ASP.NET debugging is enabled.");

            return GenerateCode(secret);
        }

        public static void SelfTest()
        {
            try
            {
                string vectorSecret = Base32Encode(Encoding.ASCII.GetBytes("12345678901234567890"));
                DateTime vectorTime = UnixEpoch.AddSeconds(59);
                string code = GenerateCodeForCounter(vectorSecret, GetTimeCounter(vectorTime), 8);
                if (code == "94287082")
                    MindWellServices.LogDiagnostic("TotpHelper.SelfTest passed RFC 6238 SHA1 vector.");
                else
                    MindWellServices.LogDiagnostic("TotpHelper.SelfTest failed RFC 6238 SHA1 vector. Expected 94287082, got " + code + ".");
            }
            catch (Exception ex)
            {
                MindWellServices.LogDiagnostic("TotpHelper.SelfTest failed with exception.", ex);
            }
        }

        private static long GetTimeCounter(DateTime utcNow)
        {
            long unixSeconds = (long)(utcNow.ToUniversalTime() - UnixEpoch).TotalSeconds;
            return unixSeconds / 30L;
        }

        private static string GenerateCodeForCounter(string secret, long counter, int digits)
        {
            byte[] key = Base32Decode(secret);
            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

            using (HMACSHA1 hmac = new HMACSHA1(key))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);
                int offset = hash[hash.Length - 1] & 0x0f;
                int binary = ((hash[offset] & 0x7f) << 24)
                    | ((hash[offset + 1] & 0xff) << 16)
                    | ((hash[offset + 2] & 0xff) << 8)
                    | (hash[offset + 3] & 0xff);

                int modulus = digits == 8 ? 100000000 : 1000000;
                int otp = binary % modulus;
                return otp.ToString(new string('0', digits));
            }
        }

        private static bool ConstantTimeEquals(string expected, string actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length)
                return false;

            int mismatch = 0;
            for (int i = 0; i < expected.Length; i++)
                mismatch |= expected[i] ^ actual[i];

            return mismatch == 0;
        }
    }
}
