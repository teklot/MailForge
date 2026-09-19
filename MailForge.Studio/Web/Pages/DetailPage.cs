namespace MailForge.Studio.Web.Pages;

/// <summary>Message detail page with tabs for overview, HTML preview, plain text, raw MIME, model JSON, attachments, and headers.</summary>
public static class DetailPage
{
    /// <summary>Renders the full message detail panel with tabbed sections.</summary>
    public static IResult Render(HttpContext http, Guid id)
    {
        var store = http.RequestServices.GetRequiredService<IStudioCaptureStore>();
        var message = store.GetByIdAsync(id).GetAwaiter().GetResult();
        if (message is null)
            return Results.NotFound();

        var dto = MessageMapper.ToDetailDto(message);
        var content = RenderPanel(dto);
        return content.ToHtmlResult();
    }

    /// <summary>Builds the message detail panel (header, tabs, and tab content).</summary>
    public static Node RenderPanel(MessageDetailDto dto)
    {
        return Div(
            Div(
                H5(dto.Subject ?? "(no subject)").Class("mb-0 text-truncate"),
                Div(
                    Badge(dto.Priority ?? "Normal").Class($"bg-{PriorityBadgeClass(dto.Priority)}"),
                    Span($"From: {dto.From ?? "(unknown)"}").Class("text-muted ms-2"),
                    Span($" — {dto.CreatedAt.LocalDateTime:g}").Class("text-muted")
                ).Class("small mt-1")
            ).Class("px-3 pt-3 pb-2 border-bottom bg-light"),
            Tabs(
                TabItem(TabLink("Overview").Href("#tab-overview").Active()),
                TabItem(TabLink("HTML").Href("#tab-html")),
                TabItem(TabLink("Text").Href("#tab-text")),
                TabItem(TabLink("Raw MIME").Href("#tab-raw")),
                TabItem(TabLink("Model").Href("#tab-model")),
                TabItem(TabLink($"Attachments ({dto.Attachments.Count})").Href("#tab-attachments")),
                TabItem(TabLink($"Headers ({dto.Headers.Count})").Href("#tab-headers"))
            ).Class("px-3"),
            Div(
                TabContent(
                    BuildOverviewTab(dto).Id("tab-overview").Active().Show(),
                    BuildHtmlPreviewTab(dto).Id("tab-html"),
                    BuildTextTab(dto).Id("tab-text"),
                    BuildRawMimeTab(dto).Id("tab-raw"),
                    BuildModelTab(dto).Id("tab-model"),
                    BuildAttachmentsTab(dto).Id("tab-attachments"),
                    BuildHeadersTab(dto).Id("tab-headers")
                )
            ).Class("flex-grow-1 overflow-auto p-3")
        ).Class("d-flex flex-column h-100");
    }

    private static TabPaneComponent BuildOverviewTab(MessageDetailDto dto)
    {
        var recipientRows = dto.Recipients.Select(r =>
            Tr(
                Td(Badge(r.Type).Class($"badge bg-{RecipientBadgeClass(r.Type)}")),
                Td(new TextNode(r.Address)),
                Td(new TextNode(r.DisplayName ?? "")).Class("text-muted")
            )
        );

        return TabPane(
            Table(
                Thead(Tr(
                    Th("Type").Class("col-2"),
                    Th("Address").Class("col-5"),
                    Th("Display Name").Class("col-5 text-muted")
                )),
                Tbody(recipientRows.ToArray())
            ).Class("table table-sm").Id("detail-recipients"),
            Div(
                A($"Export as EML").Href($"/api/messages/{dto.Id}/export?format=eml").Class("btn btn-outline-secondary btn-sm"),
                A($"Export as HTML").Href($"/api/messages/{dto.Id}/export?format=html").Class("btn btn-outline-secondary btn-sm"),
                A($"Export as JSON").Href($"/api/messages/{dto.Id}/export?format=json").Class("btn btn-outline-secondary btn-sm"),
                Button("Replay").Class("btn btn-warning btn-sm ms-2")
                    .HxPost($"/api/messages/{dto.Id}/replay")
                    .HxTarget("#replay-result")
                    .HxSwap("innerHTML")
            ).Class("d-flex align-items-center gap-2 mt-3"),
            Div().Id("replay-result").Class("mt-2")
        );
    }

    private static TabPaneComponent BuildHtmlPreviewTab(MessageDetailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HtmlBody))
            return TabPane(Div("No HTML body available.").Class("text-muted p-3"));

        var frame = new IframeElement()
            .Style("width:100%;min-height:500px;border:1px solid #dee2e6;border-radius:4px");
        frame.Attributes.Set("srcdoc", dto.HtmlBody);
        frame.Attributes.Set("sandbox", "allow-same-origin");

        return TabPane(frame);
    }

    private static TabPaneComponent BuildTextTab(MessageDetailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TextBody))
            return TabPane(Div("No plain text body available.").Class("text-muted p-3"));

        return TabPane(
            Pre(new TextNode(dto.TextBody)).Class("bg-light p-3 rounded").Style("white-space:pre-wrap;max-height:500px;overflow-y:auto")
        );
    }

    private static TabPaneComponent BuildRawMimeTab(MessageDetailDto dto)
    {
        return TabPane(
            Div(
                Span("Raw MIME content is regenerated from stored data on demand.").Class("text-muted small"),
                Div().Id("raw-mime-content").Class("mt-2"),
                Button("Load Raw MIME").Class("btn btn-outline-secondary btn-sm mt-2")
                    .HxGet($"/api/messages/{dto.Id}/raw")
                    .HxTarget("#raw-mime-content")
                    .HxSwap("innerHTML")
            ).Class("p-3")
        );
    }

    private static TabPaneComponent BuildModelTab(MessageDetailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ModelJson))
            return TabPane(Div("No model data available.").Class("text-muted p-3"));

        return TabPane(
            Pre(new TextNode(dto.ModelJson)).Class("bg-light p-3 rounded").Style("white-space:pre-wrap;max-height:500px;overflow-y:auto;font-size:0.85em")
        );
    }

    private static TabPaneComponent BuildAttachmentsTab(MessageDetailDto dto)
    {
        var inline = dto.Attachments.Where(a => a.IsInline).ToList();
        var files = dto.Attachments.Where(a => !a.IsInline).ToList();

        var attachmentList = files.Select(a =>
            Div(
                Span().Class("bi bi-file-earmark me-1"),
                A(a.FileName ?? "attachment").Href($"/api/messages/{dto.Id}/attachments/{a.Id}").Class("text-decoration-none"),
                Span($" ({FormatSize(a.Size)}, {a.MediaType ?? "unknown"})").Class("text-muted small")
            ).Class("py-1 border-bottom")
        );

        var inlineList = inline.Select(a =>
            Div(
                Span().Class("bi bi-image me-1"),
                Span(a.FileName ?? "inline image").Class("small"),
                Span($" (Content-ID: {a.Id})").Class("text-muted small")
            ).Class("py-1 border-bottom")
        );

        if (!files.Any() && !inline.Any())
            return TabPane(Div("No attachments.").Class("text-muted p-3"));

        return TabPane(
            files.Count > 0
                ? new Fragment(
                    H6($"File Attachments ({files.Count})").Class("mt-2"),
                    Div(attachmentList.ToArray())
                )
                : new Fragment(),
            inline.Count > 0
                ? new Fragment(
                    H6($"Inline Images ({inline.Count})").Class("mt-3"),
                    Div(inlineList.ToArray())
                )
                : new Fragment()
        );
    }

    private static TabPaneComponent BuildHeadersTab(MessageDetailDto dto)
    {
        if (!dto.Headers.Any())
            return TabPane(Div("No custom headers.").Class("text-muted p-3"));

        var rows = dto.Headers.Select(h =>
            Tr(
                Td(Strong(h.Name)).Class("w-25"),
                Td(new TextNode(h.Value)).Class("w-75 text-break")
            )
        );

        return TabPane(
            Table(
                Thead(Tr(Th("Name").Class("w-25"), Th("Value").Class("w-75"))).Class("table-light"),
                Tbody(rows.ToArray())
            ).Class("table table-sm table-borderless")
        );
    }

    private static string PriorityBadgeClass(string? priority) => priority switch
    {
        "High" => "danger",
        "Normal" => "primary",
        "Low" => "secondary",
        _ => "info",
    };

    private static string RecipientBadgeClass(string type) => type switch
    {
        "To" => "primary",
        "Cc" => "info",
        "Bcc" => "secondary",
        _ => "secondary",
    };

    private static string FormatSize(int bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };
}
