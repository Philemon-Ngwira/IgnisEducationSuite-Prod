using EDUSphereSharedProject.UniversalModels;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Services
{
    public class BooksClientService : IBooksClientService
    {
        private readonly HttpClient _httpClient;

        public BooksClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GoogleBooksResponse?> SearchBooksAsync(string query)
        {
            return await _httpClient.GetFromJsonAsync<GoogleBooksResponse>($"api/Books/search?query={query}");
        }
        public async Task<GoogleBooksResponse?> GetDefaultBooksAsync(string query)
        {
            return await _httpClient.GetFromJsonAsync<GoogleBooksResponse>($"api/Books/default/{query}");
        }

    }
}
