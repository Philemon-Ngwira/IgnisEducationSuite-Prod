namespace IgnisEducationSuite.ServerServices.Security;

public sealed class EncryptionConfiguration
{
    public string CurrentKeyVersion { get; set; } = string.Empty;

    public Dictionary<string, string> Keys { get; set; } = new();
}