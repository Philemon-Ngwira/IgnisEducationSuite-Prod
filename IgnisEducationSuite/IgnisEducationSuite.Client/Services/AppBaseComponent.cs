using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public class AppBaseComponent : ComponentBase, IDisposable
{
    [Inject] protected AppState AppState { get; set; }
    [Inject] protected HttpClient HttpClient { get; set; }
    [Inject] protected IJSRuntime JS {  get; set; }
    [Inject] protected GenericServiceFactory GenericService { get; set; }
    protected override void OnInitialized()
    {
        AppState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        AppState.OnChange -= StateHasChanged;
    }
}
