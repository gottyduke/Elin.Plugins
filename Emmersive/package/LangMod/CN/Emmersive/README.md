## 提示词文件

- `SystemPrompt.txt`：主提示词，每轮 AI 对话请求都会发送。
    - 必须保留 `{{max_reactions}}` 和 `{{language_code}}` 占位符，它们会被替换为反应条数上限和游戏语言。
    - 必须保留输出格式：由 `{"uid","text","duration","delay"}` 组成的 JSON 数组，`emote` 可选。其他格式的输出无法播放。
- `MemoryPrompt.txt`：记忆总结用的提示词。输出必须是 JSON 数组，例如 `[{"fact":"...","importance":3}]`。此文件中的占位符不会被替换。
- `Characters/`、`Relations/`、`Zones/`：背景提示词，详见各文件夹内的 README。

## 覆盖

模组更新会覆盖模组文件夹里的文件，所以不要在那里修改。请把你自己的版本放到下面目录的相同路径：

`%USERPROFILE%\AppData\LocalLow\Lafrontier\Elin\Emmersive\Custom\Emmersive\`

Elin with AI 面板里的「编辑」按钮会把当前提示词复制到这里并打开，「重置」则会删除你的副本。`Custom` 中的文件优先于所有模组，修改后会自动重新加载。

其他模组也可以在自己的 `LangMod/<语言>/Emmersive/` 下按相同结构提供这些文件。