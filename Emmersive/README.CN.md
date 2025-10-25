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
* [x] 支持 OpenAI 兼容的服务 (DeepSeek、Qwen等)
* [x] 支持 本地部署的模型 (webui, Ollama等)
    * [x] 自定义模型参数
* [x] 运行时测试服务
    * [x] 通过 UI 编辑
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
* [x] 物品/装备上下文
    * [x] 附近物品
* [x] 信仰上下文
* [ ] 任务上下文
    * [ ] 随机任务生成
* [ ] 响应选项
* [x] 自定义上下文提示词
    * [x] 通过 UI 编辑
    * [x] 内置本地化支持（使用 CWL）

## 如何添加 AI 服务

Emmersive（Elin with AI）要求使用的 AI 服务具备 **函数调用**（或 **工具调用**）和 **结构化输出**（或 **JSON 模式**）能力。

你的 API 密钥会在本地加密存储，不会发送到任何地方。

通过添加多个 AI 服务，Emmersive（Elin with AI）将在请求失败时将自动使用下一个可用服务进行重试。

启动游戏，载入存档，按 Esc → Mods → Emmersive 开始配置菜单。

[详细的主流API服务设置指南](./API_Setup.CN.md)

## 反馈

如有建议、反馈、Bug 报告或功能请求，请请请请请请请请请请请请请请请请请请请请请请请请请请请请<生成失败>
