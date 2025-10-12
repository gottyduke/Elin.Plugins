
// ReSharper disable InconsistentNaming

namespace Emmersive.API.Plugins.SceneDirector;

public class SceneReaction
{
    public int uid { get; set; }

    public string text { get; set; } = null!;
    public float duration { get; set; }

    public float delay { get; set; }

    internal static string Schema =>
        """
        {
          "type": "object",
          "properties": {
            "uid": {
              "type": "integer",
              "description": "The unique identifier (uid) of the character who will speak or act."
            },
            "text": {
              "type": "string",
              "description": "The text to be displayed. Use a brief, single line. Gesture and thought should be concise and contain only a few words. Do not include quotation marks. Unity rich text tags are supported."
            },
            "duration": {
              "type": "number",
              "description": "How long, in seconds, the text bubble should remain visible. Default is 2.5f."
            },
            "delay": {
              "type": "number",
              "description": "Delay, in seconds, before executing this action. Use it to chain reactions naturally."
            }
          },
          "required": ["uid", "content", "reaction_type", "duration", "delay"]
        }
        """;
}