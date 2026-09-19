namespace MailForge.Studio.Web.Pages;

/// <summary>JSON API endpoints for message list, detail, attachments, export, replay, and raw MIME.</summary>
public static class MessagesApi
{
    /// <summary>Returns a paginated message list as JSON.</summary>
    public static async Task<IResult> List(HttpContext http, [AsParameters] StudioMessageQuery query, IStudioCaptureStore store)
    {
        var page = await store.QueryAsync(query);
        var items = page.Items.Select(MessageMapper.ToListDto).ToList();
        return Results.Json(new
        {
            items,
            page.TotalCount,
            page.Page,
            page.PageSize,
            page.TotalPages,
        });
    }

    /// <summary>Returns a single message detail as JSON.</summary>
    public static async Task<IResult> Get(Guid id, IStudioCaptureStore store)
    {
        var message = await store.GetByIdAsync(id);
        return message is null
            ? Results.NotFound()
            : Results.Json(MessageMapper.ToDetailDto(message));
    }

    /// <summary>Returns a single message as the specified format (eml, html, json).</summary>
    public static async Task<IResult> Export(Guid id, string format, IStudioCaptureStore store)
    {
        var exporter = new StudioMimeExporter(store);
        var result = await exporter.ExportAsync(id, format);
        return result is null
            ? Results.NotFound()
            : Results.Bytes(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>Returns the regenerated raw MIME content for a message.</summary>
    public static async Task<IResult> Raw(Guid id, IStudioCaptureStore store)
    {
        var message = await store.GetByIdAsync(id);
        if (message is null) return Results.NotFound();

        var mime = MimeMessageConverter.ToMimeMessage(message);
        if (mime is null) return Results.Text("Unable to regenerate MIME content", "text/plain");

        using var ms = new MemoryStream();
        mime.WriteTo(ms);
        return Results.Bytes(ms.ToArray(), "message/rfc822", $"{SanitizeFileName(message.Subject ?? "message")}.eml");
    }

    /// <summary>Downloads an attachment by its id.</summary>
    public static async Task<IResult> Attachment(Guid id, Guid attachmentId, IStudioCaptureStore store)
    {
        var attachment = await store.GetAttachmentAsync(attachmentId);
        if (attachment is null || attachment.MessageId != id)
            return Results.NotFound();

        var contentType = string.IsNullOrWhiteSpace(attachment.MediaType)
            ? "application/octet-stream"
            : attachment.MediaType;

        return Results.Bytes(attachment.Content ?? [], contentType, attachment.FileName ?? "attachment");
    }

    /// <summary>Replays a captured message via SMTP.</summary>
    public static async Task<IResult> Replay(Guid id, MessageReplayer replayer)
    {
        var result = await replayer.ReplayAsync(id);
        return Results.Json(result);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 80 ? name[..80] : name;
    }
}
