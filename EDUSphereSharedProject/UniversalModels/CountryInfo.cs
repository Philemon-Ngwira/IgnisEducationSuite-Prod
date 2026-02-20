using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class CountryInfo
    {
        public string Name { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;
        public string IsoCode { get; set; } = string.Empty;

        public string CountryCode3 { get; set; } = string.Empty;
        public string CountryCode2 { get; set; } = string.Empty;
    }
}
