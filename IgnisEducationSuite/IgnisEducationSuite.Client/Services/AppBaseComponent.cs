using EDUSphereSharedProject.Models;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public class AppBaseComponent : ComponentBase, IDisposable
{
    [Inject] protected AppState AppState { get; set; }
    [Inject] protected HttpClient HttpClient { get; set; }
    [Inject] protected IJSRuntime JS { get; set; }
    [Inject] protected GenericServiceFactory GenericService { get; set; }

    protected List<AcademicLevel> academicLevels { get; set; } = new List<AcademicLevel>();
    protected override void OnInitialized()
    {
        AppState.OnChange += StateHasChanged;
        academicLevels = AppState.academicLevels;
    }

    public void Dispose()
    {
        AppState.OnChange -= StateHasChanged;
    }


   
}
