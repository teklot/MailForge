using System.Collections.Generic;

namespace MailForge.Core.Templates
{
    /// <summary>Default in-memory <see cref="ITemplateRegistry"/> implementation.</summary>
    public sealed class TemplateRegistry : ITemplateRegistry
    {
        private readonly Dictionary<string, string> _templates = new Dictionary<string, string>();

        /// <summary>Registers a named template, replacing any existing template with the same name.</summary>
        public void Register(string name, string content)
        {
            _templates[name] = content;
        }

        /// <summary>Returns the content of a named template.</summary>
        public string Get(string name)
        {
            if (_templates.TryGetValue(name, out var content))
                return content;
            throw new KeyNotFoundException($"No template named '{name}' is registered.");
        }

        /// <summary>Returns true when a template with the given name is registered.</summary>
        public bool Contains(string name) => _templates.ContainsKey(name);
    }
}
