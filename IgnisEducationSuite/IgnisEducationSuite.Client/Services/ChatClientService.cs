using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Services
{
    public class ChatClientService
    {
        private readonly HttpClient _httpClient;
        public ChatClientService(HttpClient httpClient)
        {

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GetChatResponseAsync(string endPoint, string userMessage)
        {
            try
            {
                // Making a GET request with the provided endpoint and user message
                var response = await _httpClient.GetStringAsync($"{endPoint}/{userMessage}");

                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new Exception("Received an empty response from the server.");
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                // Log the exception or handle it appropriately
                throw new Exception("Failed to retrieve chat response.", ex);
            }
        }

    }
}
