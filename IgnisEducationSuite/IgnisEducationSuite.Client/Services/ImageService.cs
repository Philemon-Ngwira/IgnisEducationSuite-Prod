using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class ImageService
{
    private readonly HttpClient _httpClient;

    public ImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetRandomImageUrlAsync(string baseURI)
    {
        var response = await _httpClient.GetFromJsonAsync<ImageResponse>($"{baseURI}api/image/random");
        return response?.Url;
    }

    private class ImageResponse
    {
        public string Url { get; set; }
    }
}
