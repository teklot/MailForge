using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MailForge.Core.Templates;

namespace MailForge.Core.Templates
{
    /// <summary>
    /// Renders <c>{{Property.Path}}</c> placeholders by walking the property graph of the model.
    /// Array and <see cref="IList"/> indices are supported (for example, <c>{{Items[0].Name}}</c>).
    /// </summary>
    public sealed class InlineTemplateRenderer : IInlineTemplateRenderer
    {
        private static readonly Regex Placeholder = new Regex(@"\{\{([A-Za-z_][A-Za-z0-9_.\[\]]*)\}\}", RegexOptions.Compiled);

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache =
            new ConcurrentDictionary<Type, PropertyInfo[]>();

        /// <summary>Renders a template by substituting model property placeholders.</summary>
        public string Render(string template, object model)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            return Placeholder.Replace(template, match =>
            {
                var path = match.Groups[1].Value;
                return Resolve(model, path) ?? match.Value;
            });
        }

        /// <summary>Resolves a property path against a model, or null when it cannot be resolved.</summary>
        private static string Resolve(object model, string path)
        {
            object current = model;
            foreach (var segment in path.Split('.'))
            {
                var (name, index) = SplitIndex(segment);
                if (current == null)
                    return null;

                current = GetPropertyValue(current, name);
                if (current == null)
                    return null;

                if (index.HasValue)
                {
                    if (current is IList list)
                    {
                        if (index.Value < 0 || index.Value >= list.Count)
                            return null;
                        current = list[index.Value];
                    }
                    else if (current is Array array)
                    {
                        if (index.Value < 0 || index.Value >= array.Length)
                            return null;
                        current = array.GetValue(index.Value);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return current == null ? null : Convert.ToString(current, CultureInfo.InvariantCulture);
        }

        /// <summary>Splits a path segment into a property name and an optional index.</summary>
        private static (string Name, int? Index) SplitIndex(string segment)
        {
            var bracket = segment.IndexOf('[');
            if (bracket < 0)
                return (segment, null);

            var name = segment.Substring(0, bracket);
            var indexText = segment.Substring(bracket + 1).TrimEnd(']');
            return int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                ? (name, index)
                : (segment, null);
        }

        private static object GetPropertyValue(object target, string propertyName)
        {
            var type = target.GetType();
            var properties = PropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            foreach (var property in properties)
            {
                if (string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                    return property.GetValue(target);
            }
            return null;
        }
    }
}
