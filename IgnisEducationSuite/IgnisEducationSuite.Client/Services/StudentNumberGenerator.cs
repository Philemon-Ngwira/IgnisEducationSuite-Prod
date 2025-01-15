using System.Net.Http.Json;

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
    }
}
