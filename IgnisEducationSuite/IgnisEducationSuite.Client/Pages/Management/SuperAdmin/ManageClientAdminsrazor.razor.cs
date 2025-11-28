using EDUSphereSharedProject.IdentiyModels;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Data;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Pages.Management.SuperAdmin

{
    public partial class ManageClientAdminsrazor : ComponentBase
    {

        protected UniversalUser newUser = new();
        protected School school = new();
        protected School selectedSchool = new();
        protected List<Gender> genders = new();
        protected List<CountryDTO> countries = new();
        protected List<CityDTO> Cities = new();
        protected List<School> schools = new();
        protected List<ClientAdmin> admins = new();
        protected CountryDTO Country = new();
        protected CityDTO City = new();
        protected ClientAdmin newClientAdmin = new();
        protected ApplicationUser selectedAdmin = new();

        protected PasswordGenerator passwordGenerator = new();
        [Inject] GenericServiceFactory _genericService { get; set; } = default!;

        [Inject] NavigationManager navigationManager { get; set; } = default!;
        [Inject] HttpClient Http { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; } = default!;
        [Inject] ClientEmailService _emailService { get; set; } = default!;
        [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        private List<ApplicationUser> userRoles = new();

        protected List<string> Roles = new();

        [Inject] NavigationManager _navigationManager { get; set; }
        protected bool isLoading = false;
        protected string searchString { get; set; } = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            await GetSchools();
            await GetGenders();
            await GetCountries();
            isLoading = false;
        }
        protected async Task GetUsersWithRoles()
        {
            isLoading = true;
            userRoles = await Http.GetFromJsonAsync<List<ApplicationUser>>($"{_navigationManager.BaseUri}api/Admin/getUsersByRoleAdmin/{selectedSchool.SchoolID}");
            if (userRoles.Any())
            {
                isLoading = false;
            }
            isLoading = false;
        }
        protected async Task GetGenders()
        {
            var result = _genericService.GetService<Gender>();
            var service = await result.GetAllAsync($"api/Dynamic/GetEntity/{"genders"}", true);
            genders = service.Data.ToList();

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
                Snackbar.Add("Failed to Collect Countries", Severity.Error);

            }
        }
        protected async Task AddAdmin()
        {
            isLoading = true;
            var auth = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = auth.User;
            var Gen = new PasswordGenerator();
            newUser.Password = Gen.GenerateOneTimePassword();
            newUser.Role = "Admin";
            CreateUserModel createUserModel = new()
            {
                UserName = newUser.UserName,
                Password = newUser.Password,
                Email = newUser.Email,
                Role = newUser.Role,
                SchoolID = school.SchoolID,
                UserID = "N/A",
                ProfilePic = newUser.profilePic,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName
            };
            try
            {
                var result = await Http.PostAsJsonAsync($"{navigationManager.BaseUri}api/Admin/createUser", createUserModel);
                if (result.IsSuccessStatusCode)
                {
                    var newlyAddeduser = await result.Content.ReadFromJsonAsync<ApplicationUser>();
                    newClientAdmin = new()
                    {
                        AdminID = Guid.NewGuid(),
                        SchoolID = school.SchoolID,
                        FirstName = newUser.FirstName,
                        LastName = newUser.LastName,
                        Email = newUser.Email,
                        Gender = newUser.Gender,
                        MobileNumber = newUser.ContactNo,
                        Nationality = Country.CountryName,
                        City = City.CityName,
                        UserID = newlyAddeduser.Id,
                        CreatedBy = user.Identity.Name,
                        ProfilePic = newUser.profilePic,
                        CreatedDate = DateTime.Now,


                    };
                    var service = _genericService.GetService<ClientAdmin>();

                    var Returnresult = await service.PostAsync("api/Dynamic/PostEntity", "clientadmin", newClientAdmin);
                    if (Returnresult.IsSuccess)
                    {
                        newClientAdmin.School = school;
                        admins.Add(newClientAdmin);

                        Snackbar.Add($"New admin for {school.SchoolName} has been added successfully", Severity.Success);
                        //Mail User//
                        EmailRequest email = new()
                        {
                            To = newClientAdmin.Email,
                            Reciepient = newClientAdmin.FirstName + " " + newClientAdmin.LastName,
                            Password = newUser.Password,
                            UserName = newUser.UserName,
                            StudentID = "N/A",
                            isFirstMail = true
                        };
                        newClientAdmin = new();
                        var emailsent = await _emailService.SendPasswordResetEmailAsync(email, navigationManager.BaseUri);
                        if (emailsent == "Password reset email sent successfully!")
                        {
                            isLoading = false;
                            Snackbar.Add("New teacher One time password mailed successfully", Severity.Success);
                        }
                        else
                        {
                            isLoading = false;
                            Snackbar.Add("Error Sending Password email", Severity.Error);

                        }
                        StateHasChanged();
                    }
                    else
                    {
                        isLoading = false;
                        Snackbar.Add("Error adding new Admin", Severity.Error);
                    }
                }
                else
                {
                    isLoading = false;
                    Snackbar.Add("Error Creating Account", Severity.Error);

                }
            }
            catch (Exception ex)
            {
                var _ = ex.Message;

                throw;
            }

        }
        private async Task UploadFiles(IBrowserFile file)
        {

            if (file == null || newUser == null)
                throw new ArgumentNullException(nameof(file), "File or model cannot be null");

            using (var memoryStream = new MemoryStream())
            {
                await file.OpenReadStream().CopyToAsync(memoryStream);
                newUser.profilePic = memoryStream.ToArray();
            }
        }
        protected async Task GetSchools()
        {
            var service = _genericService.GetService<School>();
            var result = await service.GetAllAsync($"api/Dynamic/GetEntity/{"school"}", true);
            if (result.IsSuccess)
            {
                schools = result.Data.ToList();
            }
        }

        protected async Task GetCities(CountryDTO selectedCountry)
        {
            Country = selectedCountry;
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

            if (string.IsNullOrEmpty(value))
                return new List<CountryDTO>();
            return countries.Where(x => x.CountryName.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
        private async Task<IEnumerable<CityDTO>> SearchCity(string value, CancellationToken token)
        {

            await Task.Delay(5, token);


            if (string.IsNullOrEmpty(value))
                return new List<CityDTO>();
            return Cities.Where(x => x.CityName.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
        private async Task<IEnumerable<School>> SearchSchool(string value, CancellationToken token)
        {

            await Task.Delay(5, token);


            if (string.IsNullOrEmpty(value))
                return new List<School>();
            return schools.Where(x => x.SchoolName.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }
        private bool FilterFunc1(ApplicationUser element) => FilterFunc(element, searchString);

        private bool FilterFunc(ApplicationUser element, string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return true;
            if (element.FirstName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            if (element.LastName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
        protected async Task ResetPassword(ApplicationUser user)
        {
            isLoading = true;
            PasswordResetModel model = new();
            model.UserID = user.Id;
            model.Password = passwordGenerator.GenerateOneTimePassword();

            var result = await Http.PutAsJsonAsync($"{_navigationManager.BaseUri}api/Admin/resetPassword", model);
            if (result.IsSuccessStatusCode)
            {
                //Mail one time password
                //Mail User//
                EmailRequest email = new()
                {
                    To = user.Email,
                    Reciepient = user.UserName,
                    Password = model.Password
                };
                var emailsent = await _emailService.SendPasswordResetEmailAsync(email, _navigationManager.BaseUri);
                if (emailsent == "Password reset email sent successfully!")
                {
                    isLoading = false;
                    Snackbar.Add(emailsent, Severity.Success);
                    isLoading = false;
                    Snackbar.Add("Password successfully Reset and One time password has been mailed to the user.", Severity.Success);

                }
                else
                {
                    isLoading = false;
                    Snackbar.Add("Password successfully Reset and One time password has been mailed to the user.", Severity.Success);

                }
                //Close Dialog

            }
        }
        #region To String Func

        protected Func<CountryDTO, string> CountryTypeconverter = p => p?.CountryCode + ":" + p?.CountryName;
        protected Func<CityDTO, string> CityTypeconverter = p => p?.CityName;
        protected Func<School, string> SchoolTypeconverter = p => p?.SchoolName + " " + p?.SchoolName;
        #endregion
    }
}