# Templates

Templates translate an `Email<TModel>` into its final bodies before the pipeline validates
and sends. A registry holds named templates; renderers resolve content with a model.

## Inline Templates

`InlineTemplateRenderer` substitutes `{{Property.Path}}` placeholders by walking the model's
property graph. Arrays and `IList` indices are supported: `{{Items[0].Name}}`.

```csharp
var renderer = new InlineTemplateRenderer();
var html = renderer.Render("<h1>Hi {{Name}}, welcome back</h1>", new { Name = "Jane" });
```

In `Email<TModel>` subclasses this is what `SetHtml("...{{Model.Prop}}...")` uses.

## Razor Templates

`RazorEmailTemplateRenderer` compiles Razor templates with
[RazorEngineCore](https://www.nuget.org/packages/RazorEngineCore). Compiled templates are
cached by content so each unique template compiles once.

> **Model visibility:** Razor templates bind the model through the runtime dynamic
> dispatcher, so the model type must be publicly visible. A `public` record or class works;
> a `private`/`internal` nested model cannot be bound.

```csharp
services.AddMailForge(builder => builder
    .RegisterTemplate("receipt", "<p>Receipt for @Model.Amount paid (ref @Model.Reference).</p>"));
```

A typed email then selects it by name:

```csharp
public class ReceiptEmail : Email<ReceiptModel>
{
    public ReceiptEmail(ReceiptModel model) : base(model)
    {
        AddTo("customer@example.com");
        SetSubject("Your receipt");
        UseTemplate("receipt");
    }
}
```

## Template Registry

`ITemplateRegistry` holds named templates:

- `Register(string name, string content)` — replaces any template with the same name
- `Get(string name)` — throws when the template is missing
- `Contains(string name)` — existence check

The default `TemplateRegistry` is an in-memory dictionary; implement `ITemplateRegistry`
for file- or database-backed storage and register it through `RegisterTemplate` or by
replacing the registered service.