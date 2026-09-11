using System;

namespace MailForge.Studio.Capture
{
    /// <summary>Configuration for the MailForge Studio capture subsystem.</summary>
    public sealed class StudioCaptureOptions
    {
        /// <summary>
        /// The path of the SQLite database file used to persist captured messages.
        /// Defaults to "mailforge-studio.db". The database can be reset by deleting the file
        /// (migrations rebuild the schema on next use).
        /// </summary>
        public string DatabasePath { get; set; } = "mailforge-studio.db";

        /// <summary>
        /// True to run the local SMTP relay listener. Defaults to true.
        /// </summary>
        public bool EnableSmtpRelay { get; set; } = true;

        /// <summary>The host the SMTP relay binds to. Defaults to the loopback address.</summary>
        public string SmtpRelayHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// The port the SMTP relay listens on. Defaults to 2525. Use 0 to select an
        /// ephemeral port (useful in tests).
        /// </summary>
        public int SmtpRelayPort { get; set; } = 2525;

        /// <summary>
        /// Builds a complete SQLite connection string from the database file path.
        /// Pooling is disabled so the file is never locked while the app is running.
        /// </summary>
        public string BuildConnectionString() => $"Data Source={DatabasePath};Pooling=false";
    }
}