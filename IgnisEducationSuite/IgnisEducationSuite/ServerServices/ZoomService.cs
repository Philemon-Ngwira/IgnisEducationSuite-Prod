using EDUSphereSharedProject.Zoom;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IgnisEducationSuite.ServerServices
{
    public class ZoomService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public ZoomService(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _config = config;
            _http = httpFactory.CreateClient();
        }

        private string GetEnv(string key)
        {
            // Try environment variable first, fallback to appsettings if missing
            return Environment.GetEnvironmentVariable(key)
                   ?? _config[$"Zoom:{key}"];
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var accountId = GetEnv("ZoomAccountID");
            var clientId = GetEnv("ZoomClientID");
            var clientSecret = GetEnv("ZoomClientSecretKey");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={accountId}"
            );

            var authString = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
            );

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonSerializer.Deserialize<ZoomTokenResponse>(json);

            return obj.access_token;
        }
        public string GenerateSignature(string meetingNumber, int role, string _sdkKey, string _sdkSecret)
        {
            // Header
            var header = new { alg = "HS256", typ = "JWT" };
            var headerJson = JsonSerializer.Serialize(header);
            var headerBytes = Encoding.UTF8.GetBytes(headerJson);
            var base64Header = Base64UrlEncode(headerBytes);

            // Payload
            var payload = new
            {
                sdkKey = _sdkKey,
                mn = meetingNumber,
                role = role,
                iat = ToTimestamp(DateTime.UtcNow),
                exp = ToTimestamp(DateTime.UtcNow.AddMinutes(5)),
                tokenExp = ToTimestamp(DateTime.UtcNow.AddMinutes(5))
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var base64Payload = Base64UrlEncode(payloadBytes);

            // Signature
            var stringToSign = $"{base64Header}.{base64Payload}";
            var secretBytes = Encoding.UTF8.GetBytes(_sdkSecret);

            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            var base64Signature = Base64UrlEncode(hash);

            return $"{base64Header}.{base64Payload}.{base64Signature}";
        }

        private static long ToTimestamp(DateTime date) =>
            (long)Math.Round((date - new DateTime(1970, 1, 1)).TotalSeconds);

        private static string Base64UrlEncode(byte[] input) =>
            Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        public string GetSdkKey() =>
      GetEnv("ZoomSdkKey");

        public string GetSdkSecret() =>
            GetEnv("ZoomSdkSecret");
        public async Task<ZoomMeetingResponse> CreateMeetingAsync(string teacherEmail, string Topic)
        {
            var token = await GetAccessTokenAsync();
            var Password = GenerateRandomPassword();

            var body = new
            {
                topic = Topic,
                type = 2,
                duration = 60,
                password = Password,
                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    waiting_room = true
                }
            };

            var jsonBody = JsonSerializer.Serialize(body);

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.zoom.us/v2/users/{teacherEmail}/meetings");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(jsonBody);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var meeting = JsonSerializer.Deserialize<ZoomMeetingResponse>(responseJson);

            // Include the generated password and meeting number in the response
            meeting.Password = Password;
            meeting.MeetingNumber = meeting.id.ToString(); // Zoom meeting ID is used as meeting number
            return meeting;
        }


        public static string GenerateRandomPassword(int length = 10)
        {
            const string validChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$%!&*";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public class SignatureResponse
        {
            public string signature { get; set; }
        }
    }


}
