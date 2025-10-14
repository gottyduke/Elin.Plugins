// ReSharper disable InconsistentNaming

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
                "type": "integer",
              },
              "text": {
                "type": "string",
              },
              "duration": {
                "type": "number",
                "format": "float",
              },
              "delay": {
                "type": "number",
                "format": "float",
              }
            },
            "required": ["uid", "text", "duration", "delay"]
          }
        }
        """;
}