using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.PaymentDTos.Lipila
{
    public sealed class LipilaConfiguration
    {
        public string Environment { get; set; } = "Sandbox";

        public string SandboxBaseUrl { get; set; }
            = "https://api.lipila.dev";

        public string ProductionBaseUrl { get; set; }
            = "https://blz.lipila.io";

        public string CallbackUrl { get; set; } = string.Empty;
    }
}
