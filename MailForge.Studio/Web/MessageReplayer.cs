using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

namespace MailForge.Studio.Web;

/// <summary>Replays captured messages through an SMTP relay.</summary>
public sealed class MessageReplayer
{
    private readonly IStudioCaptureStore _store;
    private readonly StudioWebOptions _options;
    private readonly ILogger<MessageReplayer> _logger;

    /// <summary>Initialises a new <see cref="MessageReplayer"/>.</summary>
    public MessageReplayer(IStudioCaptureStore store, StudioWebOptions options, ILogger<MessageReplayer> logger)
    {
        _store = store;
        _options = options;
        _logger = logger;
    }

    /// <summary>Replays the specified captured message via SMTP.</summary>
    public async Task<ReplayResult> ReplayAsync(Guid messageId, ReplayRequest? request = null, CancellationToken ct = default)
    {
        var message = await _store.GetByIdAsync(messageId, ct);
        if (message is null)
            return new ReplayResult { Succeeded = false, Message = "Message not found" };

        var recipients = request?.OverrideRecipients?.Count > 0
            ? request.OverrideRecipients!
            : message.Recipients
                .Select(r => r.Address!)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();

        if (recipients.Count == 0)
            return new ReplayResult { Succeeded = false, Message = "No recipients" };

        try
        {
            var mime = MimeMessageConverter.ToMimeMessage(message);
            if (mime is null)
                return new ReplayResult { Succeeded = false, Message = "Failed to build MIME message from stored data" };

            mime.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId();

            mime.To.Clear();
            mime.Cc.Clear();
            mime.Bcc.Clear();

            foreach (var addr in recipients)
                mime.To.Add(MimeKit.MailboxAddress.Parse(addr));

            using var client = new SmtpClient { Timeout = _options.TimeoutMilliseconds };
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, MailKit.Security.SecureSocketOptions.None, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Replayed message {Id} to {Count} recipient(s)", messageId, recipients.Count);
            return new ReplayResult
            {
                Succeeded = true,
                Message = $"Replayed to {recipients.Count} recipient(s)",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replay failed for message {Id}", messageId);
            return new ReplayResult { Succeeded = false, Message = ex.Message };
        }
    }
}