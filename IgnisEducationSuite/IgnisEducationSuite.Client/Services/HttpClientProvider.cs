namespace IgnisEducationSuite.Client.Services
{

    public class HttpClientProvider : IServiceProvider
    {
        private readonly HttpClient _httpClient;

        public HttpClientProvider(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public object GetService(Type serviceType)
        {
            // Only handle GenericService<T>
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IGenericService<>))
            {
                var entityType = serviceType.GetGenericArguments()[0];
                var genericServiceType = typeof(GenericService<>).MakeGenericType(entityType);
                return Activator.CreateInstance(genericServiceType, _httpClient)!;
            }

            throw new NotImplementedException($"Service type {serviceType.Name} is not supported by HttpClientProvider.");
        }
    }

}

