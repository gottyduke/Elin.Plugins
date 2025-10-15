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
  + [x] Custom model parameters 
+ [x] Test services at runtime
  + [x] ...with easy UI
  + [x] Service pooling
+ [x] Character context
  + [x] Nearby characters
  + [x] Character backgrounds (Puddles provided a bunch of them)
  + [x] Character relationships (Puddles provided a bunch of them)
  + [x] Character original talk triggers
+ [x] Recent action context
  + [x] Toggle between talk-only and full action
+ [x] Zone context
  + [x] Zone backgrounds
+ [x] Environment context
+ [ ] Item/Equipment context
  + [ ] Nearby things
+ [ ] Religion context
+ [ ] Quest context
  + [ ] Random quest generation
+ [ ] Response choices
+ [x] Customize context prompts
  + [ ] ...with easy UI (currently by hot reloading files)
  + [x] Builtin localization support (by CWL)

## How to Add Services

Emmersive(Elin with AI) requires the AI service with **function-calling**(or **tool-call**) and **structured output**(or **json mode**) capabilities.

Your API keys will be encrypted locally your computer, not sent anywhere.

### Google(Free!)

Head to [Google AI Studio](https://aistudio.google.com/projects), create a project. If you are using free tier, I recommend making 3 projects (rate limit is per project, not per API key) and generate an API key for each project.

Launch game, load up a save, press Esc and go to Mods->Emmersive->Add Google Gemini.

You can modify the model as you wish, do note that free tier is likely to be slower, even so on heavier models like `gemini-2.5-pro`.

After adding all the API keys to the game, close the panel and start walking around and see some talking.

### OpenAI chatGPT

Head to [OpenAI Platform](https://platform.openai.com/api-keys), and generate a new API key.

Launch game, load up a save, press Esc and go to Mods->Emmersive->Add OpenAI Provider.

You can modify the model as you wish, but note that thinking(reasoning) is disabled by default, to avoid noticeable latencies. You can however, edit the params yourself.

### DeepSeek & Other OpenAI Compatible Providers

Head to [DeepSeek Platform](https://platform.deepseek.com/api_keys), and generate a new API key.

Launch game, load up a save, press Esc and go to Mods->Emmersive->Add OpenAI Provider.

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

### Mod Configurations

Use Mod Config GUI or edit the config file at `Elin\BepInEx\config\dk.elinplugins.emmersive.cfg`.

|Category|Setting|Type|Default|Range / Notes|Description|
|-|-|-|-|-|-|
|**Policy**|Verbose|bool|false|Automatically true in [`DEBUG` builds](https://github.com/gottyduke/Elin.Plugins/actions)|Enables verbose debug output (may spam logs)|
||Timeout|float|5f|1f – 20f|Maximum seconds for a generation request; no retry on timeout|
||Retries|int|1|0 – 5|Number of retry attempts after a failed request|
|**Context**|DisabledProviders|string|""|—|Comma-separated list of disabled context provider types|
||RecentLogDepth|int|20|0 – 100|Maximum number of previous logs fetched as context|
||RecentTalkOnly|bool|false|—|Only fetch talk logs; ignore combat/gameplay info|
|**Scene**|MaxReactions|int|4|1 – 8|Max reactions allowed in a single scene request|
||NearbyRadius|int|4|2 – 8|Tiles radius to scan for nearby characters|
||TurnsCooldown|int|12|0 – 100|Minimum turns required before the next scene request|
||SecondsCooldown|float|6f|0 – 100|Minimum real-time seconds before next scene request|
||SceneTriggerWindow|float|0.05f|0f – 1f|Buffer window to capture scene-triggering talks; prevents everyone talking at once|
||BlockCharaTalk|bool|true|—|Block original character talks and use them for scene context; non-generic talks may be skipped if cooldown/API unavailable|

## Feedback

For any suggestions, feedbacks, bug reports, or feature requests, ping Omega at Elona discord.
