using System.Text.Json;
using MailForge.Studio.Capture.Entities;

namespace MailForge.Studio.Web;

/// <summary>Summary data for a captured message in a list view.</summary>
public class MessageListDto
{
    /// <summary>The message id.</summary>
    public Guid Id { get; init; }

    /// <summary>The displayed sender (From header).</summary>
    public string? From { get; init; }

    /// <summary>The message subject.</summary>
    public string? Subject { get; init; }

    /// <summary>The message importance level.</summary>
    public string? Priority { get; init; }

    /// <summary>The time at which the message was captured.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The number of recipients on the message.</summary>
    public int RecipientCount { get; init; }

    /// <summary>The number of non-inline attachments on the message.</summary>
    public int AttachmentCount { get; init; }
}

/// <summary>Full detail for a captured message, including bodies, recipients, and attachments.</summary>
public sealed class MessageDetailDto : MessageListDto
{
    /// <summary>The HTML body.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>The plain-text body.</summary>
    public string? TextBody { get; init; }

    /// <summary>The MailForge message id.</summary>
    public string? MessageId { get; init; }

    /// <summary>The message recipients.</summary>
    public List<RecipientDto> Recipients { get; init; } = [];

    /// <summary>The message attachments and inline images.</summary>
    public List<AttachmentDto> Attachments { get; init; } = [];

    /// <summary>The custom message headers.</summary>
    public List<HeaderDto> Headers { get; init; } = [];

    /// <summary>Serialized JSON metadata for the message model.</summary>
    public string? ModelJson { get; init; }
}

/// <summary>A recipient and its role on a message.</summary>
public sealed class RecipientDto
{
    /// <summary>The role of this recipient (To, Cc, or Bcc).</summary>
    public string Type { get; init; } = "";

    /// <summary>The recipient's address.</summary>
    public string Address { get; init; } = "";

    /// <summary>An optional display name.</summary>
    public string? DisplayName { get; init; }
}

/// <summary>Metadata for a captured attachment or inline image.</summary>
public sealed class AttachmentDto
{
    /// <summary>The attachment id.</summary>
    public Guid Id { get; init; }

    /// <summary>The file name of the attachment.</summary>
    public string? FileName { get; init; }

    /// <summary>The MIME media type.</summary>
    public string? MediaType { get; init; }

    /// <summary>True when the attachment is an inline image referenced via a content id (cid:).</summary>
    public bool IsInline { get; init; }

    /// <summary>The attachment content size in bytes.</summary>
    public int Size { get; init; }
}

/// <summary>A captured message header.</summary>
public sealed class HeaderDto
{
    /// <summary>The header name.</summary>
    public string Name { get; init; } = "";

    /// <summary>The header value.</summary>
    public string Value { get; init; } = "";
}

/// <summary>A request to replay a captured message.</summary>
public sealed class ReplayRequest
{
    /// <summary>Optional recipient overrides; when null the original recipients are used.</summary>
    public List<string>? OverrideRecipients { get; init; }
}

/// <summary>The outcome of a replay request.</summary>
public sealed class ReplayResult
{
    /// <summary>True when the replay succeeded.</summary>
    public bool Succeeded { get; init; }

    /// <summary>An optional success or error message.</summary>
    public string? Message { get; init; }
}

/// <summary>Maps captured message entities to their JSON DTOs.</summary>
public static class MessageMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Builds a <see cref="MessageListDto"/> from a captured message.</summary>
    public static MessageListDto ToListDto(CapturedMessage m) => new()
    {
        Id = m.Id,
        From = m.From,
        Subject = m.Subject,
        Priority = m.Priority.ToString(),
        CreatedAt = m.CreatedAt,
        RecipientCount = m.Recipients?.Count ?? 0,
        AttachmentCount = m.Attachments?.Count(a => !a.IsInline) ?? 0,
    };

    /// <summary>Builds a <see cref="MessageDetailDto"/> from a captured message.</summary>
    public static MessageDetailDto ToDetailDto(CapturedMessage m) => new()
    {
        Id = m.Id,
        From = m.From,
        Subject = m.Subject,
        Priority = m.Priority.ToString(),
        CreatedAt = m.CreatedAt,
        MessageId = m.MessageId,
        HtmlBody = m.HtmlBody,
        TextBody = m.TextBody,
        RecipientCount = m.Recipients?.Count ?? 0,
        AttachmentCount = m.Attachments?.Count(a => !a.IsInline) ?? 0,
        Recipients = m.Recipients?.Select(r => new RecipientDto
        {
            Type = r.Type.ToString(),
            Address = r.Address ?? "",
            DisplayName = r.DisplayName,
        }).ToList() ?? [],
        Attachments = m.Attachments?.Select(a => new AttachmentDto
        {
            Id = a.Id,
            FileName = a.FileName,
            MediaType = a.MediaType,
            IsInline = a.IsInline,
            Size = a.Content?.Length ?? 0,
        }).ToList() ?? [],
        Headers = m.Headers?.Select(h => new HeaderDto
        {
            Name = h.Name ?? "",
            Value = h.Value ?? "",
        }).ToList() ?? [],
        ModelJson = JsonSerializer.Serialize(new
        {
            m.MessageId,
            m.From,
            m.Subject,
            m.Priority,
            m.CreatedAt,
            m.EnvelopeFrom,
        }, JsonOptions),
    };
}