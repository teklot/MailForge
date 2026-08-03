namespace MailForge.Templates
{
    /// <summary>A registry of named Razor email templates.</summary>
    public interface ITemplateRegistry
    {
        /// <summary>Registers a named template, replacing any existing template with the same name.</summary>
        void Register(string name, string content);

        /// <summary>Returns the content of a named template.</summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown when the template is not registered.</exception>
        string Get(string name);

        /// <summary>Returns true when a template with the given name is registered.</summary>
        bool Contains(string name);
    }
}
