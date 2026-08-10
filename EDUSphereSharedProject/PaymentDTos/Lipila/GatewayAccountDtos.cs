using System;
using System.Collections.Generic;

namespace EDUSphereSharedProject.PaymentDTos.Lipila
{
    /// <summary>
    /// A fee bucket paired with whatever Lipila wallet(s) - Sandbox and/or
    /// Production - have been configured for it. Never carries a secret.
    /// </summary>
    public class FeeBucketGatewayDto
    {
        public Guid BucketId { get; set; }

        public string BucketName { get; set; } = default!;

        public List<PaymentGatewayAccountDto> Accounts { get; set; } = new();
    }

    /// <summary>
    /// Read model for a PaymentGatewayAccount. Only ever indicates whether a
    /// credential exists and when it was last set - the encrypted secret and
    /// decrypted API key never leave the server.
    /// </summary>
    public class PaymentGatewayAccountDto
    {
        public Guid Id { get; set; }

        public Guid BucketId { get; set; }

        public string Provider { get; set; } = "Lipila";

        public string WalletId { get; set; } = default!;

        public string Environment { get; set; } = default!;

        public bool IsActive { get; set; }

        public bool HasCredential { get; set; }

        public DateTime? CredentialSetAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class CreatePaymentGatewayAccountRequest
    {
        public Guid SchoolId { get; set; }

        public Guid BucketId { get; set; }

        public string WalletId { get; set; } = default!;

        public string Environment { get; set; } = "Sandbox";
    }

    public class UpdatePaymentGatewayAccountRequest
    {
        public string WalletId { get; set; } = default!;

        public string Environment { get; set; } = default!;

        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Body for setting/rotating a wallet's API key. The plaintext key is only
    /// ever held in memory long enough to encrypt it - never persisted or logged.
    /// </summary>
    public class SetPaymentGatewayCredentialRequest
    {
        public string ApiKey { get; set; } = default!;
    }
}
