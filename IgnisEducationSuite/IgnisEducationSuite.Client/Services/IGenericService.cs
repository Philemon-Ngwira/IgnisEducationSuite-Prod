
namespace IgnisEducationSuite.Client.Services
{
    public interface IGenericService<T> where T : class
    {
        Task<ServiceResult<IEnumerable<T>>> GetAllAsync(string endpoint, bool recall, CancellationToken cancellationToken = default); 
        Task<ServiceResult<T>> GetByIdAsync(string endpoint, Guid id, CancellationToken cancellationToken = default); 
        Task<ServiceResult<T>> UpdateAsync(string endpoint, string entity, T data, CancellationToken cancellationToken = default);
        Task<ServiceResult<T>> PostAsync(string endpoint, string entity, T data, CancellationToken cancellationToken = default);
    }
}