using DinkToPdf.Contracts;
using DinkToPdf;
using EduSphereDomain.Data;
using EduSphereDomain.MessagingData;
using EduSphereDomain.Repositories;
using IgnisEducationSuite.Client.Pages;
using IgnisEducationSuite.Client.Services;
using IgnisEducationSuite.Components;
using IgnisEducationSuite.Components.Account;
using IgnisEducationSuite.Data;
using IgnisEducationSuite.ServerServices;
using IgnisEducationSuite.Settings;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using IgnisEducationSuite.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace IgnisEducationSuite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //Mudblazor
            builder.Services.AddMudServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

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
            //---------------------------------------------------------------
            #region DB CONTEXTS
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<PhoenixEdusphereContext>(options => options.UseSqlServer(connectionString));
            builder.Services.AddDbContext<MessagingContext>(options => options.UseSqlServer(connectionString));
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
            builder.Services.AddScoped<ChatClientService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<PDFService>();
            // Register GenericService and related services
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));
            builder.Services.AddScoped<GenericServiceFactory>();
            builder.Services.AddScoped<EduSphereRepository>();
            builder.Services.AddScoped<PhoenixEdusphereContextProcedures>();
            builder.Services.AddScoped<ImageService>();
            builder.Services.AddScoped<StudentNumberGenerator>();
            builder.Services.AddScoped<ClientEmailService>();
            builder.Services.AddScoped<PDFService>();
            builder.Services.AddScoped<RolesAndLicensingService>();
            builder.Services.AddScoped<RolesService>();
            builder.Services.AddScoped<LicenseService>();
            builder.Services.AddScoped<LessonService>();
            builder.Services.AddSingleton<AppState>();
            builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));

            #endregion
            //--------------------------------------------------------------
            #region Controllers
            builder.Services.AddControllersWithViews();
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
