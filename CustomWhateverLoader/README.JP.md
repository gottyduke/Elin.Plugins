## Custom Whatever Loader 任意のローダー

![Version](https://img.shields.io/badge/Version-1.16.0-R.svg)

ゲームが自動的にモジュールディレクトリからモジュール制作者のカスタムリソースをロードできるようにし、モジュール制作者がさまざまなゲーム機能を利用するプロセスを簡素化し、追加の手順を必要とせず、ローカライズサポートを拡張します。

新しいアイテム、キャラクター、要素、または音声を導入するモジュールに非常に適しており、CWLはDLLを使用して表をインポートする手間を省きます。

## サポート

- ソース表（キャラクター、アイテム、種族、対話など）
- カスタム冒険者
- カスタム商人
- カスタム能力
- カスタム宗教
- カスタムマテリアル (カスタムカラー)
- セリフ・ドラマ
- 本のテキスト
- 上記のすべてには拡張ローカリゼーション サポートが含まれています
- 音声/BGM
- 多くの修正と最適化
    - 統合インポートによりロード時間が短縮されます
    - ソーステーブルの互換性を自動的に検出します
    - ソーステーブル解析例外を再スローし、詳細情報を添付します
    - 読み込みを妨げるカスタム要素/タスク/カードを削除します
- 機能豊富な API

必要に応じて新機能を追加します。

## サンプルモジュール設定

CWLは、Modを**LangMod**フォルダーに配置することを要求します。**Lang**ではありません。そうしないと、ゲームは翻訳ツリー全体をあなたのモジュールフォルダーにコピーします。**LangMod**フォルダー内では、言語コードを使用してサブフォルダーを命名することで、任意の数のサポート言語を追加できます。例えば：

![img](https://i.postimg.cc/tJypn1Ys/image.png)

CWLがリソースをインポートする際は、現在の言語フォルダーから優先的にインポートされ、現在のElin xlsxの翻訳問題を効果的に解決します。なぜなら、ほとんどのワークシートには通常JPとENのエントリしか含まれていないからです。

## カスタムソース表

各言語フォルダーにxlsxファイルを単純に置くだけで、各xlsxワークシート上で**ModUtil.ImportExcel**を手動で呼び出す必要はありません。CWLは、SourceDataまたはSourceLangと一致する表名に基づいて、すべてのローカライズされたソースをインポートします。

注意すべきは**表名**であり、ファイル名ではありません！例えば、これはそれぞれ**SourceThing**、**SourceChara**、**LangGeneral**をインポートします。
![img](https://i.postimg.cc/vZqGNjfC/Screenshot-1.png)

サポートされているSourceData：
```
Chara, CharaText, Thing, Race, Element, Job, Obj, Material, Quest, Religion, Zone, Area, Block, Category, CellEffect, Collectible, Faction, Floor, Food, GlobalTile, Hobby, HomeResource, KeyItem, Person, Recipe, Research, SpawnList, Stat, Tactics, ThingV
```

サポートされているSourceLang：
```
General, Game, List, Word, Note
```

管理を容易にするために、ワークシートを複数のxlsxファイルに分割することもできます。xlsxファイル名は関係ありません。

ゲーム中のアイテム/キャラクター/さまざまなソースのIDを参照したい場合は、[Elin Modding Wiki](https://elin-modding-resources.github.io/Elin.Docs)を確認してください：

![img](https://i.postimg.cc/15wF6V2L/image.png)

## 特殊カスタム

[カスタム冒険者](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomAdventurer.md#カスタム冒険者)  
[カスタム商人](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomMerchant.md#カスタム商人)  
[自定義ストーリー](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomDrama.md#自定義ストーリー)  
[カスタム信仰](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomReligion.md#カスタム信仰)  
[カスタム能力](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomElement.md#カスタム能力)  
[カスタムマテリアル(Material)](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomMaterial.md#カスタムマテリアル)  
[音声/BGM](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/CustomSound.md#音声bgm)  

## 使用例

CWLの使用例をいくつか確認するには、以下のモジュール（およびその他）をご覧ください：

[若葉睦](https://steamcommunity.com/sharedfiles/filedetails/?id=3380127472)  
[The Eternal Student: Kubrika](https://steamcommunity.com/sharedfiles/filedetails/?id=3380350255)  
[Miranda, Rookie Gunner](https://steamcommunity.com/sharedfiles/filedetails/?id=3383166653)  
[Christmas Red Saber](https://steamcommunity.com/sharedfiles/filedetails/?id=3383191390)  
[Fairy Dust: Una](https://steamcommunity.com/sharedfiles/filedetails/?id=3384670717)  
[Kiria's Memory Quest DLC](https://steamcommunity.com/sharedfiles/filedetails/?id=3381789374)  
[Drill Legend: Reincarnation Eiln KasaneTeto](https://steamcommunity.com/sharedfiles/filedetails/?id=3385442190)  
[「オブザーバー」種族追加MOD](https://steamcommunity.com/sharedfiles/filedetails/?id=3385578698)  
[Custom Instrument Track](https://steamcommunity.com/sharedfiles/filedetails/?id=3374708172)  

## API

CustomWhateverLoader.dllを参照することで、**CWL.API**および**CWL.Helper**名前空間全体にアクセスできます。GitHubの関連ソースコードをご覧ください。

dllを参照してAPIを使用する場合は、CustomWhateverLoader.dllをモジュールと一緒に配布しないでください。

## 配置ファイル  
  
[詳細配置CWLの部分機能](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader/Docs/Config.md)  

## コードのローカライズ

テキストエントリをGeneralテーブルにエクスポートし、Custom Whatever LoaderにLangGeneralにインポートさせることで、実行時に **"my_lang_str".lang()** を使用してコードをローカライズできます。

![img](https://i.postimg.cc/76HS3t8M/image.png)

## 更新ログ

**1.13** カスタムマテリアルのインポートサポートが追加され、安全なロードの最適化が多数行われました。
**1.12** カスタム能力のインポートサポートが追加されました。
**1.11** CWLが他のモジュールに対して奇妙なことをしようとするバグを修正しました。  
**1.10** カスタム信仰のインポートおよびカスタム信仰/領域/派閥の肖像のサポートを追加しました。  
**1.9** 非互換のソーステーブルの自動検出およびヘッダーの再整列を追加しました。切り替え可能です。  
**1.8** 装備/アイテムにカスタム冒険者タグを追加しました。  
**1.7** 英語を第一の代替言語に設定しました。  
**1.6** カスタム冒険者関連のインポートおよびdialog.xlsxのマージのサポートを追加しました。  
**1.5** APIのリファクタリング。  
**1.4** ローカライズサポート付きのソーステーブルインポートを追加しました。  
**1.3** BGMData.Partが最初のエントリを重複する問題を修正しました。  
**1.2** カスタムオーディオのサポートを追加しました。  
**1.1** 書籍テキストのサポートを追加しました。  
**1.0** ダイアログ/ストーリーのサポートを追加しました。

## 問題がありますか？

新機能が必要な場合、フィードバックを提供したい場合、または助けが必要な場合は、気軽にコメントするか、Elona Discordで @freshcloth に連絡してください。

エラーが発生した場合は、**ユーザー名/AppData/LocalLow/Lafrontier/Elin/Player.log**を確認することを忘れないでください。CWLはそこで**多くの**情報を記録します。

[sauce](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomWhateverLoader)
