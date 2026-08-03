namespace MailForge.Templates
{
    /// <summary>
    /// Renders inline templates that use the simple <c>{{Property.Path}}</c> placeholder syntax.
    /// This is used for <c>Html()</c>, <c>Text()</c>, and subject templates on typed emails.
    /// Unresolved placeholders are left untouched in the output.
    /// </summary>
    public interface IInlineTemplateRenderer
    {
        /// <summary>Renders a template by substituting model property placeholders.</summary>
        /// <param name="template">The template text.</param>
        /// <param name="model">The model instance.</param>
        /// <returns>The rendered output.</returns>
        string Render(string template, object model);
    }
}
