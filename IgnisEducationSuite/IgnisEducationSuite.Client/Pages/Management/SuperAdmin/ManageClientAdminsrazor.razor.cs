using EDUSphereSharedProject.IdentiyModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels;
using EDUSphereSharedProject.UniversalModels.SuperAdmin;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Pages.Management.SuperAdmin
{
    /// <summary>
    /// Creating and managing the administrator accounts that run each school.
    ///
    /// Administrators are now read through the SuperAdmin console's endpoint rather than
    /// api/Admin/getUsersByRoleAdmin. That one returns whole ApplicationUser records — password
    /// hash, security stamp and all — straight to the browser; TenantAdminDto carries only what the
    /// screen displays.
    /// </summary>
    public partial class ManageClientAdminsrazor : ComponentBase
    {
        protected UniversalUser newUser = new();
        protected School? school;
        protected School? selectedSchool;

        protected List<Gender> genders = new();
        protected List<CountryDTO> countries = new();
        protected List<CityDTO> Cities = new();
        protected List<School> schools = new();
        protected CountryDTO? Country;
        protected CityDTO? City;

        private List<TenantAdminDto> admins = new();

        [Inject] GenericServiceFactory _genericService { get; set; } = default!;
        [Inject] ISuperAdminClientService _superAdmin { get; set; } = default!;
        [Inject] NavigationManager navigationManager { get; set; } = default!;
        [Inject] HttpClient Http { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] IDialogService DialogService { get; set; } = default!;
        [Inject] ClientEmailService _emailService { get; set; } = default!;
        [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        private MudForm _form = default!;

        protected bool isLoading;
        protected bool isWorking;
        protected string searchString { get; set; } = string.Empty;

        private IEnumerable<TenantAdminDto> FilteredAdmins =>
            string.IsNullOrWhiteSpace(searchString)
                ? admins
                : admins.Where(a =>
                    Contains(a.FirstName) || Contains(a.LastName) ||
                    Contains(a.Email) || Contains(a.UserName));

        private bool Contains(string? value) =>
            value is not null && value.Contains(searchString, StringComparison.OrdinalIgnoreCase);

        /// <summary>Active means both flags: an account can be marked active and still be locked out,
        /// in which case the person cannot sign in and the screen must not claim otherwise.</summary>
        private static bool IsActive(TenantAdminDto admin) => admin.AccountActive && !admin.IsLockedOut;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            try
            {
                await GetSchools();
                await GetGenders();
                await GetCountries();
            }
            finally
            {
                isLoading = false;
            }
        }

        // -------------------------------------------------------------------------------
        // Existing administrators
        // -------------------------------------------------------------------------------

        private async Task OnSchoolChanged(School? value)
        {
            selectedSchool = value;
            await LoadAdminsAsync();
        }

        /// <summary>Loads the selected school's administrators. Previously this needed a separate
        /// "Get School Admins" button press after choosing a school.</summary>
        protected async Task LoadAdminsAsync()
        {
            if (selectedSchool is null)
            {
                admins = new();
                return;
            }

            isLoading = true;
            try
            {
                admins = await _superAdmin.ListAdminsAsync(selectedSchool.SchoolID);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SetActiveAsync(TenantAdminDto admin, bool active)
        {
            if (selectedSchool is null) return;

            if (!active)
            {
                var confirmed = await DialogService.ShowMessageBox(
                    "Suspend this administrator?",
                    $"{admin.DisplayName} will not be able to sign in. Their account and everything they " +
                    "created stay in place, and access can be restored at any time.",
                    yesText: "Suspend", cancelText: "Cancel");

                if (confirmed != true) return;
            }

            isWorking = true;
            try
            {
                var result = await _superAdmin.SetAdminActiveAsync(selectedSchool.SchoolID, admin.UserId, active);
                Snackbar.Add(result.Message, result.Succeeded ? Severity.Success : Severity.Error);

                if (result.Succeeded) await LoadAdminsAsync();
            }
            finally
            {
                isWorking = false;
            }
        }

        /// <summary>
        /// Issues a one-time password and mails it.
        ///
        /// The password is generated server-side now. The reset and the email are reported
        /// separately: a password that changed but was never delivered leaves an account nobody can
        /// get into, and reporting only "failed" would hide that.
        /// </summary>
        protected async Task ResetPasswordAsync(TenantAdminDto admin)
        {
            if (selectedSchool is null) return;

            var confirmed = await DialogService.ShowMessageBox(
                "Reset this password?",
                $"A new one-time password will be issued to {admin.DisplayName} and emailed to " +
                $"{admin.Email ?? "their address on file"}. Their current password stops working immediately.",
                yesText: "Reset", cancelText: "Cancel");

            if (confirmed != true) return;

            isWorking = true;
            try
            {
                var reset = await _superAdmin.ResetAdminPasswordAsync(selectedSchool.SchoolID, admin.UserId);

                if (!reset.Succeeded)
                {
                    Snackbar.Add($"Password not reset: {reset.Message}", Severity.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(reset.Email))
                {
                    Snackbar.Add("Password reset, but this account has no email address on file — " +
                                 "the new password could not be sent.", Severity.Warning);
                    return;
                }

                await MailOneTimePasswordAsync(reset.Email, admin.DisplayName, reset.UserName, reset.OneTimePassword);
                await LoadAdminsAsync();
            }
            finally
            {
                isWorking = false;
            }
        }

        private async Task MailOneTimePasswordAsync(string to, string recipient, string? userName, string? password)
        {
            var email = new EmailRequest
            {
                To = to,
                Reciepient = recipient,
                UserName = userName ?? "",
                Password = password ?? "",
                StudentID = "N/A",
                isFirstMail = true,
            };

            try
            {
                var sent = await _emailService.SendPasswordResetEmailAsync(email, navigationManager.BaseUri);
                var ok = sent == "Password reset email sent successfully!";

                Snackbar.Add(
                    ok ? $"One-time password emailed to {to}."
                       : $"The password was set, but the email failed: {sent}",
                    ok ? Severity.Success : Severity.Warning);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"The password was set, but the email could not be sent: {ex.Message}", Severity.Warning);
            }
        }

        // -------------------------------------------------------------------------------
        // Creating an administrator
        // -------------------------------------------------------------------------------

        private static string? ValidateEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? "An email address is required."
            : !email.Contains('@') || !email.Contains('.') ? "That does not look like an email address."
            : null;

        /// <summary>
        /// Creates the Identity account, the ClientAdmin record, and mails the one-time password.
        ///
        /// The previous version rethrew from its catch with no message, so any failure looked like
        /// a click that did nothing, and left the loading flag on. It also created the Identity
        /// account first and only then the ClientAdmin row — if the second step failed, the account
        /// existed with no admin record and no way to notice. That case is now reported explicitly.
        /// </summary>
        protected async Task AddAdmin()
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                Snackbar.Add("Please correct the highlighted fields.", Severity.Warning);
                return;
            }

            if (school is null)
            {
                Snackbar.Add("Choose the school this administrator belongs to.", Severity.Error);
                return;
            }

            isWorking = true;
            try
            {
                var auth = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var createdBy = auth.User.Identity?.Name ?? "SuperAdmin";

                var oneTimePassword = new PasswordGenerator().GenerateOneTimePassword();

                var createUserModel = new CreateUserModel
                {
                    UserName = newUser.UserName,
                    Password = oneTimePassword,
                    Email = newUser.Email,
                    Role = "Admin",
                    SchoolID = school.SchoolID,
                    UserID = "N/A",
                    ProfilePic = newUser.profilePic ?? Array.Empty<byte>(),
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                };

                var response = await Http.PostAsJsonAsync($"{navigationManager.BaseUri}api/Admin/createUser", createUserModel);

                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync();
                    Snackbar.Add(string.IsNullOrWhiteSpace(detail)
                        ? "The account could not be created."
                        : $"The account could not be created: {detail}", Severity.Error);
                    return;
                }

                var createdUser = await response.Content.ReadFromJsonAsync<ApplicationUser>();

                var clientAdmin = new ClientAdmin
                {
                    AdminID = Guid.NewGuid(),
                    SchoolID = school.SchoolID,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    Email = newUser.Email,
                    Gender = newUser.Gender,
                    MobileNumber = newUser.ContactNo,
                    Nationality = Country?.CountryName,
                    City = City?.CityName,
                    UserID = createdUser?.Id,
                    CreatedBy = createdBy,
                    ProfilePic = newUser.profilePic,
                    CreatedDate = DateTime.Now,
                };

                var service = _genericService.GetService<ClientAdmin>();
                var saved = await service.PostAsync("api/Dynamic/PostEntity", "clientadmin", clientAdmin);

                if (!saved.IsSuccess)
                {
                    // The sign-in account exists at this point. Saying so matters: the operator can
                    // sign in as this person, and would otherwise assume nothing was created.
                    Snackbar.Add($"The sign-in account for {newUser.Email} was created, but the administrator " +
                                 "record could not be saved. Check the school's administrator list before retrying.",
                                 Severity.Error);
                    return;
                }

                Snackbar.Add($"{newUser.FirstName} {newUser.LastName} added as an administrator for {school.SchoolName}.",
                    Severity.Success);

                await MailOneTimePasswordAsync(newUser.Email, $"{newUser.FirstName} {newUser.LastName}",
                    newUser.UserName, oneTimePassword);

                // Reset the form, and show the school just added to if it is the one on screen.
                var addedTo = school;
                newUser = new();
                school = null;
                Country = null;
                City = null;
                await _form.ResetAsync();

                if (selectedSchool?.SchoolID == addedTo.SchoolID)
                {
                    await LoadAdminsAsync();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Creating the administrator failed: {ex.Message}", Severity.Error);
            }
            finally
            {
                isWorking = false;
            }
        }

        private async Task UploadFiles(IBrowserFile file)
        {
            if (file is null) return;

            using var memoryStream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(memoryStream);
            newUser.profilePic = memoryStream.ToArray();
        }

        // -------------------------------------------------------------------------------
        // Reference data
        // -------------------------------------------------------------------------------

        protected async Task GetSchools()
        {
            var service = _genericService.GetService<School>();
            var result = await service.GetAllAsync($"api/Dynamic/GetEntity/{"school"}", true);

            if (result.IsSuccess)
            {
                schools = result.Data.OrderBy(s => s.SchoolName).ToList();
            }
        }

        protected async Task GetGenders()
        {
            var service = _genericService.GetService<Gender>();
            var result = await service.GetAllAsync($"api/Dynamic/GetEntity/{"genders"}", true);

            if (result.IsSuccess)
            {
                genders = result.Data.ToList();
            }
        }

        protected async Task GetCountries()
        {
            var service = _genericService.GetService<CountryDTO>();
            var result = await service.GetAllAsync("api/CountriesAndCities/GetAllCountries", true);

            if (result.IsSuccess)
            {
                countries = result.Data.ToList();
            }
            else
            {
                Snackbar.Add("Countries could not be loaded.", Severity.Error);
            }
        }

        protected async Task GetCities(CountryDTO selectedCountry)
        {
            Country = selectedCountry;
            City = null;

            if (selectedCountry?.CountryCode is null) return;

            var service = _genericService.GetService<CityDTO>();
            var result = await service.GetAllAsync($"api/CountriesAndCities/GetCities/{selectedCountry.CountryCode}", true);

            if (result.IsSuccess)
            {
                Cities = result.Data.ToList();
            }
        }

        private async Task<IEnumerable<CountryDTO>> SearchCountry(string value, CancellationToken token)
        {
            await Task.Delay(5, token);
            return string.IsNullOrEmpty(value)
                ? new List<CountryDTO>()
                : countries.Where(x => x.CountryName.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task<IEnumerable<CityDTO>> SearchCity(string value, CancellationToken token)
        {
            await Task.Delay(5, token);
            return string.IsNullOrEmpty(value)
                ? new List<CityDTO>()
                : Cities.Where(x => x.CityName.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task<IEnumerable<School>> SearchSchool(string value, CancellationToken token)
        {
            await Task.Delay(5, token);
            return string.IsNullOrEmpty(value)
                ? schools
                : schools.Where(x => x.SchoolName.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        #region To String Func

        protected Func<CountryDTO, string> CountryTypeconverter = p => p is null ? "" : $"{p.CountryCode}: {p.CountryName}";
        protected Func<CityDTO, string> CityTypeconverter = p => p?.CityName ?? "";

        // Was SchoolName + " " + SchoolName, which rendered every school's name twice.
        protected Func<School, string> SchoolTypeconverter = p => p?.SchoolName ?? "";

        #endregion
    }
}
