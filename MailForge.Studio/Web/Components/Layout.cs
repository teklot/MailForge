namespace MailForge.Studio.Web.Components;

/// <summary>Shared HTML layout with Bootstrap 5 + HTMX for all Studio web pages.</summary>
public static class Layout
{
    /// <summary>Wraps page content in a full HTML document with navbar and shared scripts.</summary>
    public static PageElement Page(HttpContext http, string title, params Node[] content)
    {
        return new PageElement(
            new HeadElement(
                new TitleElement(title),
                Meta().Charset("utf-8"),
                Meta().Name("viewport").Content("width=device-width, initial-scale=1"),
                Link().Href("https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css").Rel("stylesheet"),
                Link().Href("https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css").Rel("stylesheet"),
                Script().Src("https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"),
                Script().Src("https://unpkg.com/htmx.org@2.0.4/dist/htmx.min.js")
            ),
            new BodyElement(
                RenderNavbar(),
                new MainElement(content).Class("container-fluid p-0"),
                new FooterElement(
                    Div($"MailForge Studio — {DateTimeOffset.UtcNow.Year} TekLot").Class("text-center text-muted py-3 border-top")
                ).Class("container")
            )
        ).Lang("en");
    }

    private static Element RenderNavbar()
    {
        return Navbar(
            Div(
                NavbarBrand("MailForge Studio").Href("/"),
                NavbarCollapse(
                    NavbarNav(
NavbarNavItem(A("Inbox").Href("/").Class("nav-link"))
            )
                )
            ).Class("container-fluid")
        ).Dark().ExpandLg().Class("bg-dark").Id("mainNav");
    }
}
