using System;
using System.Collections.Generic;

namespace EDUSphereSharedProject.UniversalModels.Chat
{
    /// <summary>
    /// Unread totals for the signed-in user. Drives the navigation badge, which is what makes chat
    /// discoverable at all — previously a message was invisible unless you happened to open the chat
    /// page.
    /// </summary>
    public class ChatUnreadSummaryDto
    {
        public int TotalUnread { get; set; }

        /// <summary>Per-conversation counts, keyed the same way as ChatReadState.ConversationKey:
        /// the other participant's user id for a direct message, the group name for a group.</summary>
        public List<ConversationUnreadDto> Conversations { get; set; } = new();
    }

    public class ConversationUnreadDto
    {
        public string ConversationKey { get; set; } = "";
        public bool IsGroup { get; set; }
        public int UnreadCount { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? LastMessagePreview { get; set; }
        public string? LastMessageFromName { get; set; }
    }

    public class MarkConversationReadRequest
    {
        public string ConversationKey { get; set; } = "";
        public bool IsGroup { get; set; }

        /// <summary>Null marks everything up to now as read.</summary>
        public DateTime? ReadUpTo { get; set; }
    }

    /// <summary>A page of conversation history, newest-last, requested oldest-ward.</summary>
    public class MessagePageDto
    {
        public List<ChatMessageDto> Messages { get; set; } = new();

        /// <summary>Timestamp to pass as <c>before</c> to fetch the previous page.</summary>
        public DateTime? OldestTimestamp { get; set; }

        public bool HasMore { get; set; }
    }

    /// <summary>
    /// The message shape used across the wire. Distinct from the ChatMessage entity, whose property
    /// names do not match (Message vs Content, ReciepientId vs RecipientId) — sending the entity
    /// directly is what broke realtime delivery previously.
    /// </summary>
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = "";
        public string UserId { get; set; } = "";
        public string RecipientId { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string GroupIdentifier { get; set; } = "";
        public DateTime? Timestamp { get; set; }

        /// <summary>Display name of the sender, resolved server-side so group chats can show who
        /// spoke without the client holding every user.</summary>
        public string? SenderName { get; set; }

        public ChatAttachmentDto? Attachment { get; set; }
    }

    public class ChatAttachmentDto
    {
        public string? Name { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }

        /// <summary>Short-lived read URL, minted per request. The stored value is a blob path, so a
        /// copied link stops working rather than granting permanent access.</summary>
        public string? Url { get; set; }

        public bool IsImage =>
            !string.IsNullOrEmpty(ContentType) && ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Groups built from the school's own structure ----------

    /// <summary>
    /// A candidate group a teacher or admin can create in one step, derived from classes and
    /// enrolment rather than assembled by hand. "Form 1 B — Parents" is the case schools actually
    /// want: one message to every parent in a section.
    /// </summary>
    public class GroupSuggestionDto
    {
        public string GroupName { get; set; } = "";
        public string Description { get; set; } = "";
        public GroupAudience Audience { get; set; }
        public int? AcademicLevel { get; set; }
        public string? LevelName { get; set; }
        public string? GradeSection { get; set; }
        public Guid? ClassId { get; set; }

        /// <summary>How many user accounts would be added. Zero means nobody in that audience has a
        /// login yet, so the group would be created empty — worth saying before it is created.</summary>
        public int MemberCount { get; set; }

        public bool AlreadyExists { get; set; }
    }

    public enum GroupAudience
    {
        /// <summary>Parents of the students in a level/section.</summary>
        SectionParents = 0,

        /// <summary>The students in a level/section.</summary>
        SectionStudents = 1,

        /// <summary>Teachers who teach a level/section.</summary>
        SectionTeachers = 2,

        /// <summary>Parents of the students enrolled in one specific class.</summary>
        ClassParents = 3,
    }

    public class CreateGroupRequest
    {
        public string GroupName { get; set; } = "";
        public GroupAudience Audience { get; set; }
        public int? AcademicLevel { get; set; }
        public string? GradeSection { get; set; }
        public Guid? ClassId { get; set; }
    }

    public class CreateGroupResult
    {
        public bool Succeeded { get; set; }
        public Guid? GroupId { get; set; }
        public string? GroupName { get; set; }
        public int MembersAdded { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class GroupSummaryDto
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = "";
        public DateTime? CreatedDate { get; set; }
        public int MemberCount { get; set; }
    }

    // ---------- Safeguarding / audit ----------

    public class ChatAuditQuery
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        /// <summary>Free text across sender name, recipient name and message content.</summary>
        public string? Search { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; } = 50;
    }

    public class ChatAuditResult
    {
        public List<ChatAuditRow> Rows { get; set; } = new();
        public int TotalCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ChatAuditRow
    {
        public Guid Id { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? SenderName { get; set; }
        public string? SenderRole { get; set; }
        public string? RecipientName { get; set; }
        public string? RecipientRole { get; set; }
        public string? GroupName { get; set; }
        public string? Content { get; set; }
        public bool HasAttachment { get; set; }
        public string? AttachmentName { get; set; }
    }
}
