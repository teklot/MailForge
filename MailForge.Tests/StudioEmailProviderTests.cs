using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Studio.Capture;
using MailForge.Studio.Capture.Entities;
using MailForge.Studio.Web;

namespace MailForge.Tests
{
    public class StudioEmailProviderTests
    {
        private sealed class StubStore : IStudioCaptureStore
        {
            public EmailMessage? Captured { get; private set; }
            public string? EnvelopeFrom { get; private set; }

            public Task<CapturedMessage> CaptureAsync(
                EmailMessage message,
                string? envelopeFrom = null,
                byte[]? rawMime = null,
                CancellationToken cancellationToken = default)
            {
                Captured = message;
                EnvelopeFrom = envelopeFrom;
                return Task.FromResult(new CapturedMessage { Id = Guid.NewGuid(), MessageId = message.MessageId });
            }

            public Task<CapturedMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
                Task.FromResult<CapturedMessage?>(null);

            public Task<IReadOnlyList<CapturedMessage>> ListAsync(
                int skip = 0,
                int take = 50,
                CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<CapturedMessage>>(Array.Empty<CapturedMessage>());

            public Task<StudioMessagePage> QueryAsync(StudioMessageQuery query, CancellationToken cancellationToken = default) =>
                Task.FromResult(new StudioMessagePage());

            public Task<CapturedAttachment?> GetAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default) =>
                Task.FromResult<CapturedAttachment?>(null);
        }

        [Fact]
        public void Name_IsStudioCapture()
        {
            Assert.Equal("Studio Capture", new StudioEmailProvider(new StubStore()).Name);
        }

        [Fact]
        public void Capabilities_CoversAllMessageFeatures()
        {
            Assert.Equal(ProviderCapabilities.All, new StudioEmailProvider(new StubStore()).Capabilities);
        }

        [Fact]
        public async Task SendAsync_CapturesMessageAndReportsSuccess()
        {
            var store = new StubStore();
            var provider = new StudioEmailProvider(store);
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("Hi")
                .Build();

            var result = await provider.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(message.MessageId, result.ProviderMessageId);
            Assert.Same(message, store.Captured);
            Assert.Null(store.EnvelopeFrom);
        }

        [Fact]
        public async Task SendAsync_NullMessage_Throws()
        {
            var provider = new StudioEmailProvider(new StubStore());
            await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SendAsync(null!, TestContext.Current.CancellationToken));
        }
    }
}