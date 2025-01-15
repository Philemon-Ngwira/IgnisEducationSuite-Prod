using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;

public class AppBaseComponent : ComponentBase, IDisposable
{
    [Inject] protected AppState AppState { get; set; }

    protected override void OnInitialized()
    {
        AppState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        AppState.OnChange -= StateHasChanged;
    }
}
