
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
            if(user == null)
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

    }


}
