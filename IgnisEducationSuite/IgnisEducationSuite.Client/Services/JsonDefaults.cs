using System.Text.Json;

namespace IgnisEducationSuite.Client.Services;

/// <summary>
/// Shared JSON options for talking to our own API from Razor components.
/// System.Net.Http.Json's parameterless overloads are not guaranteed
/// case-insensitive, and our controllers serialize with camelCase - always
/// pass this explicitly rather than relying on undocumented defaults.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
