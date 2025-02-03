using EDUSphereSharedProject.UniversalModels;

namespace IgnisEducationSuite.Client.Services
{
    public interface IBooksClientService
    {
        Task<GoogleBooksResponse?> SearchBooksAsync(string query);
        Task<GoogleBooksResponse?> GetDefaultBooksAsync(string query);
    }
}