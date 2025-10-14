// ReSharper disable InconsistentNaming

namespace Emmersive.API.Services.SceneDirector;

public class SceneReaction
{
    public int uid { get; set; }

    public string text { get; set; } = null!;
    public float duration { get; set; }

    public float delay { get; set; }

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