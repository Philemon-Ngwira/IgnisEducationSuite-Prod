using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;

namespace IgnisEducationSuite.Client.Services
{
    public class GenericService<T> : IGenericService<T> where T : class
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public GenericService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }
        // Caching example for GetAllAsync
        public async Task<ServiceResult<IEnumerable<T>>> GetAllAsync(string endpoint, bool recall, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"{typeof(T).Name}_all";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<T> cachedItems) & !recall)
            {
                return ServiceResult<IEnumerable<T>>.Success(cachedItems);
            }

            var response = await _httpClient.GetAsync($"{endpoint}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            
            try
            {
                var result = JsonSerializer.Deserialize<IEnumerable<T>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5)); // Cache the result for 5 minutes
                return ServiceResult<IEnumerable<T>>.Success(result);
            }
            catch (JsonException ex)
            {
                return ServiceResult<IEnumerable<T>>.Failure($"Deserialization error: {ex.Message}");
            }
        }
        public async Task<ServiceResult<T>> GetByIdAsync(string endpoint, Guid id, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"{typeof(T).Name}_{id}";

            if (_cache.TryGetValue(cacheKey, out T cachedItem))
            {
                return ServiceResult<T>.Success(cachedItem);
            }

            var response = await _httpClient.GetAsync($"{endpoint}/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                var result = JsonSerializer.Deserialize<T>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5)); // Cache the result for 5 minutes
                return ServiceResult<T>.Success(result);
            }
            catch (JsonException ex)
            {
                return ServiceResult<T>.Failure($"Deserialization error: {ex.Message}");
            }
        }
        public async Task<ServiceResult<T>> UpdateAsync(string endpoint, string entity, T data, CancellationToken cancellationToken = default)
        {
            var jsonContent = JsonSerializer.Serialize(data); var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            try
            { // Construct the URL with the entity query parameter
                var url = $"{endpoint}?entity={entity}"; // Send the PUT request
                var response = await _httpClient.PutAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                // Update the cache with the new data
                string cacheKey = $"{typeof(T).Name}_{data.GetHashCode()}";
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                // Cache the result for 5 minutes
                return ServiceResult<T>.Success(result);
            }
            catch (HttpRequestException ex)
            {
                return ServiceResult<T>.Failure($"Request error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return ServiceResult<T>.Failure($"Deserialization error: {ex.Message}");
            }
        }
        public async Task<bool> DeleteEntityAsync(string entity, Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Dynamic/DeleteEntity/{entity}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                Console.WriteLine($"Error: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting entity: {ex.Message}");
                return false;
            }
        }
        public async Task<ServiceResult<T>> PostAsync(string endpoint, string entity, T data, CancellationToken cancellationToken = default)
        {
            var jsonContent = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                // Construct the URL with the entity query parameter
                var url = $"{endpoint}?entity={entity}";

                // Send the POST request
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return ServiceResult<T>.Success(result);
            }
            catch (HttpRequestException ex)
            {
                return ServiceResult<T>.Failure($"Request error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return ServiceResult<T>.Failure($"Deserialization error: {ex.Message}");
            }
        }

    }


}




// ServiceResult class for wrapping responses
public class ServiceResult<T>
{
    public T Data { get; private set; }
    public bool IsSuccess { get; private set; }
    public string ErrorMessage { get; private set; }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T> { Data = data, IsSuccess = true };
    }

    public static ServiceResult<T> Failure(string errorMessage)
    {
        return new ServiceResult<T> { IsSuccess = false, ErrorMessage = errorMessage };
    }
}


