using Newtonsoft.Json.Linq;

namespace Emmersive.API.Plugins;

// ReSharper disable InconsistentNaming
public class SceneReaction
{
    public required int uid { get; init; }
    public required string text { get; init; }
    public required float duration { get; init; }
    public required float delay { get; init; }

    internal static string SchemaStr =>
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
              }
            },
            "required": [
              "uid",
              "text",
              "duration",
              "delay"
            ],
            "additionalProperties": false
          }
        }
        """;


    internal static JObject Schema =>
        field ??= JObject.FromObject(new {
            type = "array",
            items = new {
                type = "object",
                properties = new {
                    uid = new { type = "integer" },
                    text = new { type = "string" },
                    duration = new { type = "number", format = "float" },
                    delay = new { type = "number", format = "float" },
                },
                required = new[] { "uid", "text", "duration", "delay" },
                additionalProperties = false,
            },
        });


    internal static JObject OpenAiSchema =>
        field ??= JObject.FromObject(new {
                type = "json_schema",
                json_schema = JObject.FromObject(new {
                    strict = true,
                    name = "scene_reaction_array",
                    schema = new {
                        type = "object",
                        properties = new {
                            items = Schema,
                        },
                        required = new[] { "items" },
                        additionalProperties = false,
                    },
                }),
            }
        );
}