using IgnisEducationSuite.Client;
using IgnisEducationSuite.Client.Pages.Achievements;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;
using IgnisEducationSuite.Client.Pages.Achievements.Services;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor.Services;
using Radzen;
using System.Net.Http;

namespace IgnisEducationSuite.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // --- MudBlazor ---
            builder.Services.AddMudServices();
            //----Radzen ---
            builder.Services.AddRadzenComponents();
            // --- HTTP Clients ---
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
            });

            builder.Services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
            }).AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            // --- Authentication ---
            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
            builder.Services.AddOidcAuthentication(options =>
            {
                builder.Configuration.Bind("Oidc", options.ProviderOptions);
            });

            // --- Memory Cache ---
            builder.Services.AddMemoryCache();

            // --- Custom Services ---
            builder.Services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));
            builder.Services.AddScoped<GenericServiceFactory>();
            builder.Services.AddScoped<AppState>();
            builder.Services.AddScoped<RolesAndLicensingService>();
            builder.Services.AddScoped<ChatClientService>();
            builder.Services.AddScoped<ImageService>();
            builder.Services.AddScoped<LessonService>();
            builder.Services.AddScoped<StudentNumberGenerator>();
            builder.Services.AddScoped<ClientEmailService>();
            builder.Services.AddScoped<IBadgeService, BadgeService>();
            builder.Services.AddScoped<IBooksClientService, BooksClientService>();
            builder.Services.AddScoped<IUserActivityService, UserActivityService>();
            builder.Services.AddScoped<IActivityService, ActivityService>();
            builder.Services.AddScoped<IBadgeCriteriaService, BadgeCriteriaService>();
            builder.Services.AddScoped<AchievementDecider>();
            builder.Services.AddScoped<ILessonMediaClientService, LessonMediaClientService>();
            builder.Services.AddScoped<ZoomInteropBridge>();
            builder.Services.AddScoped<CountryCurrencyService>();
            builder.Services.AddScoped<LoaderService>();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            await builder.Build().RunAsync();
        }
    }
}
