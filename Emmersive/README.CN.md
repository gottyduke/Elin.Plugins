# Elin with AI

![](./assets/Em_banner.png)

![版本](https://img.shields.io/badge/Version-Beta%20Testing-R?style=flat\&labelColor=red\&color=blue)

[English](./README.md)

使用 AI 大语言模型增强 Elin，让世界充满生机，生成具有环境感知的对话。

## 依赖 Custom Whatever Loader & YKFramework

* [Custom Whatever Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=3370512305) (使用**正确的**版本)
* [YKFramework](https://steamcommunity.com/sharedfiles/filedetails/?id=3400020753)

将这俩模组置于 Elin with AI 上方。

## 功能与饼

这是一个**测试版**，用于收集反馈并优化。

* [x] 支持 Google AI Studio (Gemini)
* [x] 支持 OpenAI chatGPT
* [x] 支持 OpenAI 兼容的服务（DeepSeek、Qwen 等）
    * [x] 自定义模型参数
* [x] 运行时测试服务
    * [x] 提供简单 UI
    * [x] 服务池管理
* [x] 角色上下文
    * [x] 附近角色
    * [x] 角色背景
    * [x] 角色关系
    * [x] 角色原始对话触发场景
* [x] 最近动作上下文
    * [x] 切换仅对话 / 全动作模式
* [x] 区域上下文
    * [x] 区域背景
* [x] 环境上下文
* [ ] 物品/装备上下文
    * [ ] 附近物品
* [ ] 信仰上下文
* [ ] 任务上下文
    * [ ] 随机任务生成
* [ ] 响应选项
* [x] 自定义上下文提示词
    * [ ] 通过 UI 编辑（当前通过热重载文件的方式）
    * [x] 内置本地化支持（使用 CWL）

## 如何添加 AI 服务

Emmersive（Elin with AI）要求使用的 AI 服务具备 **函数调用**（或 **工具调用**）和 **结构化输出**（或 **JSON 模式**）能力。

你的 API 密钥会在本地加密存储，不会发送到任何地方。

通过添加多个 AI 服务，Emmersive（Elin with AI）将在请求失败时将自动使用下一个可用服务进行重试。

### Google（免费！）

访问 [Google AI Studio](https://aistudio.google.com/projects)，创建一个项目。如果使用免费套餐，建议创建 3 个项目（速率限制按项目计算，而非
API 密钥），并为每个项目生成一个 API 密钥。

启动游戏，载入存档，按 Esc → Mods → Emmersive → 添加 Google Gemini。

你可以根据需求修改模型，但免费套餐可能会较慢，尤其是像 `gemini-2.5-pro` 这样的思考模型。

### OpenAI chatGPT

访问 [OpenAI 平台](https://platform.openai.com/api-keys)，生成一个新的 API 密钥。

启动游戏，载入存档，按 Esc → Mods → Emmersive → 添加 OpenAI 服务。

你可以修改模型，但默认禁用“思考/推理”以避免明显延迟。如果需要，可以自行编辑参数。

### DeepSeek 及其他 OpenAI 兼容模型

访问 [DeepSeek 平台](https://platform.deepseek.com/api_keys)，生成新的 API 密钥。

启动游戏，载入存档，按 Esc → Mods → Emmersive → 添加 OpenAI 服务。

**注意事项：**

* 根据模型提供商要求，需要将基址改为正确的地址。例如 DeepSeek 的基址是 `http://api.deepseek.com/v1`。
* 修改模型名称为对应服务，例如 `deepseek-chat`（DeepSeekV3.2-Exp 的禁用思考版本）。
* 根据模型提供商要求，需要修改请求参数。例如 DeepSeek 不支持 JSON Schema 输出模式，需要将参数块：

```json
"response_format": {
  "type": "json_schema",
  "json_schema": {
    ...
  }
}
```

改为：

```json
"response_format": {
  "type": "json_object"
}
```

## 参数 & 配置项

### 请求参数

你可以单独修改每个 AI 服务的请求参数，如 `temperature`、`topP`、`presence_penalty` 等。保存参数后，点击“Reload”即可立即生效。

### Mod 配置

使用 Mod Config GUI 或者 编辑配置文件 `Elin\BepInEx\config\dk.elinplugins.emmersive.cfg`。

| 类别          | 设置                 | 类型     | 默认    | 范围 / 说明                                                                     | 描述                                        |
|-------------|--------------------|--------|-------|-----------------------------------------------------------------------------|-------------------------------------------|
| **Policy**  | Verbose            | bool   | false | 在 [`DEBUG` 构建](https://github.com/gottyduke/Elin.Plugins/actions) 中自动为 true | 启用详细调试输出（打印大量日志）                          |
|             | Timeout            | float  | 5f    | 1f – 20f                                                                    | 生成请求最大等待秒数；超时不重试                          |
|             | Retries            | int    | 1     | 0 – 5                                                                       | 请求失败后的重试次数                                |
| **Context** | DisabledProviders  | string | ""    | —                                                                           | 禁用的上下文提供器类型，逗号分隔                          |
|             | RecentLogDepth     | int    | 20    | 0 – 100                                                                     | 获取最近日志的最大条数                               |
|             | RecentTalkOnly     | bool   | false | —                                                                           | 仅获取对话日志，忽略战斗/游戏信息                         |
| **Scene**   | MaxReactions       | int    | 4     | 1 – 8                                                                       | 单次场景请求允许的最大反应数                            |
|             | NearbyRadius       | int    | 4     | 2 – 8                                                                       | 扫描附近角色的格子半径                               |
|             | TurnsCooldown      | int    | 12    | 0 – 100                                                                     | 下一次场景请求的最小回合数                             |
|             | SecondsCooldown    | float  | 6f    | 0 – 100                                                                     | 下一次场景请求的最少实际秒数                            |
|             | SceneTriggerWindow | float  | 0.05f | 0f – 1f                                                                     | 捕获触发对话的缓冲窗口；防止所有角色同时发言                    |
|             | BlockCharaTalk     | bool   | true  | —                                                                           | 阻止原始角色对话，用于场景上下文；如果冷却或 API 不可用，非通用对话可能被跳过 |

## 反馈

如有建议、反馈、Bug 报告或功能请求，请请请请请请请请请请请请请请请请请请请请请请请请请请请请<生成失败>
