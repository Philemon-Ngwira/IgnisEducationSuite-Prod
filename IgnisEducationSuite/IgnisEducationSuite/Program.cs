using DinkToPdf;
using DinkToPdf.Contracts;
using EduSphereDomain.AchievementData;
using EduSphereDomain.ChatData;
using EduSphereDomain.Data;
using EduSphereDomain.FinanceData;
using EduSphereDomain.MessagingData;
using EduSphereDomain.Repositories;
using IgnisEducationSuite.Client.Pages.Achievements;
using IgnisEducationSuite.Client.Pages.Achievements.Interfaces;
using IgnisEducationSuite.Client.Pages.Achievements.Services;
using IgnisEducationSuite.Client.Services;
using IgnisEducationSuite.Components;
using IgnisEducationSuite.Components.Account;
using IgnisEducationSuite.Data;
using IgnisEducationSuite.Hubs;
using IgnisEducationSuite.ServerServices;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;
using IgnisEducationSuite.Settings;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using QuestPDF.Infrastructure;
using Radzen;

namespace IgnisEducationSuite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //Mudblazor
            builder.Services.AddMudServices();
            builder.Services.AddRadzenComponents();
            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();
            builder.Services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
                .AddIdentityCookies();
            //---------------------------------------------------------------
            #region HTTP CLIENT
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped(http => new HttpClient
            {
                BaseAddress = new Uri(builder.Configuration.GetSection("BaseUri").Value!),
            });



            #endregion
            builder.Services.AddHttpClient<GoogleBooksService>();
            builder.Services.AddMemoryCache();

            // somewhere at app startup, e.g., Program.cs
            QuestPDF.Settings.License = LicenseType.Community;

            //---------------------------------------------------------------
            #region DB CONTEXTS
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<PhoenixEdusphereContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<PhoenixEdusphereChatContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<PhoenixEdusphereFinanceContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<MessagingContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<AchievementContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();
            #endregion
            //--------------------------------------------------------------
            #region SMTP CONFIGURATION
            builder.Services.Configure<SmtpSettings>(options =>
            {
                options.Email = Environment.GetEnvironmentVariable("SMTP_EMAIL");
                options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
                options.Host = Environment.GetEnvironmentVariable("SMTP_HOST");
                options.Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));
            });
            #endregion
            //--------------------------------------------------------------
            #region Custom Services
            builder.Services.AddScoped<LoaderService>();
            builder.Services.AddScoped<IBadgeService, BadgeService>();
            builder.Services.AddScoped<IUserActivityService, UserActivityService>();
            builder.Services.AddScoped<IActivityService, ActivityService>();
            builder.Services.AddScoped<IBadgeCriteriaService, BadgeCriteriaService>();
            builder.Services.AddScoped<IBooksClientService, BooksClientService>();
            builder.Services.AddScoped<AchievementDecider>();
            builder.Services.AddScoped<ChatGPTService>();
            builder.Services.AddScoped<ChatClientService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<PDFService>();
            // Register GenericService and related services
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));
            builder.Services.AddScoped<GenericServiceFactory>();
            builder.Services.AddScoped<EduSphereRepository>();
            builder.Services.AddScoped<FinananceRepository>();
            builder.Services.AddScoped<PhoenixEdusphereContextProcedures>();
            builder.Services.AddScoped<PhoenixEdusphereFinanceContextProcedures>();
            builder.Services.AddScoped<AchievementContextProcedures>();
            builder.Services.AddScoped<ImageService>();
            builder.Services.AddScoped<StudentNumberGenerator>();
            builder.Services.AddScoped<ClientEmailService>();
            builder.Services.AddScoped<PDFService>();
            builder.Services.AddScoped<RolesAndLicensingService>();
            builder.Services.AddScoped<RolesService>();
            builder.Services.AddScoped<LicenseService>();
            builder.Services.AddScoped<LessonService>();
            builder.Services.AddScoped<AppState>();
            builder.Services.AddScoped<ILessonMediaClientService, LessonMediaClientService>();
            builder.Services.AddScoped<LessonMediaService>();
            builder.Services.AddScoped<ZoomService>();
            builder.Services.AddScoped<ZoomInteropBridge>();
            builder.Services.AddScoped<CountryCurrencyService>();
            builder.Services.AddScoped<StudentPaymentUploadTemplate>();
            builder.Services.AddScoped<ITimetableGenerator, TimetableGenerator>();
            builder.Services.AddScoped<ITeacherAvailabilityProvider, TeacherAvailabilityProvider>();
            builder.Services.AddHttpClient(); // Registers IHttpClientFactory

            builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));

            #endregion
            //--------------------------------------------------------------
            #region Controllers
            builder.Services.AddControllers();
            #endregion
            //--------------------------------------------------------------
            #region SignalR
            builder.Services.AddSignalR();
            #endregion
            //--------------------------------------------------------------

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();
            app.MapControllers();
            app.MapHub<ChatHub>("/chathub");
            app.Run();
        }
    }
}
