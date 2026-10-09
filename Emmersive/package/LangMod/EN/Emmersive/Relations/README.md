## Relationship Prompts

File name: `<id1>+<id2>.txt`, e.g. `ashland+fiama.txt`. More ids also work, e.g. `ashland+fiama+loytel.txt`.

- Ids are `SourceChara` ids. The player is `player`.
- Order doesn't matter: `fiama+ashland.txt` is the same as `ashland+fiama.txt`.
- The file name is the key, and the whole file is the prompt. There is no header line.
- The prompt is only sent when every listed character is nearby.
- Files with an unknown id are skipped, with a warning in `Player.log`.