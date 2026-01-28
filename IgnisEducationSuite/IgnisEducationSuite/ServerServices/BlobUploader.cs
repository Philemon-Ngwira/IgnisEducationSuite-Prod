using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace IgnisEducationSuite.ServerServices
{
    public class BlobUploader
    {
        private readonly IConfiguration _configuration;
        private BlobServiceClient? _blobServiceClient;
        private readonly object _lock = new();

        public BlobUploader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // -----------------------------
        // Core client bootstrap
        // -----------------------------
        private BlobServiceClient GetServiceClient()
        {
            if (_blobServiceClient != null)
                return _blobServiceClient;

            lock (_lock)
            {
                if (_blobServiceClient != null)
                    return _blobServiceClient;

                var connectionString = _configuration["AzureBlobStorageConnectionString"];

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    // Local / dev
                    _blobServiceClient = new BlobServiceClient(connectionString);
                }
                else
                {
                    // Production (Managed Identity)
                    var accountName = _configuration["AzureBlobStorageAccountName"]
                        ?? throw new InvalidOperationException("AzureBlobStorageAccountName not configured");

                    var serviceUri = new Uri($"https://{accountName}.blob.core.windows.net");
                    _blobServiceClient = new BlobServiceClient(serviceUri, new DefaultAzureCredential());
                }
            }

            return _blobServiceClient!;
        }

        // -----------------------------
        // Container access
        // -----------------------------
        private BlobContainerClient GetContainer(string containerName)
        {
            var serviceClient = GetServiceClient();
            var container = serviceClient.GetBlobContainerClient(containerName.ToLower());

            container.CreateIfNotExists(PublicAccessType.None);

            return container;
        }

        // -----------------------------
        // Upload
        // -----------------------------
        public async Task<string> UploadFileAsync(
            Stream stream,
            string blobPath,
            string containerName)
        {
            var container = GetContainer(containerName);
            var blob = container.GetBlobClient(blobPath);

            await blob.UploadAsync(stream, overwrite: true);

            return blob.Uri.ToString();
        }

        // -----------------------------
        // Generate read-only SAS
        // -----------------------------
        public string GenerateReadSas(
            string containerName,
            string blobPath,
            TimeSpan? lifetime = null)
        {
            var container = GetContainer(containerName);
            var blob = container.GetBlobClient(blobPath);

            var expires = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(10));

            // Local dev (connection string)
            var connectionString = _configuration["AzureBlobStorageConnectionString"];
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                var serviceClient = new BlobServiceClient(connectionString);
                var accountName = serviceClient.AccountName;

                var key = connectionString
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .First(x => x.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase))
                    .Split('=')[1];

                var credential = new StorageSharedKeyCredential(accountName, key);

                var sas = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobPath,
                    Resource = "b",
                    ExpiresOn = expires
                };

                sas.SetPermissions(BlobSasPermissions.Read);

                return $"{blob.Uri}?{sas.ToSasQueryParameters(credential)}";
            }

            // Production (Managed Identity)
            var delegationKey = GetServiceClient()
                .GetUserDelegationKey(DateTimeOffset.UtcNow, expires);

            var builder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobPath,
                Resource = "b",
                ExpiresOn = expires
            };

            builder.SetPermissions(BlobSasPermissions.Read);

            return $"{blob.Uri}?{builder.ToSasQueryParameters(delegationKey, GetServiceClient().AccountName)}";
        }
    }
}
