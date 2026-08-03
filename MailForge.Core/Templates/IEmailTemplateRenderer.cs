namespace MailForge.Core.Templates
{
    /// <summary>
    /// Renders Razor templates registered on the MailForge builder. Templates use Razor syntax
    /// and can reference the model through <c>@Model</c>.
    /// </summary>
    public interface IEmailTemplateRenderer
    {
        /// <summary>Renders a Razor template against a model.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="template">The Razor template content.</param>
        /// <param name="model">The model instance.</param>
        /// <returns>The rendered output.</returns>
        string Render<TModel>(string template, TModel model);
    }
}
