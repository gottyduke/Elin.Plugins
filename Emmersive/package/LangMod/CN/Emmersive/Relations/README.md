## 关系提示词

文件名：`<id1>+<id2>.txt`，例如 `ashland+fiama.txt`。也可以写更多 id，例如 `ashland+fiama+loytel.txt`。

- id 是 `SourceChara` 的 id。玩家用 `player`。
- 顺序无关：`fiama+ashland.txt` 与 `ashland+fiama.txt` 相同。
- 文件名就是键，整个文件都是提示词，没有标题行。
- 只有当所有列出的角色都在附近时才会发送该提示词。
- 含有未知 id 的文件会被跳过，并在 `Player.log` 中记录警告。