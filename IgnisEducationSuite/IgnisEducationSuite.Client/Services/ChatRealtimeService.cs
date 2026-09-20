using System.Net.Http.Json;
using EDUSphereSharedProject.IdentitySharedModels;
using EDUSphereSharedProject.UniversalModels.Chat;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;

namespace IgnisEducationSuite.Client.Services
{
    /// <summary>
    /// Owns the chat hub connection for the whole app session.
    ///
    /// Previously the connection was built inside ChatPage, so it existed only while that page was
    /// open — navigate away and realtime stopped, and nothing anywhere else in the app could know a
    /// message had arrived. Hosting it here means messages are delivered on any page, which is what
    /// makes the unread badge and notifications possible at all.
    ///
    /// Registered scoped: in a Blazor WebAssembly app that is the lifetime of the browser session,
    /// so the connection is shared by every component that asks for it.
    /// </summary>
    public class ChatRealtimeService : IAsyncDisposable
    {
        private readonly HttpClient _http;
        private readonly NavigationManager _navigation;

        private HubConnection? _connection;
        private bool _starting;
        private string? _currentUserId;

        public ChatRealtimeService(HttpClient http, NavigationManager navigation)
        {
            _http = http;
            _navigation = navigation;
        }

        /// <summary>Latest unread state. Components read this rather than each fetching their own.</summary>
        public ChatUnreadSummaryDto Unread { get; private set; } = new();

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        /// <summary>Null until the identity check has run; false means the hub could not identify
        /// this connection, so direct messages cannot be delivered to this user.</summary>
        public bool? HubIdentityResolved { get; private set; }

        /// <summary>Raised for every message that arrives, on any page.</summary>
        public event Func<ChatMessageDto, Task>? MessageReceived;

        /// <summary>Raised whenever unread totals change, so badges can re-render.</summary>
        public event Action? UnreadChanged;

        /// <summary>Raised on connect/disconnect so pages can show connection state.</summary>
        public event Action? ConnectionStateChanged;

        /// <summary>
        /// Safe to call repeatedly — from a layout on every navigation, for example. Only the first
        /// call builds and starts the connection.
        /// </summary>
        public async Task EnsureStartedAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;
            if (_starting) return;
            if (_connection is not null && _connection.State != HubConnectionState.Disconnected) return;

            _starting = true;
            _currentUserId = userId;

            try
            {
                _connection ??= BuildConnection();

                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                }

                ConnectionStateChanged?.Invoke();

                await VerifyIdentityAsync();
                await JoinGroupsAsync();
                await RefreshUnreadAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat hub could not start: {ex.Message}");
                ConnectionStateChanged?.Invoke();
            }
            finally
            {
                _starting = false;
            }
        }

        private HubConnection BuildConnection()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(_navigation.ToAbsoluteUri("/chathub"))
                .WithAutomaticReconnect()
                .Build();

            connection.On<ChatMessageDto>("ReceiveMessage", async incoming =>
            {
                if (incoming is null) return;

                // Anything from someone else may change unread totals, so refresh before telling
                // listeners — a page reacting to the message will then see correct counts.
                if (!string.Equals(incoming.UserId, _currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    await RefreshUnreadAsync();
                }

                if (MessageReceived is not null)
                {
                    await MessageReceived.Invoke(incoming);
                }
            });

            connection.Reconnecting += _ =>
            {
                ConnectionStateChanged?.Invoke();
                return Task.CompletedTask;
            };

            // Group membership is tied to the connection id, so it is lost on a drop and must be
            // re-established. Unread is refreshed too, since anything sent while disconnected was
            // never pushed to this client.
            connection.Reconnected += async _ =>
            {
                ConnectionStateChanged?.Invoke();
                await JoinGroupsAsync();
                await RefreshUnreadAsync();
            };

            connection.Closed += _ =>
            {
                ConnectionStateChanged?.Invoke();
                return Task.CompletedTask;
            };

            return connection;
        }

        /// <summary>
        /// Confirms SignalR resolved this connection to the same account the app is using. Direct
        /// messages route on that value, so if it is missing they are saved but never delivered —
        /// a failure mode otherwise indistinguishable from "realtime is broken".
        /// </summary>
        private async Task VerifyIdentityAsync()
        {
            if (_connection is null || _connection.State != HubConnectionState.Connected) return;

            try
            {
                var hubUserId = await _connection.InvokeAsync<string?>("WhoAmI");

                HubIdentityResolved = !string.IsNullOrWhiteSpace(hubUserId)
                                      && string.Equals(hubUserId, _currentUserId, StringComparison.OrdinalIgnoreCase);

                if (HubIdentityResolved != true)
                {
                    Console.WriteLine($"Chat identity mismatch. Hub sees '{hubUserId ?? "(none)"}', app uses '{_currentUserId}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not verify chat identity: {ex.Message}");
            }
        }

        public async Task JoinGroupsAsync()
        {
            if (_connection is null || _connection.State != HubConnectionState.Connected) return;

            foreach (var group in Unread.Conversations.Where(c => c.IsGroup))
            {
                await JoinGroupAsync(group.ConversationKey);
            }
        }

        public async Task JoinGroupAsync(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            if (_connection is null || _connection.State != HubConnectionState.Connected) return;

            try
            {
                await _connection.InvokeAsync("AddToGroup", groupName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not join chat group '{groupName}': {ex.Message}");
            }
        }

        public async Task RefreshUnreadAsync()
        {
            try
            {
                var summary = await _http.GetFromJsonAsync<ChatUnreadSummaryDto>("api/ChatEngagement/unread");
                Unread = summary ?? new ChatUnreadSummaryDto();
                UnreadChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not refresh unread chat counts: {ex.Message}");
            }
        }

        /// <summary>Clears the badge for one conversation and tells the server how far was read.</summary>
        public async Task MarkReadAsync(string conversationKey, bool isGroup)
        {
            if (string.IsNullOrWhiteSpace(conversationKey)) return;

            try
            {
                await _http.PostAsJsonAsync("api/ChatEngagement/mark-read", new MarkConversationReadRequest
                {
                    ConversationKey = conversationKey,
                    IsGroup = isGroup,
                });

                // Update locally so the badge clears immediately rather than after a round trip.
                var conversation = Unread.Conversations
                    .FirstOrDefault(c => string.Equals(c.ConversationKey, conversationKey, StringComparison.OrdinalIgnoreCase));

                if (conversation is not null)
                {
                    Unread.TotalUnread = Math.Max(0, Unread.TotalUnread - conversation.UnreadCount);
                    conversation.UnreadCount = 0;
                    UnreadChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not mark conversation read: {ex.Message}");
            }
        }

        public int UnreadFor(string conversationKey) =>
            Unread.Conversations
                .FirstOrDefault(c => string.Equals(c.ConversationKey, conversationKey, StringComparison.OrdinalIgnoreCase))
                ?.UnreadCount ?? 0;

        /// <summary>Sends a direct message, returning only once the hub has stored it.</summary>
        public Task SendDirectAsync(string recipientId, Message message) =>
            InvokeAsync("SendMessageToUser", recipientId, message);

        public Task SendGroupAsync(string groupName, Message message) =>
            InvokeAsync("SendMessageToGroup", groupName, message);

        private Task InvokeAsync(string method, string target, Message message)
        {
            if (_connection is null || _connection.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Not connected to the chat service.");
            }

            // InvokeAsync, not SendAsync: SendAsync completes even when the hub method throws, so a
            // failed save is indistinguishable from a successful one.
            return _connection.InvokeAsync(method, target, message);
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
