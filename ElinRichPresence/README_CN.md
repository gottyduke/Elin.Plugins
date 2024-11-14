# 需要Fixed Package Loader，Elin 0.23.26

## Elin Discord丰富状态（ERPC）
为Elin添加Discord丰富状态，让整个服务器都能~~视奸~~欣赏您的魅魔钢琴家...

支持语言：简体中文，英语，日语

## 功能

- 显示您的职业图标、职业信息和种族信息（悬停时显示）
- 显示您当前的地图、危险等级和日期（悬停时显示）
- 显示不同区域的封面图片（更多区域会逐步添加）
- 自定义丰富状态短语

## 待办事项

按照以下顺序为不同区域添加更多图片：
- 更多城镇
    - 德尔菲 ✔
    - 阿库里·提欧拉 ✔
- 地牢
- 知名地点
- 不同生态区
- 自定义职业图标

## 配置

配置可以在 `Elin\BepInEx\config\dk.elinplugins.discordrpc.cfg` 中更改。

- `LangCodeOverride`  
  默认情况下，ERPC将使用与您的游戏区域相同的语言。如果您想为丰富状态显示使用不同的语言。
- `UpdateTicksInterval`  
  每次丰富存在更新之间的间隔轮次，默认情况下ERPC将等待8轮。

## 自定义

要修改或添加更多的存在短语变体，请到 `Elin\BepInEx\config\` 文件夹并编辑最新的 `erpc_localization_*.json` 文件。

请勿编辑 mod 文件夹中的基础文件，因为该文件将在每次 mod 更新时被覆盖。

## 翻译

日语翻译由 DeepL 提供。如果您希望改进这些翻译或添加对其他语言的支持，请告诉我。

[source](https://github.com/gottyduke/Elin.Plugins/tree/master/ElinRichPresence)
