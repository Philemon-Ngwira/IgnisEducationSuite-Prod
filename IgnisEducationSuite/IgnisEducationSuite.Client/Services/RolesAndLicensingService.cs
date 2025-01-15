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
