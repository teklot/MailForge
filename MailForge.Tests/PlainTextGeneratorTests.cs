using MailForge.Core;

namespace MailForge.Tests
{
    public class PlainTextGeneratorTests
    {
        [Fact]
        public void Generate_StripsTagsAndDecodesEntities()
        {
            var html = "<h1>Hello</h1><p>This is &amp; that.</p>";
            var text = PlainTextGenerator.Generate(html);
            Assert.Contains("Hello", text);
            Assert.Contains("This is & that.", text);
            Assert.DoesNotContain("<", text);
        }

        [Fact]
        public void Generate_AddsLineBreaksAtBlockBoundaries()
        {
            var html = "<p>First</p><p>Second</p><br><p>Third</p>";
            var text = PlainTextGenerator.Generate(html);
            Assert.Contains("First\n", text);
            Assert.Contains("Second\n", text);
            Assert.Contains("Third", text);
        }

        [Fact]
        public void Generate_ReturnsNullOrEmptyAsIs()
        {
            Assert.Null(PlainTextGenerator.Generate(null));
            Assert.Equal(string.Empty, PlainTextGenerator.Generate(string.Empty));
        }
    }
}
