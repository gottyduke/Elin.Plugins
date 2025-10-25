## API Setup Examples

> [!Important]
> When setting a nickname for the provider, don't forget to click "Reload" before editing params.   
> If you are unfamilar with JSON format, use website like [JSONLint](https://jsonlint.com/).

Emmersive(Elin with AI) requires the AI service with **function-calling** (or **tool-call**) and **structured output** (
or **json mode**) capabilities.

## To Think, or Not To Think

Some models provide reasoning capability, which can produce relatively higher quality outputs, at the cost of longer
generation time and higher token usages.

Given the contexts of Elin game and the default Emmersive system instruction which only needs a few lines of json
output, it may not worth the cost to enable reasoning as it induces a higher latency, token usages, and make the talks
not as responsive.

All `reasoning_effort` / `thinkingBudget` are set to minimal by default, but you can change that in the params.

## Google Gemini(Free!)

Head to [Google AI Studio](https://aistudio.google.com/projects), create a project. If you are using free tier, I
recommend making 3 projects (rate limit is per project, not per API key) and generate an API key for each project.

Model: `gemini-2.5-flash` You can modify the model as you wish, do note that free tier is likely to be slower, even so
on heavier models like `gemini-2.5-pro`. Default model is `gemini-2.5-flash`.

Params:
[Google Gemini API Reference](https://ai.google.dev/api/generate-content#request-body)

**Free tier ratelimit**: 15 request per min per project, 250 requests per day per account

Recommended AI Service cooldown: `1`s

## NVIDIA NIM(Free!)

Head to [NVIDIA Build](https://build.nvidia.com/settings/api-keys), create a new API key.

Endpoint: `https://integrate.api.nvidia.com/v1`

Model: `deepseek-ai/deepseek-v3.1-terminus` (
Or [any other model you like](https://docs.api.nvidia.com/nim/reference/deepseek-ai-deepseek-v3_1-terminus))

Params:

```json
{
  "response_format": {
    "type": "json_object"
  }
}
```

Optional: `temperature` `top_p` `max_tokens` `frequency_penalty` `presence_penalty`

**Free tier ratelimit**: 40 requests per min

Recommended AI Service cooldown: `1`s

## OpenAI ChatGPT

Head to [OpenAI Platform](https://platform.openai.com/api-keys), and generate a new API key.

Endpoint: `https://api.openai.com/v1`

Model: `gpt-5-nano` (Or [any other model you like](https://platform.openai.com/docs/pricing))

Params: (provided by Emmersive as default)

```json
{
  "frequency_penalty": 0.6,
  "reasoning_effort": "minimal",
  "response_format": {
    "type": "json_schema",
    "json_schema": {
      // removed for brevity
    }
  }
}
```

Optional: [OpenAI Chat Completion API Reference](https://platform.openai.com/docs/api-reference/chat/create)

## Other OpenAI Compatible Providers

Using different providers is as simple as swapping the endpoint, model, and correcting params.

### DeepSeek

Head to [DeepSeek Platform](https://platform.deepseek.com/api_keys), and generate a new API key.

Endpoint: `http://api.deepseek.com/v1`

Model: `deepseek-chat` (the thinking-disabled variant of DeepSeekV3.2-Exp)

Params:

```json
{
  "frequency_penalty": 0.6,
  "response_format": {
    "type": "json_object"
  }
}
```

Optional: [DeepSeek Chat Completion API Reference](https://api-docs.deepseek.com/api/create-chat-completion)

### X.AI(grok)

Head to [X.AI](https://docs.x.ai/docs/models), login, and generate a new API key.

Endpoint: `https://api.x.ai/v1`

Model: `grok-4-fast-non-reasoning` (Or [any other model you like](https://docs.x.ai/docs/models))

Params:

```json
{
  "response_format": {
    "type": "json_schema",
    "json_schema": {
        // skipped for brevity
    }
  }
}
```

Optional: [X.AI Chat Completions API Reference](https://docs.x.ai/docs/api-reference#chat-completions)

### Local LLM (Ollama/Webui)

Same setup but swap the endpoints to your local port.

Change the model and params accordingly to your choice.