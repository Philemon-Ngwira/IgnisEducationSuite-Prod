using IgnisEducationSuite.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IgnisEducationSuite.ServerServices
{
    public class RolesService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public string UserRole { get; private set; }
        public string UserID { get; private set; }
        public string SchoolID { get; private set; }
        public RolesService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<string> GetUserRolesAsync(string Email)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(Email);


                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(Email);
                    if (user == null)
                    {

                        user = await _userManager.FindByNameAsync(Email);
                        if (user == null)
                            return ("UnAuthorized");
                    }
                }

                var roles = await _userManager.GetRolesAsync(user);
                return roles.FirstOrDefault();

                // Now you have a list of roles

            }
            catch (HttpRequestException ex)
            {
                // Handle exceptions, such as logging or returning an empty list
                Console.WriteLine($"Error fetching roles: {ex.Message}");
                return string.Empty;
            }
        }



        public async Task<string> GetUserIdAsync(ClaimsPrincipal loggeduser)
        {
            var user = await _userManager.GetUserAsync(loggeduser);
            var userId = user?.Id;
            return userId.ToString();
        }
        public async Task<string> GetUserSchoolIdAsync(string Email)
        {
            var user = await _userManager.FindByIdAsync(Email);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(Email);
            }
            var userId = user?.SchoolID;
            return userId.ToString();

        }
    }
}
