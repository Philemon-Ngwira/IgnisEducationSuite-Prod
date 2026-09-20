using System.Security.Claims;
using EDUSphereSharedProject.UniversalModels.Chat;
using IgnisEducationSuite.ServerServices.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    /// <summary>
    /// Unread state and paged history for chat. The user is taken from the identity cookie, never
    /// from the request — otherwise anyone could read another person's unread counts, or mark their
    /// conversations as read.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatEngagementController : ControllerBase
    {
        private readonly ChatEngagementService _service;
        private readonly ILogger<ChatEngagementController> _logger;

        public ChatEngagementController(ChatEngagementService service, ILogger<ChatEngagementController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            try
            {
                return Ok(await _service.GetUnreadSummaryAsync(userId));
            }
            catch (Exception ex)
            {
                // A failed badge must never break the page it is rendered on.
                _logger.LogError(ex, "Failed to load unread chat summary");
                return Ok(new ChatUnreadSummaryDto());
            }
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] MarkConversationReadRequest request)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(await _service.MarkConversationReadAsync(userId, request));
        }

        /// <summary>
        /// A page of history, oldest-ward. Omit <paramref name="before"/> for the most recent page,
        /// then pass the returned OldestTimestamp to walk back.
        /// </summary>
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages(
            [FromQuery] string conversationKey,
            [FromQuery] bool isGroup = false,
            [FromQuery] DateTime? before = null,
            [FromQuery] int pageSize = 50)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();

            return Ok(await _service.GetMessagePageAsync(userId, conversationKey, isGroup, before, pageSize));
        }
    }
}
