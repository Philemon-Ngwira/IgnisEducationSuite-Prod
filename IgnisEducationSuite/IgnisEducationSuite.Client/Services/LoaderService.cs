namespace IgnisEducationSuite.Client.Services
{
    public class LoaderService
    {
        
            public event Action<bool, string>? OnLoaderChanged;

            public void Show(string text = "Loading, please wait...")
                => OnLoaderChanged?.Invoke(true, text);

            public void Hide()
                => OnLoaderChanged?.Invoke(false, string.Empty);
        

    }
}
