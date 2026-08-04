using System;

namespace MailForge
{
    /// <summary>Represents an email address with an optional display name.</summary>
    public sealed class EmailAddress : IEquatable<EmailAddress>
    {
        /// <summary>The raw email address (for example, "john@example.com").</summary>
        public string Address { get; }

        /// <summary>An optional display name shown next to the address.</summary>
        public string? DisplayName { get; }

        /// <summary>Creates an email address.</summary>
        public EmailAddress(string address, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("An email address is required.", nameof(address));
            Address = address.Trim();
            DisplayName = displayName;
        }

        /// <summary>Returns "Display Name &lt;address&gt;" when a display name is set, otherwise the address.</summary>
        public override string ToString() =>
            string.IsNullOrEmpty(DisplayName) ? Address : $"{DisplayName} <{Address}>";

        /// <summary>Returns true if this address equals another by comparing both components.</summary>
        public bool Equals(EmailAddress? other) =>
            other != null &&
            string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal);

        /// <summary>Returns true if this address equals another object.</summary>
        public override bool Equals(object? obj) => Equals(obj as EmailAddress);

        /// <summary>Returns a hash code for this address.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(Address) * 397) ^
                       (DisplayName?.GetHashCode() ?? 0);
            }
        }
    }
}
