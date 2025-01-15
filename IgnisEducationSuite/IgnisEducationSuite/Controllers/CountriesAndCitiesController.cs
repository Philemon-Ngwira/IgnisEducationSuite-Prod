using EduSphereDomain.Repositories;
using EDUSphereSharedProject.UniversalModels;
using Microsoft.AspNetCore.Mvc;
using RESTCountries.NET.Services;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesAndCitiesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private readonly EduSphereRepository _repository;
        public CountriesAndCitiesController(HttpClient httpClient, IConfiguration configuration, EduSphereRepository eduSphere)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _repository = eduSphere;
        }

        [HttpGet("GetAllCountries")]
        public IActionResult GetCountries()
        {
            var rawCountries = RestCountriesService.GetAllCountries();
            var countries = rawCountries.Select(country => new CountryDTO
            {
                CountryName = country.Name.Common.ToString(),
                CountryCode = country.Cca2.ToString(),
            }).ToList();

            // Return the list of countries
            return Ok(countries);
        }
        [HttpGet("GetCities/{placeId}")]
        public async Task<IActionResult> GetCities(string placeId)
        {
            var result = await _repository.GetCitiesAsync(placeId);
            return Ok(result);
        }
    }

   
}
