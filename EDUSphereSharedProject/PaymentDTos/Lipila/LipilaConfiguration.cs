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

        /// <summary>
        /// Page the customer's browser is redirected back to after completing
        /// (or abandoning) a hosted card payment. Distinct from CallbackUrl,
        /// which is the server-to-server webhook Lipila posts results to.
        /// </summary>
        public string CardReturnUrl { get; set; } = string.Empty;
    }
}
