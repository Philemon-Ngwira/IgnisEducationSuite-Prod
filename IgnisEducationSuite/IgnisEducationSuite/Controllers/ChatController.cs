using EduSphereDomain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatGPTService _chatGPTService;

        public ChatController(ChatGPTService chatGPTService)
        {
            _chatGPTService = chatGPTService;
        }

        [HttpPost("GetResponse")]
        public async Task<IActionResult> GetResponse([FromBody] string userMessage)
        {
            var response = await _chatGPTService.GetChatResponseAsync(userMessage);
            return Ok(response);
        }

        [HttpGet("AskChatGPT/{userMessage}")]
        public async Task<IActionResult> AskGPT( string userMessage)
        {
              var res = await _chatGPTService.GetChatResult(userMessage);
            return Ok(res);
        }
    }
}
