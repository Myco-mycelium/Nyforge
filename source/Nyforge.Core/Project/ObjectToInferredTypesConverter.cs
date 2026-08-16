using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nyforge.Core.Project;

/// <summary>
/// Without this, System.Text.Json deserializes any <c>object?</c>-typed
/// value — <see cref="Nui.NuiComponent.Properties"/>,
/// <see cref="Nui.NuiCondition.Value"/>, <see cref="Nui.NuiAction.Arguments"/>,
/// <see cref="Nui.NuiDocument.States"/> — into a boxed <see cref="JsonElement"/>
/// rather than a native <c>bool</c>/<c>string</c>/number. That's silently
/// fatal for any code that pattern-matches those values (<c>is bool</c>,
/// <c>is string</c>) after a load-from-file round trip: the match simply
/// fails, with no exception and no obvious symptom beyond "the theme
/// button doesn't do anything." <see cref="BehaviorEvaluator"/>
/// side-steps this by comparing via <c>ToString()</c> rather than pattern
/// matching, which is why conditions kept working even before this
/// converter existed — but <c>Nyforge.Shell</c>'s <c>BehaviorDispatcher</c>
/// and <c>PreviewViewModel</c> do pattern-match, so this converter is not
/// optional. Registered in <see cref="ProjectSerializer"/>.
///
/// This is the standard documented pattern for this exact problem
/// ("deserialize inferred types to object properties").
/// </summary>
internal sealed class ObjectToInferredTypesConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var longValue)) return longValue;
                return reader.GetDouble();
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Null:
                return null;
            default:
                // Nested objects/arrays inside a Properties/Arguments/States
                // value aren't part of v0.1–v0.4's scope (see NUI-SCHEMA.md
                // Non-Goals) — fall back to a JsonElement rather than throw,
                // so an unexpected shape doesn't crash a whole file load.
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.Clone();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // Serialize using the value's actual runtime type rather than the
        // static `object` type — this is what avoids infinitely
        // re-entering this same converter.
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
