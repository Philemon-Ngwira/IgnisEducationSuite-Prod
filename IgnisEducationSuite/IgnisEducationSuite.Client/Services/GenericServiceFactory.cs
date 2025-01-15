namespace IgnisEducationSuite.Client.Services
{
    public class GenericServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GenericServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IGenericService<T> GetService<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<IGenericService<T>>();
        }
      
    }

}
