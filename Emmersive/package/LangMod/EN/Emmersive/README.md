## Prompt Files

- `SystemPrompt.txt`: main instructions, sent with every AI conversation request.
    - Keep the `{{max_reactions}}` and `{{language_code}}` placeholders. They are replaced with the reaction limit and
      the game language.
    - Keep the output format: a JSON array of `{"uid","text","duration","delay"}` items, with an optional `emote`. Any
      other output can't be played.
- `MemoryPrompt.txt`: instructions for the memory summarizer. Its output must be a JSON array such as
  `[{"fact":"...","importance":3}]`. Placeholders are not replaced in this file.
- `Characters/`, `Relations/`, `Zones/`: background prompts. See the README in each folder.

## Overriding

Mod updates overwrite the files in the mod folder, so don't edit them there. Put your own version at the same path
under:

`%USERPROFILE%\AppData\LocalLow\Lafrontier\Elin\Emmersive\Custom\Emmersive\`

The Edit button in the Elin with AI panel copies the current prompt there and opens it, and Reset deletes your copy.
Files
in `Custom` take priority over every mod, and changes are reloaded automatically.

Other mods can ship files with the same layout under their own `LangMod/<language>/Emmersive/`.