using System;

namespace EDUSphereSharedProject.ChatModels;

/// <summary>
/// How far a user has read in one conversation.
///
/// Deliberately one row per (user, conversation) rather than a read flag per message: marking a
/// conversation read is a single upsert instead of an update across every unread row, and the table
/// grows with conversations rather than with message volume. Unread counts are then "messages newer
/// than LastReadAt", which is an indexed range scan.
/// </summary>
public partial class ChatReadState
{
    public Guid ChatReadStateID { get; set; }

    public string UserId { get; set; }

    /// <summary>
    /// The other participant's user id for a direct message, or the group name for a group chat.
    /// </summary>
    public string ConversationKey { get; set; }

    public bool IsGroup { get; set; }

    public DateTime LastReadAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
