using MailKit.Security;

namespace MailForge.Smtp
{
    /// <summary>Configuration for the SMTP provider.</summary>
    public sealed class SmtpOptions
    {
        /// <summary>The SMTP server host name or IP address.</summary>
        public string Host { get; set; }

        /// <summary>The SMTP server port (typically 25, 465, or 587).</summary>
        public int Port { get; set; } = 587;

        /// <summary>How the connection is secured (defaults to auto-negotiation).</summary>
        public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.Auto;

        /// <summary>The login user name (optional).</summary>
        public string Username { get; set; }

        /// <summary>The login password (optional).</summary>
        public string Password { get; set; }

        /// <summary>The local domain used during the SMTP conversation (optional).</summary>
        public string LocalDomain { get; set; }

        /// <summary>The socket and command timeout in milliseconds.</summary>
        public int TimeoutMilliseconds { get; set; } = 30000;
    }
}
