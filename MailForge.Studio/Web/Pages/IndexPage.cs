namespace MailForge.Studio.Web.Pages;

/// <summary>
/// Two-column inbox page: a full-width toolbar (title + search) over a message list on the
/// left and the body of the selected message on the right. The list auto-refreshes every 5 s
/// via HTMX without disturbing the open detail panel.
/// </summary>
public static class IndexPage
{
    /// <summary>
    /// Renders the inbox. Full requests render the whole page (region + detail panel); HTMX
    /// requests return only the list region, except a row click which returns the re-rendered,
    /// highlighted region plus the detail panel as an out-of-band swap.
    /// </summary>
    public static IResult Render(HttpContext http, [AsParameters] StudioMessageQuery query)
    {
        var store = http.RequestServices.GetRequiredService<IStudioCaptureStore>();
        var page = store.QueryAsync(query).GetAwaiter().GetResult();
        var selectedId = ParseSelected(query);

        if (http.Request.Headers.ContainsKey("HX-Request"))
        {
            var isRegionPoll =
                http.Request.Headers.TryGetValue("HX-Trigger", out var trigger)
                && trigger.ToString() == "inbox-region";

            if (!isRegionPoll && selectedId is Guid active)
            {
                var message = store.GetByIdAsync(active).GetAwaiter().GetResult();
                if (message is not null)
                {
                    return new Fragment(
                        BuildRegion(query, page, selectedId, includeCount: true),
                        Div(DetailPage.RenderPanel(MessageMapper.ToDetailDto(message)))
                            .Id("detail-panel")
                            .HxSwapOob("innerHTML:#detail-panel")
                    ).ToHtmlResult();
                }
            }

            return BuildRegion(query, page, selectedId, includeCount: true).ToHtmlResult();
        }

        var detail = Div(BuildDetailPlaceholder());
        if (selectedId is Guid selected)
        {
            var message = store.GetByIdAsync(selected).GetAwaiter().GetResult();
            if (message is not null)
                detail = Div(DetailPage.RenderPanel(MessageMapper.ToDetailDto(message)));
        }

        var content = Div(
            Div(
                BuildHeader(page.TotalCount),
                BuildSearchForm(query)
            ).Class("d-flex align-items-center gap-3 px-3 py-3 border-bottom"),
            Div(
                Div(
                    Div(BuildRegion(query, page, selectedId, includeCount: false)).Class("flex-grow-1 overflow-auto")
                ).Class("col-4 d-flex flex-column border-end"),
                Div(
                    Div(detail).Id("detail-panel").Class("h-100 overflow-auto")
                ).Class("col-8")
            ).Class("row g-0").Style("height: calc(100vh - 180px)")
        );

        return Layout.Page(http, "MailForge Studio — Inbox", content).ToHtmlResult();
    }

    private static Node BuildDetailPlaceholder() =>
        Div(
            Span().Class("bi bi-envelope-open text-muted d-block text-center fs-2 mb-2"),
            Span("Select a message to view its contents.").Class("text-muted d-block text-center")
        ).Class("h-100 d-flex flex-column align-items-center justify-content-center text-center");

    private static Guid? ParseSelected(StudioMessageQuery query) =>
        Guid.TryParse(query.Selected, out var selected) ? selected : null;

    private static Node BuildHeader(int totalCount) =>
        H4(
            new TextNode("Inbox ("),
            Span($"{totalCount}").Id("inbox-count").Class("text-muted"),
            new TextNode(")")
        ).Class("mb-0 fw-semibold");

    private static Node BuildRegion(StudioMessageQuery query, StudioMessagePage page, Guid? selectedId, bool includeCount)
    {
        var rows = page.Items.Select(m => BuildRow(m, selectedId, query, page.Page)).ToList();

        return Div(
            rows.Count > 0
                ? Table(
                    Thead(
                        Tr(
                            Th("From").Class("w-25"),
                            Th("Subject").Class("w-50"),
                            new ThElement(),
                            new ThElement(),
                            Th("Date").Class("w-25")
                        )
                    ).Class("table-light"),
                    Tbody(rows.ToArray())
                ).Class("table table-hover align-middle mb-0")
                : Div("No messages found.").Class("text-muted py-4"),
            BuildPagingBar(query, page),
            includeCount
                ? Span($"{page.TotalCount}").Id("inbox-count").Class("text-muted").HxSwapOob("true")
                : new Fragment()
        )
        .Id("inbox-region")
        .HxTrigger("every 5s")
        .HxGet(BuildPageUrl(query, page.Page))
        .HxTarget("#inbox-region")
        .HxSwap("outerHTML");
    }

    private static Node BuildRow(CapturedMessage m, Guid? selectedId, StudioMessageQuery query, int page)
    {
        var dto = MessageMapper.ToListDto(m);
        var (icon, color) = (dto.Priority ?? "Normal") switch
        {
            "High" => ("bi-exclamation-triangle-fill", "text-danger"),
            "Low"  => ("bi-arrow-down-circle", "text-secondary"),
            _      => ("bi-envelope-fill", "text-primary"),
        };

        var tr = Tr(
            Td(Span(dto.From ?? "(no sender)").Class("fw-semibold")).Class("border-bottom"),
            Td(Span(dto.Subject ?? "(no subject)")).Class("border-bottom"),
            Td(Span().Class($"bi {icon} {color}").Title(dto.Priority ?? "Normal")).Class("border-bottom text-center"),
            Td(
                dto.AttachmentCount > 0
                    ? Span().Class("bi bi-paperclip text-muted").Title($"{dto.AttachmentCount} attachment(s)")
                    : new Fragment()
            ).Class("border-bottom text-center"),
            Td(Span(dto.CreatedAt.LocalDateTime.ToString("g")).Class("text-muted")).Class("border-bottom text-end")
        );

        if (selectedId == dto.Id)
            tr.Class("table-active");

        return tr
            .HxGet(BuildPageUrl(query, page, dto.Id))
            .HxTarget("#inbox-region")
            .HxSwap("outerHTML")
            .HxPushUrl("true");
    }

    private static Node BuildSearchForm(StudioMessageQuery query)
    {
        return Form(
            Div(
                Input().Type("text").Name("search").Value(query.Search ?? "")
                    .Placeholder("Search subject or sender...")
                    .Class("form-control")
                    .HxTrigger("keyup changed delay:600ms")
                    .HxGet("/")
                    .HxTarget("#inbox-region")
                    .HxSwap("outerHTML")
                    .Style("min-width:240px;width:360px"),
                Select(
                    Option("All").Value("All"),
                    Option("High").Value("High"),
                    Option("Normal").Value("Normal"),
                    Option("Low").Value("Low")
                ).Name("priority").Class("form-select ms-2")
                    .HxTrigger("change")
                    .HxGet("/")
                    .HxTarget("#inbox-region")
                    .HxSwap("outerHTML")
                    .Style("width:130px")
            ).Class("d-flex align-items-center")
        )
        .Class("mb-0 ms-auto")
        .HxGet("/")
        .HxTarget("#inbox-region")
        .HxSwap("outerHTML");
    }

    private static Node BuildPagingBar(StudioMessageQuery query, StudioMessagePage page)
    {
        if (page.TotalPages <= 1) return new Fragment();

        var buttons = new List<Node>();
        for (var i = 1; i <= page.TotalPages; i++)
        {
            var isActive = i == page.Page;
            buttons.Add(
                Li(
                    A($"{i}").Class($"page-link{(isActive ? " active" : "")}").HxGet(BuildPageUrl(query, i))
                        .HxTarget("#inbox-region").HxSwap("outerHTML")
                ).Class($"page-item{(isActive ? " active" : "")}")
            );
        }

        return Nav(Ul(buttons.ToArray()).Class("pagination pagination-sm mt-3")).Class("d-flex justify-content-center");
    }

    private static string BuildPageUrl(StudioMessageQuery query, int page, Guid? selected = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Search))
            parts.Add($"search={Uri.EscapeDataString(query.Search)}");
        if (!string.IsNullOrWhiteSpace(query.Priority) && query.Priority != "All")
            parts.Add($"priority={Uri.EscapeDataString(query.Priority)}");
        if (!string.IsNullOrWhiteSpace(query.Sort) && query.Sort != "Newest")
            parts.Add($"sort={Uri.EscapeDataString(query.Sort)}");
        var id = selected ?? (Guid.TryParse(query.Selected, out var parsed) ? parsed : null);
        if (id is Guid g)
            parts.Add($"selected={g}");
        parts.Add($"page={page}");
        return "/?" + string.Join("&", parts);
    }
}