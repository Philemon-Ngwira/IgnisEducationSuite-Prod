using Microsoft.AspNetCore.Components.Forms;
using static System.Net.WebRequestMethods;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Services
{
    public class LessonMediaClientService : ILessonMediaClientService
    {
        private readonly HttpClient _httpClient;

        public LessonMediaClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> UploadFilesAsync(MultipartFormDataContent content)
        {


            try
            {

                var response = await _httpClient.PostAsync("api/Media/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    var urls = await response.Content.ReadFromJsonAsync<List<string>>();
                    return urls ?? new List<string>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Upload failed: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MediaService] Error: {ex.Message}");
                throw;
            }
        }

    }
}
