using EduSphereDomain.MessagingData;
using EDUSphereSharedProject.ChatModels;
using EDUSphereSharedProject.UniversalModels.Chat;
using Microsoft.EntityFrameworkCore;

namespace IgnisEducationSuite.ServerServices.Chat
{
    /// <summary>
    /// Unread state and history paging for chat.
    ///
    /// Unread is derived, not stored per message: a conversation's unread count is the number of
    /// messages in it newer than the reader's ChatReadState.LastReadAt. That keeps marking a
    /// conversation read to a single upsert, and means no write amplification as message volume
    /// grows.
    /// </summary>
    public class ChatEngagementService
    {
        private const int DefaultPageSize = 50;
        private const int MaxPageSize = 200;

        private readonly MessagingContext _context;

        public ChatEngagementService(MessagingContext context)
        {
            _context = context;
        }

        public async Task<ChatUnreadSummaryDto> GetUnreadSummaryAsync(string userId)
        {
            var summary = new ChatUnreadSummaryDto();
            if (string.IsNullOrWhiteSpace(userId)) return summary;

            var readState = await _context.ChatReadStates
                .Where(r => r.UserId == userId)
                .ToDictionaryAsync(r => r.ConversationKey, r => r.LastReadAt, StringComparer.OrdinalIgnoreCase);

            // ---- Direct conversations ----
            var directMessages = await _context.ChatMessages
                .Where(m => (m.ReciepientId == userId || m.UserId == userId) && m.GroupName == null)
                .Select(m => new
                {
                    m.UserId,
                    m.ReciepientId,
                    m.Message,
                    m.Timestamp,
                })
                .ToListAsync();

            var directGroups = directMessages
                .GroupBy(m => string.Equals(m.UserId, userId, StringComparison.OrdinalIgnoreCase)
                    ? m.ReciepientId
                    : m.UserId);

            foreach (var conversation in directGroups)
            {
                if (string.IsNullOrWhiteSpace(conversation.Key)) continue;

                readState.TryGetValue(conversation.Key, out var lastRead);

                // Only messages FROM the other person can be unread — your own never are.
                var unread = conversation.Count(m =>
                    !string.Equals(m.UserId, userId, StringComparison.OrdinalIgnoreCase) &&
                    m.Timestamp > lastRead);

                var latest = conversation.OrderByDescending(m => m.Timestamp).First();

                summary.Conversations.Add(new ConversationUnreadDto
                {
                    ConversationKey = conversation.Key,
                    IsGroup = false,
                    UnreadCount = unread,
                    LastMessageAt = latest.Timestamp,
                    LastMessagePreview = Preview(latest.Message),
                });
            }

            // ---- Group conversations the user belongs to ----
            var groupNames = await _context.GroupMembers
                .Where(g => g.UserId == userId && g.GroupName != null)
                .Select(g => g.GroupName)
                .Distinct()
                .ToListAsync();

            if (groupNames.Count > 0)
            {
                var groupMessages = await _context.ChatMessages
                    .Where(m => m.GroupName != null && groupNames.Contains(m.GroupName))
                    .Select(m => new { m.GroupName, m.UserId, m.Message, m.Timestamp })
                    .ToListAsync();

                foreach (var groupName in groupNames)
                {
                    var inGroup = groupMessages
                        .Where(m => string.Equals(m.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    readState.TryGetValue(groupName, out var lastRead);

                    var unread = inGroup.Count(m =>
                        !string.Equals(m.UserId, userId, StringComparison.OrdinalIgnoreCase) &&
                        m.Timestamp > lastRead);

                    var latest = inGroup.OrderByDescending(m => m.Timestamp).FirstOrDefault();

                    summary.Conversations.Add(new ConversationUnreadDto
                    {
                        ConversationKey = groupName,
                        IsGroup = true,
                        UnreadCount = unread,
                        LastMessageAt = latest?.Timestamp,
                        LastMessagePreview = Preview(latest?.Message),
                    });
                }
            }

            // Resolve sender display names in one pass rather than per conversation.
            await AttachSenderNamesAsync(summary, userId);

            summary.TotalUnread = summary.Conversations.Sum(c => c.UnreadCount);
            summary.Conversations = summary.Conversations
                .OrderByDescending(c => c.UnreadCount > 0)
                .ThenByDescending(c => c.LastMessageAt)
                .ToList();

            return summary;
        }

        private async Task AttachSenderNamesAsync(ChatUnreadSummaryDto summary, string userId)
        {
            var directKeys = summary.Conversations
                .Where(c => !c.IsGroup)
                .Select(c => c.ConversationKey)
                .ToList();

            if (directKeys.Count == 0) return;

            var names = await _context.AspNetUsers
                .Where(u => directKeys.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName })
                .ToListAsync();

            var nameById = names.ToDictionary(
                n => n.Id,
                n => string.IsNullOrWhiteSpace($"{n.FirstName} {n.LastName}".Trim())
                    ? n.UserName
                    : $"{n.FirstName} {n.LastName}".Trim(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var conversation in summary.Conversations.Where(c => !c.IsGroup))
            {
                if (nameById.TryGetValue(conversation.ConversationKey, out var name))
                {
                    conversation.LastMessageFromName = name;
                }
            }
        }

        /// <summary>
        /// Moves the reader's marker forward. Never backwards: a stale client reporting an older
        /// timestamp must not resurrect messages the user has already seen.
        /// </summary>
        public async Task<bool> MarkConversationReadAsync(string userId, MarkConversationReadRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(request.ConversationKey))
                return false;

            var readUpTo = request.ReadUpTo ?? DateTime.Now;

            var state = await _context.ChatReadStates
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ConversationKey == request.ConversationKey);

            if (state is null)
            {
                state = new ChatReadState
                {
                    ChatReadStateID = Guid.NewGuid(),
                    UserId = userId,
                    ConversationKey = request.ConversationKey,
                    IsGroup = request.IsGroup,
                    LastReadAt = readUpTo,
                    UpdatedAt = DateTime.Now,
                };

                _context.ChatReadStates.Add(state);
            }
            else
            {
                if (readUpTo <= state.LastReadAt) return true;

                state.LastReadAt = readUpTo;
                state.IsGroup = request.IsGroup;
                state.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// One page of history, newest-last. <paramref name="before"/> walks backwards through the
        /// conversation; the previous endpoint loaded every message ever sent, which is fine for a
        /// week and painful after a term.
        /// </summary>
        public async Task<MessagePageDto> GetMessagePageAsync(
            string userId, string conversationKey, bool isGroup, DateTime? before, int pageSize)
        {
            var result = new MessagePageDto();
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(conversationKey)) return result;

            pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

            IQueryable<ChatMessage> query = _context.ChatMessages;

            if (isGroup)
            {
                // Membership is required: without it, knowing a group's name would be enough to read it.
                var isMember = await _context.GroupMembers
                    .AnyAsync(g => g.UserId == userId && g.GroupName == conversationKey);

                if (!isMember) return result;

                query = query.Where(m => m.GroupName == conversationKey);
            }
            else
            {
                query = query.Where(m =>
                    m.GroupName == null &&
                    ((m.UserId == userId && m.ReciepientId == conversationKey) ||
                     (m.UserId == conversationKey && m.ReciepientId == userId)));
            }

            if (before.HasValue)
            {
                query = query.Where(m => m.Timestamp < before.Value);
            }

            // Take newest-first for the page window, then flip so the caller gets chronological order.
            var page = await query
                .OrderByDescending(m => m.Timestamp)
                .Take(pageSize + 1)
                .ToListAsync();

            result.HasMore = page.Count > pageSize;
            var window = page.Take(pageSize).OrderBy(m => m.Timestamp).ToList();

            var senderNames = await ResolveNamesAsync(window.Select(m => m.UserId));

            result.Messages = window.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Content = m.Message ?? "",
                UserId = m.UserId ?? "",
                RecipientId = m.ReciepientId ?? "",
                GroupName = m.GroupName ?? "",
                GroupIdentifier = m.GroupIdentifier ?? "",
                Timestamp = m.Timestamp,
                SenderName = m.UserId is not null && senderNames.TryGetValue(m.UserId, out var name) ? name : null,
                Attachment = string.IsNullOrWhiteSpace(m.AttachmentPath) ? null : new ChatAttachmentDto
                {
                    Name = m.AttachmentName,
                    ContentType = m.AttachmentContentType,
                    SizeBytes = m.AttachmentSizeBytes,
                },
            }).ToList();

            result.OldestTimestamp = window.FirstOrDefault()?.Timestamp;

            return result;
        }

        private async Task<Dictionary<string, string>> ResolveNamesAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var users = await _context.AspNetUsers
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName })
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => string.IsNullOrWhiteSpace($"{u.FirstName} {u.LastName}".Trim())
                    ? u.UserName ?? ""
                    : $"{u.FirstName} {u.LastName}".Trim(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string? Preview(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            var trimmed = content.Trim();
            return trimmed.Length <= 80 ? trimmed : trimmed[..80] + "…";
        }
    }
}
