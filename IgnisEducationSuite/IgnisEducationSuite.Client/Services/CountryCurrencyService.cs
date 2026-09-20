using EDUSphereSharedProject.UniversalModels;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Services
{
    public class CountryCurrencyService
    {
        private readonly HttpClient _http;

        public CountryCurrencyService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<CountryInfo>> GetAllCountriesWithCurrencyAsync()
        {
            try
            {
                // Fetch only the fields we need
                var response = await _http.GetFromJsonAsync<List<RawCountryModel>>(
                    "https://restcountries.com/v3.1/all?fields=name,cca2,cca3,currencies"
                );

                if (response == null)
                    return new List<CountryInfo>();

                var countryList = response
                    .Where(c => c.Name?.Common != null && c.Currencies != null && c.Currencies.Count > 0)
                    .Select(c =>
                    {
                        // Take the first currency (most countries have only one)
                        var currencyEntry = c.Currencies.First();
                        return new CountryInfo
                        {
                            Name = c.Name.Common,
                            CurrencyCode = currencyEntry.Key ?? string.Empty,
                            CurrencyName = currencyEntry.Value?.Name ?? string.Empty,
                            CurrencySymbol = currencyEntry.Value?.Symbol ?? string.Empty,
                            CountryCode2 = c.Cca2 ?? string.Empty,
                            CountryCode3 = c.Cca3 ?? string.Empty
                        };
                    })
                    .OrderBy(c => c.Name)
                    .ToList();

                return countryList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch countries: {ex.Message}");
                return new List<CountryInfo>();
            }
        }
    }
}
