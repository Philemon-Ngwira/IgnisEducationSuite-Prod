using EDUSphereSharedProject.UniversalModels;
using IgnisEducationSuite.Data;
using IgnisEducationSuite.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // API endpoint to get all users grouped by role
        [HttpGet("GetChatUsers/{SchoolID}")]
        public async Task<IActionResult> GetChatUsers(Guid SchoolID)
        {
            var users = await _userManager.Users.Where(x => x.SchoolID == SchoolID).ToListAsync();
            return Ok(users);
        }
        [HttpGet("getUsersByRole/{SchoolID}")]
        public async Task<IActionResult> GetUsersByRole(Guid SchoolID)
        {
            var users = await _userManager.Users.ToListAsync();
            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = new Dictionary<string, List<ApplicationUser>>();

            foreach (var role in roles)
            {
                var usersInRole = new List<ApplicationUser>();

                // Collect users in the role asynchronously
                foreach (var user in users)
                {
                    if (await _userManager.IsInRoleAsync(user, role.Name) && user.SchoolID == SchoolID)
                    {
                        usersInRole.Add(user);
                    }
                }

                userRoles[role.Name] = usersInRole;
            }

            return Ok(userRoles);
        }
        [HttpGet("getUsersByRoleAdmin/{SchoolID}")]
        public async Task<IActionResult> GetUsersByRoleAdmin(Guid SchoolID)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var filteredUsers = adminUsers.Where(u => u.SchoolID == SchoolID).ToList();

            return Ok(filteredUsers);
        }

        [HttpGet("getUserById/{UserID}")]
        public async Task<IActionResult> GetUserById(string UserID)
        {
            var user = await _userManager.FindByIdAsync(UserID);
            if (user == null)
            {
                return BadRequest();

            }
            else
            {
                return Ok(user);
            }
        }
        // API endpoint to create a new user
        [HttpPost("createUser")]
        public async Task<IActionResult> CreateUser(CreateUserModel request)
        {
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                SchoolID = request.SchoolID,
                ProfilePic = request.ProfilePic,
                FirstName = request.FirstName,
                LastName = request.LastName,
                AccountActive = true,
                EmailConfirmed = true,
                requiresPasswordReset = true,

                UserID = request.UserID,
            };
            if (request.Role == "Student" || request.Role == "Teacher")
            {
                user.isFirstLogin = true;
            }
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Add user to the specified role
            if (await _roleManager.RoleExistsAsync(request.Role))
            {
                await _userManager.AddToRoleAsync(user, request.Role);
            }

            return Ok(user);
        }
        [HttpPost("UpdateLoginStatus")]
        public async Task<IActionResult> UpdateLoginStatus(string UserID)
        {
            var user = await _userManager.FindByIdAsync(UserID);
            if (user == null)
            {
                return BadRequest();
            }
            else
            {
                user.isFirstLogin = false;
                await _userManager.UpdateAsync(user);
            }
            return Ok(user);


        }
        // API endpoint to delete a user
        [HttpDelete("deleteUser/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return Ok();
        }

        // API endpoint to reactivate a user
        [HttpPut("reactivateUser/{userId}")]
        public async Task<IActionResult> ReactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.LockoutEnd = null;
                user.AccountActive = true;// Remove lockout if user is locked
                await _userManager.UpdateAsync(user);
            }
            return Ok();
        }

        [HttpPut("resetPassword")]
        public async Task<IActionResult> ResetPassword(PasswordResetModel User)
        {
            // Find the user by ID
            var user = await _userManager.FindByIdAsync(User.UserID);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Generate a one-time password
            string newPassword = User.Password;

            var ret = await _userManager.RemovePasswordAsync(user);

            var result = await _userManager.AddPasswordAsync(user, newPassword);

            if (result.Succeeded)
            {
                // Optionally, you could return the new password here or send it through email
                user.requiresPasswordReset = true;
                return Ok(new { Message = "Password reset successfully", NewPassword = newPassword });
            }

            // Handle errors if password reset failed
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = "Password reset failed", Errors = errors });
        }

        // API endpoint to reactivate a user
        [HttpPut("deactivateUser/{userId}")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                // Deactivate the user account (set AccountActive to false)
                user.AccountActive = false;

                // Optionally, lock the account (set LockoutEnd to a far future date to prevent login attempts)
                user.LockoutEnd = DateTimeOffset.MaxValue;

                // Update the user details in the database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new { message = "User account deactivated successfully." });
                }
                else
                {
                    return BadRequest(new { message = "Failed to deactivate the user account." });
                }
            }
            else
            {
                return NotFound(new { message = "User not found." });
            }
        }

    }



}

