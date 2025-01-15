using System.Security.Cryptography;
using System.Text;

namespace IgnisEducationSuite.Client.Services
{
    public class PasswordGenerator
    {

        public string GenerateOneTimePassword(int length = 16)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_-+=<>?";
            const string numbers = "1234567890";

            // Ensure that the OTP length is at least 8 characters
            if (length < 8)
            {
                length = 8;
            }

            using (var rng = new RNGCryptoServiceProvider())
            {
                while (true) // Continue generating until we get a valid OTP
                {
                    byte[] randomBytes = new byte[length];
                    rng.GetBytes(randomBytes);

                    StringBuilder otp = new StringBuilder(length);

                    // Build the OTP string by selecting characters from the validChars string
                    foreach (var randomByte in randomBytes)
                    {
                        otp.Append(validChars[randomByte % validChars.Length]);
                    }

                    string generatedOtp = otp.ToString();

                    // Check if the generated OTP contains at least one number
                    if (generatedOtp.Any(c => numbers.Contains(c)))
                    {
                        return generatedOtp;
                    }
                }
            }
        }
    }
}
