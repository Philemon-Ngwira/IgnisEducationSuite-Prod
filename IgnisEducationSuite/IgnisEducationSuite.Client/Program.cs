using IgnisEducationSuite.Client;
using IgnisEducationSuite.Client.Pages.Achievements;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;
using IgnisEducationSuite.Client.Pages.Achievements.Services;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor.Services;

namespace IgnisEducationSuite.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            //Mudblazor
            builder.Services.AddMudServices();
            //HTTP CLIENT
            builder.Services.AddScoped(http => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
            });
            builder.Services.AddAuthorizationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
            #region Memory Cache & String
            builder.Services.AddMemoryCache();

            #endregion

            #region Custom Services
            builder.Services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));
            builder.Services.AddScoped<GenericServiceFactory>();
            builder.Services.AddScoped<ChatClientService>();
            builder.Services.AddScoped<ImageService>();
            builder.Services.AddOidcAuthentication(options => { builder.Configuration.Bind("Oidc", options.ProviderOptions); });
            builder.Services.AddScoped<LessonService>();
            builder.Services.AddScoped<StudentNumberGenerator>();
            builder.Services.AddScoped<ClientEmailService>();
            builder.Services.AddScoped<RolesAndLicensingService>();
            builder.Services.AddScoped<IBadgeService, BadgeService>();
            builder.Services.AddScoped<IBooksClientService,BooksClientService>();
            builder.Services.AddScoped<IUserActivityService, UserActivityService>(); 
            builder.Services.AddScoped<IActivityService, ActivityService>(); 
            builder.Services.AddScoped<IBadgeCriteriaService, BadgeCriteriaService>();
            builder.Services.AddScoped<AchievementDecider>();
            builder.Services.AddHttpClient(); // Registers IHttpClientFactory
            builder.Services.AddSingleton<AppState>();

            #endregion

            await builder.Build().RunAsync();
        }
    }
}
