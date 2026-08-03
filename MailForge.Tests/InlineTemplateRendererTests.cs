using MailForge.Core.Templates;

namespace MailForge.Tests
{
    public class InlineTemplateRendererTests
    {
        private sealed class Model
        {
            public string Name { get; set; } = "Alice";

            public int Count { get; set; } = 3;

            public Address Address { get; set; } = new Address { City = "London" };

            public string[] Items { get; set; } = { "a", "b" };
        }

        private sealed class Address
        {
            public string City { get; set; } = string.Empty;
        }

        [Fact]
        public void Render_SubstitutesSimplePlaceholders()
        {
            var renderer = new InlineTemplateRenderer();
            var result = renderer.Render("Hi {{Name}}, count is {{Count}}.", new Model());
            Assert.Equal("Hi Alice, count is 3.", result);
        }

        [Fact]
        public void Render_ResolvesNestedPropertyPaths()
        {
            var renderer = new InlineTemplateRenderer();
            var result = renderer.Render("City: {{Address.City}}", new Model());
            Assert.Equal("City: London", result);
        }

        [Fact]
        public void Render_ResolvesArrayIndexes()
        {
            var renderer = new InlineTemplateRenderer();
            var result = renderer.Render("{{Items[1]}}", new Model());
            Assert.Equal("b", result);
        }

        [Fact]
        public void Render_LeavesUnresolvedPlaceholdersIntact()
        {
            var renderer = new InlineTemplateRenderer();
            var result = renderer.Render("{{Missing}} and {{Name}}", new Model());
            Assert.Equal("{{Missing}} and Alice", result);
        }

        [Fact]
        public void Render_WithNullModel_LeavesPlaceholdersIntact()
        {
            var renderer = new InlineTemplateRenderer();
            var result = renderer.Render("{{Name}}", null);
            Assert.Equal("{{Name}}", result);
        }
    }
}
