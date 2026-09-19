namespace MailForge.Studio.Web;

/// <summary>Configuration for the Studio web dashboard.</summary>
public sealed class StudioWebOptions
{
    /// <summary>The SQLite database path used by <see cref="StudioCaptureOptions"/>.</summary>
    public string DatabasePath { get; init; } = "";

    /// <summary>The HTTP port the web dashboard listens on (default: 5000).</summary>
    public int WebPort { get; init; } = 5000;

    /// <summary>
    /// True to run the local SMTP capture relay alongside the dashboard, so any SMTP client
    /// pointed at <see cref="SmtpRelayPort"/> is captured and shown live (default: true).
    /// </summary>
    public bool EnableSmtpRelay { get; init; } = true;

    /// <summary>The port the SMTP capture relay listens on (default: 2525). Use 0 for an ephemeral port.</summary>
    public int SmtpRelayPort { get; init; } = 2525;

    /// <summary>The SMTP relay host used for message replay (default: localhost).</summary>
    public string SmtpHost { get; init; } = "localhost";

    /// <summary>The SMTP relay port used for message replay (default: 25).</summary>
    public int SmtpPort { get; init; } = 25;

    /// <summary>The SMTP send timeout in milliseconds (default: 30000).</summary>
    public int TimeoutMilliseconds { get; init; } = 30000;
}