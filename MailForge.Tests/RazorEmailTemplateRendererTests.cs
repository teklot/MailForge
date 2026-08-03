using MailForge.Templates;

namespace MailForge.Tests
{
    public class RazorEmailTemplateRendererTests
    {
        public sealed class Model
        {
            public string Name { get; set; } = "World";
        }

        [Fact]
        public void Render_CompilesAndRunsRazorTemplate()
        {
            var renderer = new RazorEmailTemplateRenderer();
            var result = renderer.Render("Hello @Model.Name!", new Model());
            Assert.Equal("Hello World!", result);
        }

        [Fact]
        public void Render_CachesCompiledTemplates()
        {
            var renderer = new RazorEmailTemplateRenderer();
            renderer.Render("Welcome @Model.Name", new Model());

            var result = renderer.Render("Welcome @Model.Name", new Model { Name = "Bob" });
            Assert.Equal("Welcome Bob", result);
        }
    }
}
