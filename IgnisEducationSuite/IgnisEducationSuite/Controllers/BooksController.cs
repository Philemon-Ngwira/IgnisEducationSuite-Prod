using IgnisEducationSuite.ServerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly GoogleBooksService _booksService;

        public BooksController(GoogleBooksService booksService)
        {
            _booksService = booksService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query)
        {
            var apiKey = "AIzaSyC4oNUZn-s28H3IgQRL5c_0LPDpSX1KkxI"; // Replace with your actual API key
            var result = await _booksService.SearchBooksAsync(query, apiKey);

            if (result == null)
                return NotFound("No books found.");

            return Ok(result);
        }
        //High School Textbook
        [HttpGet("default/{query}")]
        public async Task<IActionResult> GetDefaultBooks(string query)
        {
            var apiKey = "AIzaSyC4oNUZn-s28H3IgQRL5c_0LPDpSX1KkxI"; // Replace with your actual API key
            var defaultQuery = query; // Use a generic query or popular category
            var result = await _booksService.SearchBooksAsync(defaultQuery, apiKey);

            if (result == null)
                return NotFound("No books found.");

            return Ok(result);
        }

    }
}
