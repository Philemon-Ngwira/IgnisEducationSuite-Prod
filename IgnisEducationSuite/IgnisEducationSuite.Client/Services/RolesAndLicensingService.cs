using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace IgnisEducationSuite.Client.Services
{
    public class RolesAndLicensingService
    {
        private readonly HttpClient _httpClient;

        public RolesAndLicensingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetUserRole(string BaseURI, string UserNameOrEmail)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseURI}api/Roles/GetUserRoles/{UserNameOrEmail}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode}, Content: {errorContent}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle specific HTTP exceptions
                return $"HttpRequestException: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<string> GetUserID(string BaseURI, string UsernameorEmail)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseURI}api/user/userID/{UsernameorEmail}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode}, Content: {errorContent}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle specific HTTP exceptions
                return $"HttpRequestException: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                return $"Exception: {ex.Message}";
            }

        }
        public async Task<string> GetSchoolId(string BaseURI, string UserID)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseURI}api/user/SchoolID/{UserID}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content.Trim('"').Replace("\\", "");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode}, Content: {errorContent}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle specific HTTP exceptions
                return $"HttpRequestException: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                return $"Exception: {ex.Message}";
            }
        }
        public async Task<bool> GetLoginAttempt(string BaseURI, string UserID)
        {
            // Send the GET request
            var result = await _httpClient.GetAsync($"{BaseURI}api/user/UserLoginAttempt/{UserID}");

            // Check if the request succeeded
            if (result.IsSuccessStatusCode)
            {
                // Read the response content as a string
                var content = await result.Content.ReadAsStringAsync();

                // Parse the content to a bool and return it
                return bool.Parse(content);
            }

            // Handle the failure case (e.g., return false or throw an exception)
            throw new Exception($"Failed to fetch login attempt. Status code: {result.StatusCode}, Reason: {result.ReasonPhrase}");
        }

        public async Task<bool> UpdateLoginAttemptAsync(string baseUri, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("UserID cannot be null or empty.", nameof(userId));
            }

            // Send the request with JSON payload
            var response = await _httpClient.PutAsJsonAsync($"{baseUri}api/user/UpdateLoginAttempt", userId);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update login attempt: {response.StatusCode} - {errorContent}");
        }

        public async Task<bool> getStudentDashState(string SchoolID)
        {
            var result = await _httpClient.GetFromJsonAsync<bool>($"api/Dynamic/GetStudentDashboardState/{SchoolID}");
            return result;
        }
        public async Task<bool> GetLicenseStatus(string BaseURI, string ClientID)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseURI}api/Verification/Validate/{ClientID}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content.StartsWith("License Invalid"))
                    {
                        return false;
                    }
                    else { return true; }
                }
                else
                {
                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle specific HTTP exceptions
                return false;
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                return false;
            }
        }
    }
}
