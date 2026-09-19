using System.Text;
using System.Text.Json;

namespace MailForge.Studio.Web;

/// <summary>Exports captured messages as EML, HTML, or JSON files.</summary>
public sealed class StudioMimeExporter
{
    private readonly IStudioCaptureStore _store;

    /// <summary>Initialises a new <see cref="StudioMimeExporter"/>.</summary>
    public StudioMimeExporter(IStudioCaptureStore store)
    {
        _store = store;
    }

    /// <summary>Exports the specified message. Returns <c>null</c> when the message is not found or the format is unsupported.</summary>
    public async Task<(byte[] Content, string ContentType, string FileName)?> ExportAsync(
        Guid messageId, string format, CancellationToken ct = default)
    {
        var message = await _store.GetByIdAsync(messageId, ct);
        if (message is null) return null;

        return format.ToLowerInvariant() switch
        {
            "eml" => ExportEml(message),
            "html" => (Encoding.UTF8.GetBytes(message.HtmlBody ?? ""),
                       "text/html",
                       $"{SanitizeFileName(message.Subject ?? "message")}.html"),
            "json" => ExportJson(message),
            _ => null,
        };
    }

    private static (byte[] Content, string ContentType, string FileName) ExportEml(CapturedMessage message)
    {
        var mime = MimeMessageConverter.ToMimeMessage(message);
        if (mime is null)
            return (Array.Empty<byte>(), "application/octet-stream", "message.eml");

        using var ms = new MemoryStream();
        mime.WriteTo(ms);
        return (ms.ToArray(), "message/rfc822", $"{SanitizeFileName(message.Subject ?? "message")}.eml");
    }

    private static (byte[] Content, string ContentType, string FileName) ExportJson(CapturedMessage message)
    {
        var dto = MessageMapper.ToDetailDto(message);
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        return (Encoding.UTF8.GetBytes(json), "application/json", $"{SanitizeFileName(message.Subject ?? "message")}.json");
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 80 ? name[..80] : name;
    }
}
