# Elin with AI

![](./assets/Em_banner.png)

![Version](https://img.shields.io/badge/Version-Beta%20Testing-R?style=flat&labelColor=red&color=blue)

[中文](./README.CN.md)

Power up Elin with AI and LLMs, make the world alive by generating contextual aware conversations.

## Requires Custom Whatever Loader & YKFramework

+ [Custom Whatever Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=3370512305) (Pick Stable **OR** Nightly)
+ [YKFramework](https://steamcommunity.com/sharedfiles/filedetails/?id=3400020753)

## Features & Todos:

This is a **beta test** version, mainly for gathering reports and feedback.

+ [x] Support Google AI Studio (gemini)
+ [x] Support OpenAI chatGPT
+ [x] Support OpenAI-compatible providers (DeepSeek, Qwen, etc)
+ [x] Support Local LLM (webui, ollama, etc)
  + [x] Custom model parameters 
+ [x] Test services at runtime
  + [x] With easy to use UI
  + [x] Service pooling
+ [x] Character context
  + [x] Nearby characters
  + [x] Character backgrounds (Puddles provided a bunch of them)
  + [x] Character relationships (Puddles provided a bunch of them)
  + [x] Character original talk as triggers
+ [x] Recent action context
  + [x] Toggle between talk-only and full action
+ [x] Zone context
  + [x] Zone backgrounds
+ [x] Environment context
+ [x] Item/Equipment context
  + [x] Nearby things
+ [x] Religion context
+ [ ] Quest context
  + [ ] Random quest generation
+ [ ] Response choices from player
+ [x] Customize context prompts
  + [x] With easy to use UI
  + [x] Builtin localization support (by CWL)

## How to Add Services

Emmersive(Elin with AI) requires the AI service with **function-calling**(or **tool-call**) and **structured output**(or **json mode**) capabilities.

Your API keys will be encrypted locally your computer, not sent anywhere.

By adding multiple AI services, Emmersive(Elin with AI) will enable auto-retry on request failure seamlessly.

Start with loading up game, press Esc and go to Mods->Emmersive to view the config panel.

### Google(Free!)

Head to [Google AI Studio](https://aistudio.google.com/projects), create a project. If you are using free tier, I recommend making 3 projects (rate limit is per project, not per API key) and generate an API key for each project.

You can modify the model as you wish, do note that free tier is likely to be slower, even so on heavier models like `gemini-2.5-pro`.

After adding all the API keys to the game, close the panel and start walking around and see some talking.

### OpenAI chatGPT

Head to [OpenAI Platform](https://platform.openai.com/api-keys), and generate a new API key.

You can modify the model as you wish, but note that thinking(reasoning) is disabled by default, to avoid noticeable latencies. You can however, edit the params yourself.

### DeepSeek & Other OpenAI Compatible Providers

Head to [DeepSeek Platform](https://platform.deepseek.com/api_keys), and generate a new API key.

**Important!**

Depending on your provider, you'll need to change the endpoint to the correct one. E.g. for DeepSeek it will be `http://api.deepseek.com/v1`.

Modify the model to your provider, e.g. `deepseek-chat` (the thinking-disabled variant of DeepSeekV3.2-Exp).

**Important!**

Depending on your provider, you'll need to change the request parameters. E.g. for DeepSeek, open up the param file and change the entire block:
```json
"response_format": {
  "type": "json_schema",
  "json_schema": {
    ...
  }
}
```
**to**
```json
"response_format": {
  "type": "json_object"
}
```

This is because DeepSeek doesn't support json schema output mode.

## Paramaters & Configurations

### Request Parameters

You can change each AI service's parameters individually, such as `temperature`, `topP`, `presence_penalty`. After saving the params, click Reload button to load it immediately.

## Feedback

For any suggestions, feedbacks, bug reports, or feature requests, ping Omega at Elona discord.
