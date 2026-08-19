using MailForge.Utilities;

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

        [Fact]
        public void Generate_StripsScriptBlocks()
        {
            var html = "<p>Hello</p><script>alert('xss');</script><p>World</p>";
            var text = PlainTextGenerator.Generate(html);
            Assert.Contains("Hello", text);
            Assert.Contains("World", text);
            Assert.DoesNotContain("alert", text);
            Assert.DoesNotContain("script", text);
        }

        [Fact]
        public void Generate_StripsStyleBlocks()
        {
            var html = "<p>Hello</p><style>.red{color:red;}</style><p>World</p>";
            var text = PlainTextGenerator.Generate(html);
            Assert.Contains("Hello", text);
            Assert.Contains("World", text);
            Assert.DoesNotContain("color", text);
        }

        [Fact]
        public void Generate_StripsTitleBlocks()
        {
            var html = "<p>Hello</p><title>My Page Title</title><p>World</p>";
            var text = PlainTextGenerator.Generate(html);
            Assert.Contains("Hello", text);
            Assert.Contains("World", text);
            Assert.DoesNotContain("My Page Title", text);
        }

        [Fact]
        public void Generate_StripsSelfClosingScriptTag()
        {
            var html = "<p>Before</p><script src=\"evil.js\" /><p>After</p>";
            var text = PlainTextGenerator.Generate(html);
            Assert.Contains("Before", text);
            Assert.Contains("After", text);
            Assert.DoesNotContain("evil.js", text);
        }

        [Fact]
        public void Generate_HandlesNestedScriptInContent()
        {
            var html = "<div><p>Safe</p><script>var x=1;</script><p>Also safe</p></div>";
            var text = PlainTextGenerator.Generate(html);
            Assert.Contains("Safe", text);
            Assert.Contains("Also safe", text);
            Assert.DoesNotContain("var x", text);
        }
    }
}
