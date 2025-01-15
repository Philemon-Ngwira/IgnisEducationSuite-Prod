using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {

        private readonly RolesService _rolesService;

        public RolesController(RolesService rolesService)
        {
           _rolesService = rolesService;
        }

        [HttpGet("GetUserRoles/{userNameOrEmail}")]
        public async Task<IActionResult> GetUserRoles(string userNameOrEmail)
        {
           var result  =  await _rolesService.GetUserRolesAsync(userNameOrEmail);
            return Ok(result);
        }
    }

}
