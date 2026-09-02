using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MailForge.AzureCS
{
    /// <summary>
    /// Implements the HMAC-SHA256 request signing algorithm required by the
    /// Azure Communication Services Email REST API (api-version 2023-03-31).
    /// </summary>
    internal static class AzureCSHmacSigner
    {
        /// <summary>Computes the shared signing headers for a POST request body.</summary>
        /// <param name="method">The HTTP method (for example, POST).</param>
        /// <param name="pathAndQuery">The URI path and query (for example, "/emails:send?api-version=2023-03-31").</param>
        /// <param name="host">The request authority (host:port).</param>
        /// <param name="body">The UTF-8 request body bytes.</param>
        /// <param name="accessKey">The Base64-encoded access key from the connection string.</param>
        /// <returns>The date, content hash, and Authorization header value.</returns>
        public static SignedHeaders Sign(string method, string pathAndQuery, string host, byte[] body, string accessKey)
        {
            var date = DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            var contentTypeHash = Convert.ToBase64String(SHA256.Create().ComputeHash(body));

            var stringToSign = string.Concat(
                method, "\n",
                pathAndQuery, "\n",
                date, ";",
                host, ";",
                contentTypeHash);

            var secretBytes = System.Convert.FromBase64String(accessKey);
            byte[] signature;
            using (var hmac = new HMACSHA256(secretBytes))
            {
                signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            }

            var authorization = "HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature="
                                + Convert.ToBase64String(signature);

            return new SignedHeaders(date, contentTypeHash, authorization);
        }
    }

    /// <summary>The signing headers produced by <see cref="AzureCSHmacSigner.Sign"/>.</summary>
    internal sealed class SignedHeaders
    {
        public SignedHeaders(string date, string contentHash, string authorization)
        {
            Date = date;
            ContentHash = contentHash;
            Authorization = authorization;
        }

        /// <summary>The x-ms-date header value (UTC RFC1123).</summary>
        public string Date { get; }

        /// <summary>The x-ms-content-sha256 header value.</summary>
        public string ContentHash { get; }

        /// <summary>The Authorization header value.</summary>
        public string Authorization { get; }
    }
}