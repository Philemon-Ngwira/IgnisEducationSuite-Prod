using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly LessonMediaService _lessonMediaService;

        public MediaController(LessonMediaService lessonMediaService)
        {
            _lessonMediaService = lessonMediaService;
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var urls = new List<string>();

            foreach (var file in files)
            {
                var url = await _lessonMediaService.SaveImageAsync(file);
                urls.Add(url);
            }

            return Ok(urls);
        }

    }
}
