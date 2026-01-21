using EDUSphereSharedProject.IdentiyModels;
using EDUSphereSharedProject.Models;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

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
    protected Color GetPriorityColor(string priority)
    {
        return priority switch
        {
            "Low" => Color.Success,   // Green
            "Medium" => Color.Warning,   // Yellow
            "High" => Color.Error,     // Red
            "Critical" => Color.Dark,      // Dark / Almost black
            _ => Color.Default
        };
    }

    protected async Task<ApplicationUser> GetUserInformation(string UserID)
    {
        var service = GenericService.GetService<ApplicationUser>();
        var result = await HttpClient.GetFromJsonAsync<ApplicationUser>($"api/Admin/getUserById/{UserID}");
        if (result != null)
        {
            return result;
        }
        else
        {
            return null;
        }
    }
    public void Dispose()
    {
        AppState.OnChange -= StateHasChanged;
    }



}
