using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly string _imageFolderPath;

        public ImageController(IWebHostEnvironment env)
        {
            _imageFolderPath = Path.Combine(env.WebRootPath, "images", "ClassImages");
        }

        [HttpGet("random")]
        public IActionResult GetRandomImage()
        {
            var imageFiles = Directory.GetFiles(_imageFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                                      .Where(file => file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                                      .ToArray();

            if (imageFiles.Length == 0)
            {
                return NotFound("No images found.");
            }

            var random = new Random();
            var randomImage = imageFiles[random.Next(imageFiles.Length)];
            var imageUrl = $"/images/ClassImages/{Path.GetFileName(randomImage)}";

            return Ok(new { Url = imageUrl });
        }
    }
}
