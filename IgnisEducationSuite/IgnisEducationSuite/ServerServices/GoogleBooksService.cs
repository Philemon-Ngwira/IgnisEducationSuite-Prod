using EDUSphereSharedProject.UniversalModels;

namespace IgnisEducationSuite.ServerServices
{
    public class GoogleBooksService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://www.googleapis.com/books/v1/volumes";

        public GoogleBooksService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GoogleBooksResponse?> SearchBooksAsync(string query, string apiKey)
        {
            var url = $"{BaseUrl}?q={query}&key={apiKey}";
            return await _httpClient.GetFromJsonAsync<GoogleBooksResponse>(url);
        }
    }

   

  

    
}
