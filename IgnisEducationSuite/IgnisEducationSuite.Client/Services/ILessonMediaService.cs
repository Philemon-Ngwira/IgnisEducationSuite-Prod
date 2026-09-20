
namespace IgnisEducationSuite.Client.Services
{
    public interface ILessonMediaClientService
    {
        Task<List<string>> UploadFilesAsync(MultipartFormDataContent content);
    }
}