using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduSphereDomain.Repositories
{
    public class ChatGPTService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ChatGPTService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                new { role = "system", content = "You are chatting with a Blazor application." },
                new { role = "user", content = userMessage }
            }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            int maxRetries = 5;
            int delay = 2000; // Start with a 2-second delay

            for (int retry = 0; retry < maxRetries; retry++)
            {
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(result);
                    return document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(delay); // Wait before retrying
                    delay *= 2; // Double the delay for each retry
                }
                else
                {
                    response.EnsureSuccessStatusCode(); // Throw if other error occurs
                }
            }

            throw new HttpRequestException("Exceeded maximum retry attempts due to rate limiting.");

        }

        public async Task<string> GetChatResult(string query)
        {
            ChatClient client = new(model: "gpt-4o", apiKey: _apiKey);
            ChatCompletion completion = client.CompleteChat(query);
            return completion.Content[0].Text;
        }
    }
}
