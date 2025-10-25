## API 设置示例

> [!Important]
> 当为提供者设置昵称时，不要忘记在编辑参数前点击“Reload”（重新加载）。  
> 如果你不熟悉 JSON 格式，可以使用网站 [JSONLint](https://jsonlint.com/) 来验证格式。

Emmersive（Elin with AI）需要具备 **function-calling（函数调用）** 或 **tool-call（工具调用）**，以及 **structured
output（结构化输出）** 或 **json mode（JSON 模式）** 功能的 AI 服务。

## 是否启用推理

部分模型提供“推理能力（reasoning）”，可以生成更高质量输出，但同时带来更长的生成时间与更高的 token 消耗。

考虑到 Elin 游戏的上下文，以及 Emmersive 系统指令默认只需要输出几行 JSON 的特性，启用推理功能的代价（延迟、token
消耗、响应性降低）可能得不偿失。

所有 `reasoning_effort` / `thinkingBudget` 默认设置为最低值，但你可以在参数中自行修改。

## Google Gemini（免费！）

访问 [Google AI Studio](https://aistudio.google.com/projects)，创建一个项目。  
如果你使用的是免费额度，建议创建 **3 个项目**（速率限制按项目计，不按 API key 计），并为每个项目生成一个 API key。

**模型**：`gemini-2.5-flash`  
你可以自由修改模型，但请注意免费层可能较慢，尤其是像 `gemini-2.5-pro` 这样的思考模型。  
默认模型为 `gemini-2.5-flash`。

**参数参考**：[Google Gemini API Reference](https://ai.google.dev/api/generate-content#request-body)

**免费速率限制**：每个项目每分钟 15 次请求，每个账号每天 250 次请求。

**推荐 AI 服务冷却时间**：`1`s

## NVIDIA NIM（免费！）

访问 [NVIDIA Build](https://build.nvidia.com/settings/api-keys)，创建一个新的 API key。

**基址**：`https://integrate.api.nvidia.com/v1`

**模型**：`deepseek-ai/deepseek-v3.1-terminus`  
（或其他可在 [NVIDIA 模型列表](https://docs.api.nvidia.com/nim/reference/deepseek-ai-deepseek-v3_1-terminus) 中找到的模型）

**参数：**

```json
{
  "response_format": {
    "type": "json_object"
  }
}
```

可选参数：`temperature`、`top_p`、`max_tokens`、`frequency_penalty`、`presence_penalty`

**免费层速率限制**：每分钟 40 次请求。

**推荐 AI 服务冷却时间**：`1`s

## OpenAI ChatGPT

访问 [OpenAI Platform](https://platform.openai.com/api-keys)，生成一个新的 API key。

**基址**：`https://api.openai.com/v1`

**模型**：`gpt-5-nano`
（或 [OpenAI 模型列表](https://platform.openai.com/docs/pricing) 中的任意模型）

**参数**（由 Emmersive 默认提供）：

```json
{
  "frequency_penalty": 0.6,
  "reasoning_effort": "minimal",
  "response_format": {
    "type": "json_schema",
    "json_schema": {
      // 省略部分内容
    }
  }
}
```

可选：查看 [OpenAI Chat Completion API Reference](https://platform.openai.com/docs/api-reference/chat/create)

## 其他兼容 OpenAI 的服务商

使用其他服务商非常简单：只需更改基址、模型，并调整参数即可。

### DeepSeek

访问 [DeepSeek Platform](https://platform.deepseek.com/api_keys)，生成一个新的 API key。

**基址**：`http://api.deepseek.com/v1` (或者其他服务提供商，例如硅基流动)

**模型**：`deepseek-chat`（DeepSeekV3.2-Exp 的非推理版本）

**参数：**

```json
{
  "frequency_penalty": 0.6,
  "response_format": {
    "type": "json_object"
  }
}
```

可选：查看 [DeepSeek Chat Completion API Reference](https://api-docs.deepseek.com/api/create-chat-completion)

### X.AI（grok）

访问 [X.AI](https://docs.x.ai/docs/models)，登录并生成新的 API key。

**基址**：`https://api.x.ai/v1`

**模型**：`grok-4-fast-non-reasoning`
（或 [X.AI 模型列表](https://docs.x.ai/docs/models) 中任意模型）

**参数：**

```json
{
  "response_format": {
    "type": "json_schema",
    "json_schema": {
        // 省略部分内容
    }
  }
}
```

可选：查看 [X.AI Chat Completions API Reference](https://docs.x.ai/docs/api-reference#chat-completions)

### 本地 LLM（Ollama / WebUI）

配置方式相同，只需将基址修改为本地端口。

根据所用模型调整对应的模型与参数。
