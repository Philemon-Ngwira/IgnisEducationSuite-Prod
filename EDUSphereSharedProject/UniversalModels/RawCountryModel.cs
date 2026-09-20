using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class RawCountryModel
    {
        public Name Name { get; set; } = new();

        public string Cca2 { get; set; } = string.Empty;
        public string Cca3 { get; set; } = string.Empty;
        public Dictionary<string, CurrencyDetail> Currencies { get; set; }
    }

    public class Name
    {
        public string Common { get; set; } = string.Empty;
    }

    public class CurrencyDetail
    {
        public string ISOCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;

    }
}
