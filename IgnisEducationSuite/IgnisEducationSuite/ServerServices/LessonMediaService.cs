using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;

namespace IgnisEducationSuite.ServerServices
{
    public class LessonMediaService
    {
        // "AzureBlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=philtiarastorageblob;AccountKey=YeT3tMBqI1L9jwE6HQSdFJV9yVCLVXNceEPkl2glga56q0L+teqbAXMijDn7LrjQtM2rW1VVvmGQ+AStZmexPw==;EndpointSuffix=core.windows.net",

        private readonly string _connectionString;
        private readonly string _containerName = "ignismedia";

        public LessonMediaService()
        {
            _connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("Azure Storage connection string not found in environment variables.");
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            try
            {
                var containerClient = new BlobContainerClient(_connectionString, _containerName);
                await containerClient.CreateIfNotExistsAsync();

                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var blobClient = containerClient.GetBlobClient(fileName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                // ✅ Generate SAS URL
                if (blobClient.CanGenerateSasUri)
                {
                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = _containerName,
                        BlobName = fileName,
                        Resource = "b", // b = blob
                        ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // Expires in 1 hour
                    };

                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                    return sasUri.ToString(); // Secure URL
                }
                else
                {
                    throw new InvalidOperationException("Cannot generate SAS URI. Ensure the BlobClient is authorized with a credential capable of signing.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
