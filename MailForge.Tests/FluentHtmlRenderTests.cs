using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Studio.Capture;
using MailForge.Studio.Capture.Entities;
using MailForge.Studio.Web;
using MailForge.Studio.Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MailForge.Tests
{
    public class FluentHtmlRenderTests
    {
        [Fact]
        public async Task IndexPage_RendersInboxWithHtmxRows()
        {
            var http = TestContext();
            var result = IndexPage.Render(http, new StudioMessageQuery { PageSize = 25 });
            var html = await RenderAsync(result, http);

            Assert.Contains("Inbox", html);
            Assert.Contains("Welcome", html);
            Assert.Contains("hx-get=\"/?page=", html);
            Assert.Contains("hx-target=\"#inbox-region\"", html);
        }

        [Fact]
        public async Task IndexPage_SelectedRow_RendersTableActive()
        {
            var http = TestContext();
            var result = IndexPage.Render(http, new StudioMessageQuery { PageSize = 25, Selected = MessageId.ToString() });
            var html = await RenderAsync(result, http);

            Assert.Contains("table-active", html);
        }

        [Fact]
        public async Task IndexPage_HxRowClick_ReturnsRegionAndOobDetail()
        {
            var http = TestContext();
            http.Request.Headers["HX-Request"] = "true";
            http.Request.Headers["HX-Trigger"] = "null";
            var result = IndexPage.Render(http, new StudioMessageQuery { PageSize = 25, Selected = MessageId.ToString() });
            var html = await RenderAsync(result, http);

            Assert.Contains("table-active", html);
            Assert.Contains("id=\"detail-panel\"", html);
            Assert.Contains("hx-swap-oob=\"true\"", html);
            Assert.Contains("hx-target=\"#inbox-region\"", html);
        }

        [Fact]
        public async Task IndexPage_EmptyInbox_ShowsEmptyState()
        {
            var http = TestContext(stub: new StubStore(isEmpty: true));
            var result = IndexPage.Render(http, new StudioMessageQuery());
            var html = await RenderAsync(result, http);

            Assert.Contains("No messages found.", html);
        }

        [Fact]
        public async Task DetailPage_RendersTabsAndActions()
        {
            var http = TestContext();
            var result = DetailPage.Render(http, MessageId);
            var html = await RenderAsync(result, http);

            Assert.Contains("Welcome to Acme", html);
            Assert.Contains("Overview", html);
            Assert.Contains("Raw MIME", html);
            Assert.Contains("Attachments (1)", html);
            Assert.Contains("export", html);
            Assert.Contains("replay", html);
        }

        [Fact]
        public async Task DetailPage_UnknownMessage_ReturnsNotFound()
        {
            var http = TestContext(stub: new StubStore(knownId: System.Guid.NewGuid()));
            var result = DetailPage.Render(http, System.Guid.NewGuid());
            var html = await RenderAsync(result, http);

            Assert.Equal(StatusCodes.Status404NotFound, http.Response.StatusCode);
            Assert.Empty(html);
        }

        private static readonly Guid MessageId = System.Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static DefaultHttpContext TestContext(IStudioCaptureStore? stub = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IStudioCaptureStore>(stub ?? new StubStore());
            services.AddLogging();
            var http = new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider(),
            };
            http.Response.Body = new MemoryStream();
            return http;
        }

        private static async Task<string> RenderAsync(IResult result, HttpContext http)
        {
            await result.ExecuteAsync(http);
            http.Response.Body.Position = 0;
            using var reader = new StreamReader(http.Response.Body);
            return await reader.ReadToEndAsync();
        }

        private sealed class StubStore : IStudioCaptureStore
        {
            private readonly CapturedMessage? _message;

            public StubStore(bool isEmpty = false, Guid knownId = default)
            {
                _message = isEmpty ? null : Sample(knownId == default ? MessageId : knownId);
            }

            public Task<CapturedMessage> CaptureAsync(
                EmailMessage message,
                string? envelopeFrom = null,
                byte[]? rawMime = null,
                CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<CapturedMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
                Task.FromResult(firstOrDefault);

            public Task<IReadOnlyList<CapturedMessage>> ListAsync(
                int skip = 0,
                int take = 50,
                CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<CapturedMessage>>(items);

            public Task<StudioMessagePage> QueryAsync(StudioMessageQuery query, CancellationToken cancellationToken = default) =>
                Task.FromResult(new StudioMessagePage
                {
                    Items = items,
                    TotalCount = items.Count,
                    Page = Math.Max(1, query.Page ?? 1),
                    PageSize = Math.Max(1, query.PageSize ?? 25),
                });

            public Task<CapturedAttachment?> GetAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default) =>
                Task.FromResult(_message?.Attachments.FirstOrDefault(a => a.Id == attachmentId));

            private IReadOnlyList<CapturedMessage> items =>
                _message is null ? Array.Empty<CapturedMessage>() : new[] { _message };

            private CapturedMessage? firstOrDefault =>
                _message is null || _message.Id != MessageId ? null : _message;
        }

        private static CapturedMessage Sample(Guid id)
        {
            return new CapturedMessage
            {
                Id = id,
                MessageId = $"<{id:N}@mailforge.dev>",
                From = "sender@example.com",
                Subject = "Welcome to Acme",
                HtmlBody = "<h1>Welcome</h1><p>Thanks for joining <b>Acme</b>!</p>",
                TextBody = "Welcome to Acme! Thanks for joining.",
                Priority = EmailPriority.Normal,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                Recipients =
                [
                    new CapturedRecipient { Type = EmailRecipientType.To, Address = "alice@example.com", DisplayName = "Alice" },
                ],
                Attachments =
                [
                    new CapturedAttachment
                    {
                        Id = System.Guid.NewGuid(),
                        FileName = "invoice.pdf",
                        MediaType = "application/pdf",
                        Content = new byte[] { 1, 2, 3 },
                    },
                ],
                Headers =
                [
                    new CapturedHeader { Name = "X-Custom", Value = "value" },
                ],
            };
        }
    }
}