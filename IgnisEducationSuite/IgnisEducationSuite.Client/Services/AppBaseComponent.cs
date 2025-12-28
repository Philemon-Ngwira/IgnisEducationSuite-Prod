using EDUSphereSharedProject.Models;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

public class AppBaseComponent : ComponentBase, IDisposable
{
    [Inject] protected AppState AppState { get; set; }
    [Inject] protected HttpClient HttpClient { get; set; }
    [Inject] protected IJSRuntime JS { get; set; }
    [Inject] protected GenericServiceFactory GenericService { get; set; }
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected LoaderService LoaderService { get; set; } = default!;
    protected List<AcademicLevel> academicLevels { get; set; } = new List<AcademicLevel>();
    protected override void OnInitialized()
    {
        AppState.OnChange += StateHasChanged;
        academicLevels = AppState.AcademicLevels;
    }

    protected async Task<IEnumerable<Staff>> GetStaffAsync(string roleName)
    {
        var service = GenericService.GetService<Staff>();
        var result = await service.GetAllAsync($"api/Dynamic/GetStaffBySchoolAndRole/{Guid.Parse(AppState.SchoolID)}/{roleName}", true);
        if (result.IsSuccess)
        {
            return result.Data.ToList();
        }
        return null;
    }
    protected readonly List<string> MealOrder = new()
    {
        "Breakfast",
        "Lunch",
        "Dinner"
    };
    public void Dispose()
    {
        AppState.OnChange -= StateHasChanged;
    }



}
