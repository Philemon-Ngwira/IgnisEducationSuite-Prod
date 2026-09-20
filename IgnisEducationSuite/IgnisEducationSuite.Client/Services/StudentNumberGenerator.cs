using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace IgnisEducationSuite.Client.Services
{
    public class StudentNumberGenerator
    {
        private readonly HttpClient _http;

        public StudentNumberGenerator(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<string> GenerateStudentIdAsync(string prefix, string BaseUri)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix cannot be null or whitespace.", nameof(prefix));
            }

            string studentId = null;
            var random = new Random();

            do
            {
                // Generate a random number (e.g., 6 digits). Adjust as necessary.
                var randomNumber = random.Next(100000, 999999);
                studentId = $"{prefix}{randomNumber}";

                // Check if the generated ID already exists
            } while (studentId != null && await StudentNumberExists(studentId, BaseUri));

            return studentId;
        }

        public async Task<bool> StudentNumberExists(string ID, string BaseUri)
        {
            var result = await _http.GetAsync($"{BaseUri}api/Dynamic/CheckIfStudentIDExists");
            if (result.IsSuccessStatusCode)
            {
                var ret = await result.Content.ReadFromJsonAsync<bool>();
                return ret;
            }
            else
            {
                return false;
            }
        }


        public static string GenerateOneTimePassword(int length = 16)
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
