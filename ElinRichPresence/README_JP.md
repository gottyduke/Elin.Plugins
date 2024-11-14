# Elin 0.23.26にはFixed Package Loaderが必要です

## Elinリッチプレゼンス (ERPC)
Elinのためのリッチプレゼンスを追加することで、Discordの体験を向上させ、サーバー全体があなたのサキュバスピアニストをストーカーし、称賛できるようにします...

対応言語: 简体中文, English, Japanese

![base building](https://i.postimg.cc/tTY6vdXY/base-build.png)
![travel](https://i.postimg.cc/mk9HBb14/travel.png)
![nefia](https://i.postimg.cc/ZR2dG9Rj/nefia.png)
![explore](https://i.postimg.cc/TPXG4Tfm/town.png)

## 機能

- クラスアイコン、クラス情報、種族情報を表示（ホバー時）
- 現在のマップ、危険度、日付を表示（ホバー時）
- 異なるゾーンのカバー画像を表示（さらに多くのゾーンが追加される予定; アートアセットの作成には時間がかかります）
- プレゼンスフレーズをカスタマイズ

## TODO

次の順序で異なるゾーンの画像を追加します:
- さらなる町
  - デルフィ ✔
  - アクイリ テオラ ✔
- ネフィアス
- 注目の場所
- 異なるバイオーム
- カスタムクラスアイコン

## 設定

設定は `Elin\BepInEx\config\dk.elinplugins.discordrpc.cfg` で変更できます。

- `LangCodeOverride`
  デフォルトでは、ERPCはゲームのロケールと同じ言語を使用します。リッチプレゼンス表示に異なる言語を使用したい場合は。
- `UpdateTicksInterval`
  各リッチプレゼンス更新の間のティック数、デフォルトではERPCは8ティック待機します。

## カスタマイズ

プレゼンスフレーズのバリエーションを変更または追加するには、`Elin\BepInEx\config\` フォルダーに移動し、最新の `erpc_localization_*.json` ファイルを編集してください。

モッドフォルダー内のベースファイルは編集しないでください。このファイルはモッドが更新されるたびに上書きされます。

## 翻訳

日本語の翻訳はDeepLによって提供されています。改善したり、他の言語のサポートを追加したい場合は、お知らせください。

[source](https://github.com/gottyduke/Elin.Plugins/tree/master/ElinRichPresence)
