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

        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // API endpoint to get all users grouped by role
        [HttpGet("GetChatUsers/{SchoolID}")]
        public async Task<IActionResult> GetChatUsers(Guid SchoolID)
        {
            var users = await _userManager.Users.Where(x => x.SchoolID == SchoolID).ToListAsync();
            return Ok(users);
        }
        [HttpGet("getUsersByRole/{schoolId}")]
        public async Task<IActionResult> GetUsersByRole(Guid schoolId)
        {
            var data = await (
                from role in _context.Roles.AsNoTracking()

                join ur in _context.UserRoles
                    on role.Id equals ur.RoleId into roleUsers
                from ur in roleUsers.DefaultIfEmpty()

                join user in _context.Users
                    on ur.UserId equals user.Id into users
                from user in users.DefaultIfEmpty()

                where user == null || user.SchoolID == schoolId

                select new
                {
                    RoleName = role.Name,
                    User = user == null ? null : new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.FirstName,
                        user.LastName
                    }
                }
            ).ToListAsync();

            var result = data
                .GroupBy(x => x.RoleName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(x => x.User != null)
                          .Select(x => x.User)
                          .ToList()
                );

            return Ok(result);
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
        [HttpPost("addrole")]
        public async Task<IActionResult> AddRoleToUser([FromBody] ModifyUserRoleModel request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null)
                return NotFound("User not found.");

            if (!await _roleManager.RoleExistsAsync(request.Role))
                return BadRequest($"Role '{request.Role}' does not exist.");

            if (await _userManager.IsInRoleAsync(user, request.Role))
                return BadRequest("User already has this role.");

            var result = await _userManager.AddToRoleAsync(user, request.Role);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                user.Id,
                AddedRole = request.Role
            });
        }
        [HttpPost("removerole")]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] ModifyUserRoleModel request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null)
                return NotFound("User not found.");

            if (!await _roleManager.RoleExistsAsync(request.Role))
                return BadRequest($"Role '{request.Role}' does not exist.");

            if (!await _userManager.IsInRoleAsync(user, request.Role))
                return BadRequest("User does not have this role.");

            var result = await _userManager.RemoveFromRoleAsync(user, request.Role);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                user.Id,
                RemovedRole = request.Role
            });
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
                PhoneNumber = request.PhoneNumber ?? "N/A",

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

            var result = await _userManager.AddPasswordAsync(user, "P@ssword1");

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

