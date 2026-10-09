using System.Linq;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;

namespace Emmersive.API.Plugins;

// ReSharper disable InconsistentNaming
public class SceneReaction
{
    internal static readonly string[] EmoteNames = [
        "angry",
        "sad",
        "hungry",
        "love",
        "happy",
        "idea",
    ];

    public required int uid { get; init; }
    public required string text { get; init; }
    public required float duration { get; init; }
    public required float delay { get; init; }
    public SceneDirector.CharacterEmote? emote { get; init; }

    public static string SchemaStr =>
        """
        {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "uid": {
                "type": "integer"
              },
              "text": {
                "type": "string"
              },
              "duration": {
                "type": "number",
                "format": "float"
              },
              "delay": {
                "type": "number",
                "format": "float"
              },
              "emote": {
                "type": "string",
                "enum": ["angry", "sad", "hungry", "love", "happy", "idea"]
              }
            },
            "required": [
              "uid",
              "text",
              "duration",
              "delay"
            ]
          }
        }
        """;

    public static JObject Schema => field ??= JObject.Parse(SchemaStr);

    public static KernelJsonSchema KernelSchema => field ??= KernelJsonSchema.Parse(SchemaStr);

    public static JObject OpenAiSchema =>
        field ??= JObject.FromObject(new {
            type = "json_schema",
            json_schema = new {
                strict = true,
                name = "scene_reaction_array",
                schema = new {
                    type = "object",
                    properties = new {
                        items = new {
                            type = "array",
                            items = new {
                                type = "object",
                                properties = new {
                                    uid = new { type = "integer" },
                                    text = new { type = "string" },
                                    duration = new { type = "number" },
                                    delay = new { type = "number" },
                                    emote = new {
                                        type = new[] { "string", "null" },
                                        @enum = EmoteNames.Append<object?>(null).ToArray(),
                                    },
                                },
                                required = new[] { "uid", "text", "duration", "delay", "emote" },
                                additionalProperties = false,
                            },
                        },
                    },
                    required = new[] { "items" },
                    additionalProperties = false,
                },
            },
        });
}