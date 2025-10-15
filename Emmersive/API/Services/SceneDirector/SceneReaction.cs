// ReSharper disable InconsistentNaming

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Emmersive.API.Services.SceneDirector;

public class SceneReaction
{
    public required int uid { get; set; }
    public required string text { get; set; }
    public required float duration { get; set; } = 2f;
    public required float delay { get; set; } = 0f;

    internal static string Schema =>
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

    [field: AllowNull]
    internal static JObject OpenAiSchema =>
        field ??= JObject.FromObject(new {
                type = "json_schema",
                json_schema = JObject.FromObject(new {
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
                                        duration = new { type = "number", format = "float" },
                                        delay = new { type = "number", format = "float" },
                                    },
                                    required = new[] { "uid", "text", "duration", "delay" },
                                    additionalProperties = false,
                                },
                            },
                        },
                        required = new[] { "items" },
                        additionalProperties = false,
                    },
                }),
            }
        );
}