using System;
using System.Collections.Concurrent;
using MailForge.Templates;
using RazorEngineCore;

namespace MailForge.Templates
{
    /// <summary>
    /// Renders Razor templates using the RazorEngineCore engine. Compiled templates are cached
    /// by template content so each unique template is compiled only once.
    /// Model types must be publicly visible (public top-level or public nested types);
    /// private or internal types cannot be bound by the runtime dynamic dispatcher.
    /// </summary>
    public sealed class RazorEmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly RazorEngine _engine = new RazorEngine();
        private readonly ConcurrentDictionary<string, IRazorEngineCompiledTemplate> _cache =
            new ConcurrentDictionary<string, IRazorEngineCompiledTemplate>();

        /// <summary>Renders a Razor template against a model.</summary>
        public string Render<TModel>(string template, TModel model)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            var compiled = _cache.GetOrAdd(template, content => _engine.Compile(content));
            return compiled.Run(model);
        }
    }
}
