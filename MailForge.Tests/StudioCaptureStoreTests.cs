using System;
using System.Linq;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Studio.Capture;
using MailForge.Studio.Capture.Entities;
using MailForge.Studio.Web;
using Microsoft.EntityFrameworkCore;

namespace MailForge.Tests
{
    public class StudioCaptureStoreTests
    {
        [Fact]
        public async Task CaptureAsync_PersistsMessageWithRecipientsAndHeaders()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);

                var message = EmailMessage.Create()
                    .From("sender@example.com", "Sender")
                    .To("a@example.com", "A")
                    .Cc("b@example.com")
                    .Bcc("c@example.com")
                    .Subject("Hello")
                    .Html("<b>Hi</b>")
                    .Text("Hi")
                    .Priority(EmailPriority.High)
                    .Header("X-Custom", "value")
                    .Attachment(new EmailAttachment("report.pdf", new byte[] { 1, 2, 3 }))
                    .Build();

                var captured = await store.CaptureAsync(message, "bounce@example.com", new byte[] { 4, 5, 6 }, TestContext.Current.CancellationToken);

                Assert.Equal(message.MessageId, captured.MessageId);
                Assert.Equal("bounce@example.com", captured.EnvelopeFrom);
                Assert.Equal("sender@example.com", captured.From);
                Assert.Equal("Hello", captured.Subject);
                Assert.Equal(EmailPriority.High, captured.Priority);
                Assert.Equal(new byte[] { 4, 5, 6 }, captured.RawMime);
                Assert.Equal(3, captured.Recipients.Count);
                Assert.Contains(captured.Recipients, r => r.Type == EmailRecipientType.To && r.Address == "a@example.com");
                Assert.Contains(captured.Recipients, r => r.Type == EmailRecipientType.Cc && r.Address == "b@example.com");
                Assert.Contains(captured.Recipients, r => r.Type == EmailRecipientType.Bcc && r.Address == "c@example.com");
                Assert.Single(captured.Headers, h => h.Name == "X-Custom" && h.Value == "value");
                Assert.Single(captured.Attachments, a => a.FileName == "report.pdf" && a.MediaType == "application/pdf");
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsLoadedChildren()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);

                var message = EmailMessage.Create()
                    .From("sender@example.com")
                    .To("a@example.com")
                    .Subject("Hello")
                    .Attachment(new EmailAttachment("a.txt", new byte[] { 9, 8 }))
                    .Build();
                var captured = await store.CaptureAsync(message, cancellationToken: TestContext.Current.CancellationToken);

                var loaded = await store.GetByIdAsync(captured.Id, TestContext.Current.CancellationToken);

                Assert.NotNull(loaded);
                Assert.Equal(captured.MessageId, loaded!.MessageId);
                Assert.Single(loaded.Recipients);
                Assert.Single(loaded.Attachments);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task ListAsync_ReturnsNewestFirstWithPaging()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var first = await CaptureAsync(store, "first@example.com");
                var second = await CaptureAsync(store, "second@example.com");
                var third = await CaptureAsync(store, "third@example.com");

                var all = await store.ListAsync(cancellationToken: TestContext.Current.CancellationToken);
                Assert.Equal(new[] { third.MessageId, second.MessageId, first.MessageId }, all.Select(m => m.MessageId));

                var page = await store.ListAsync(skip: 1, take: 1, TestContext.Current.CancellationToken);
                Assert.Single(page);
                Assert.Equal(second.MessageId, page[0].MessageId);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task CaptureAsync_MigratesSchemaAutomatically()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var message = EmailMessage.Create().From("s@example.com").To("a@example.com").Subject("Auto").Build();

                await store.CaptureAsync(message, cancellationToken: TestContext.Current.CancellationToken);

                await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
                Assert.Single(await context.Messages.ToListAsync(TestContext.Current.CancellationToken));
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task ListAsync_EmptyDatabase_ReturnsEmpty()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var all = await store.ListAsync(cancellationToken: TestContext.Current.CancellationToken);
                Assert.Empty(all);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        private static async Task<CapturedMessage> CaptureAsync(IStudioCaptureStore store, string recipient)
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To(recipient)
                .Subject("Subject")
                .Build();
            return await store.CaptureAsync(message, cancellationToken: TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task QueryAsync_SearchMatchesSubjectAndSender()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                await CaptureAsync(store, "alice@example.com");
                await CaptureAsync(store, "bob@example.com");

                var bySubject = await store.QueryAsync(new StudioMessageQuery { Search = "subject" }, TestContext.Current.CancellationToken);
                Assert.Equal(2, bySubject.TotalCount);

                var bySender = await store.QueryAsync(new StudioMessageQuery { Search = "sender" }, TestContext.Current.CancellationToken);
                Assert.Equal(2, bySender.TotalCount);

                var noMatch = await store.QueryAsync(new StudioMessageQuery { Search = "zzz" }, TestContext.Current.CancellationToken);
                Assert.Empty(noMatch.Items);
                Assert.Equal(0, noMatch.TotalCount);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task QueryAsync_PriorityFilterIsCaseInsensitive()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var normal = EmailMessage.Create().From("s@example.com").To("a@example.com").Subject("N").Build();
                var high = EmailMessage.Create().From("s@example.com").To("b@example.com").Subject("H").Priority(EmailPriority.High).Build();
                await store.CaptureAsync(normal, cancellationToken: TestContext.Current.CancellationToken);
                await store.CaptureAsync(high, cancellationToken: TestContext.Current.CancellationToken);

                var all = await store.QueryAsync(new StudioMessageQuery { Priority = "All" }, TestContext.Current.CancellationToken);
                Assert.Equal(2, all.TotalCount);

                var highs = await store.QueryAsync(new StudioMessageQuery { Priority = "high" }, TestContext.Current.CancellationToken);
                Assert.Single(highs.Items);
                Assert.Equal(EmailPriority.High, highs.Items[0].Priority);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task QueryAsync_SortsAndPages()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var a = EmailMessage.Create().From("s@example.com").To("a@example.com").Subject("Alpha").Build();
                var b = EmailMessage.Create().From("s@example.com").To("b@example.com").Subject("Beta").Build();
                await store.CaptureAsync(a, cancellationToken: TestContext.Current.CancellationToken);
                await store.CaptureAsync(b, cancellationToken: TestContext.Current.CancellationToken);

                var bySubject = await store.QueryAsync(new StudioMessageQuery { Sort = "Subject", PageSize = 10 }, TestContext.Current.CancellationToken);
                Assert.Equal(new[] { "Alpha", "Beta" }, bySubject.Items.Select(m => m.Subject));

                var firstPage = await store.QueryAsync(new StudioMessageQuery { Sort = "From", Page = 1, PageSize = 1 }, TestContext.Current.CancellationToken);
                Assert.Equal(2, firstPage.TotalCount);
                Assert.Equal(2, firstPage.TotalPages);
                Assert.Single(firstPage.Items);
                Assert.Equal(1, firstPage.Page);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task QueryAsync_LoadsRecipients()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var message = EmailMessage.Create()
                    .From("sender@example.com")
                    .To("a@example.com")
                    .Cc("b@example.com")
                    .Subject("Hello")
                    .Build();
                await store.CaptureAsync(message, cancellationToken: TestContext.Current.CancellationToken);

                var page = await store.QueryAsync(new StudioMessageQuery(), TestContext.Current.CancellationToken);
                Assert.Single(page.Items);
                Assert.Equal(2, page.Items[0].Recipients.Count);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task GetAttachmentAsync_ReturnsAttachmentWithContent()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var message = EmailMessage.Create()
                    .From("sender@example.com")
                    .To("a@example.com")
                    .Subject("Files")
                    .Attachment(new EmailAttachment("a.txt", new byte[] { 1, 2, 3 }, "text/plain"))
                    .Build();
                var captured = await store.CaptureAsync(message, cancellationToken: TestContext.Current.CancellationToken);

                var attachmentId = captured.Attachments.First().Id;
                var loaded = await store.GetAttachmentAsync(attachmentId, TestContext.Current.CancellationToken);

                Assert.NotNull(loaded);
                Assert.Equal("a.txt", loaded!.FileName);
                Assert.Equal("text/plain", loaded.MediaType);
                Assert.Equal(new byte[] { 1, 2, 3 }, loaded.Content);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task GetAttachmentAsync_UnknownId_ReturnsNull()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var loaded = await store.GetAttachmentAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
                Assert.Null(loaded);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }
    }
}