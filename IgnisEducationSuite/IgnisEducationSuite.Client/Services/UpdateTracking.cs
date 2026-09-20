using Microsoft.JSInterop;

namespace IgnisEducationSuite.Client.Services
{
    /// <summary>
    /// Whether this browser has seen the current release notes.
    ///
    /// Stored in localStorage rather than against the user record: it needs no schema, and "have I
    /// read this" is a per-person convenience, not data the school needs to keep. The cost is that
    /// it resets on a new browser, which for a "what's new" dot is the right trade.
    /// </summary>
    public static class UpdateTracking
    {
        public const string LastSeenVersionKey = "ignis-last-seen-version";

        /// <summary>
        /// True when the running version differs from whatever this browser last read.
        ///
        /// Never throws. Storage can be unavailable (a private window, a browser blocking site
        /// data), and during prerendering there is no browser at all — in every such case this
        /// returns false, so the worst outcome is a dot that does not appear rather than an error on
        /// the page.
        /// </summary>
        public static async Task<bool> HasUnseenUpdateAsync(IJSRuntime js, string currentVersion)
        {
            try
            {
                var seen = await js.InvokeAsync<string?>("localStorage.getItem", LastSeenVersionKey);
                return !string.Equals(seen, currentVersion, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public static async Task MarkSeenAsync(IJSRuntime js, string currentVersion)
        {
            try
            {
                await js.InvokeVoidAsync("localStorage.setItem", LastSeenVersionKey, currentVersion);
            }
            catch
            {
                // Nothing to do — the dot stays until next time.
            }
        }
    }
}
