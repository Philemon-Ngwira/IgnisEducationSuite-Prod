
using IgnisEducationSuite.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet("userID/{UserNameOrEmail}")]
        public async Task<IActionResult> GetUserId(string UserNameOrEmail)
        {
            var user = await _userManager.FindByNameAsync(UserNameOrEmail);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(UserNameOrEmail);
            }
            var userId = user?.Id;
            return Ok(userId);
        }
        [HttpGet("SchoolID/{UserID}")]
        public async Task<IActionResult> GetUserSchoolId(string UserID)
        {
            var user = await _userManager.FindByIdAsync(UserID);
            var userId = user?.SchoolID;
            return Ok(userId);
        }
        [HttpGet("UserLoginAttempt/{UserNameOrEmail}")]
        public async Task<IActionResult> GetLoginAttempt(string UserNameOrEmail)
        {
            var user = await _userManager.FindByIdAsync(UserNameOrEmail);
            var isFirstLogin = user?.isFirstLogin;
            return Ok(isFirstLogin);
        }

        [HttpPut("UpdateLoginAttempt")]
        public async Task<IActionResult> UpdateAttempt([FromBody] string UserID)
        {
            // Ensure UserID is provided
            if (string.IsNullOrEmpty(UserID))
            {
                return BadRequest("UserID cannot be null or empty.");
            }

            // Find the user
            var user = await _userManager.FindByIdAsync(UserID);
            if (user == null)
            {
                return NotFound($"User with ID '{UserID}' not found.");
            }

            // Update the user's isFirstLogin property
            user.isFirstLogin = false;

            // Attempt to update the user
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { Message = "User login attempt updated successfully." });
            }

            // Handle errors during update
            return BadRequest(new { Errors = result.Errors });
        }

    }


}
