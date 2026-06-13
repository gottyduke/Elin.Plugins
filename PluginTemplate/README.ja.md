# Elin.PluginTemplate

[English](README.md) | [中文](README.zh.md) | 日本語

![nuget](https://img.shields.io/nuget/v/ElinPluginTemplate)
[![.NET SDK 10.0.x](https://img.shields.io/badge/10-green?logoColor=blue&label=dotnet%20SDK&labelColor=blue)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

[Elin](https://store.steampowered.com/app/2135150/Elin/) の MOD プロジェクトを [BepInEx](https://github.com/BepInEx/BepInEx) + [Harmony](https://github.com/pardeike/Harmony) で素早く作成するための `dotnet new` テンプレートです。

---

## 前提条件

- [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)（またはそれ以降）
- Elin がインストール済みであること（Steam）

---

## テンプレートのインストール

```pwsh
dotnet new install ElinPluginTemplate
```

既存バージョンを更新する場合：

```pwsh
dotnet new install ElinPluginTemplate --force
```

---

## MOD の作成

### IDE を使う場合（推奨）

**JetBrains Rider** または **Visual Studio** で **Elin Plugin** テンプレートを選択し、詳細設定で必要なパラメータを入力してください。

![新規プロジェクト](./new_project.png)

### CLI を使う場合

```pwsh
dotnet new elinplugin -n MyNewMod --Guid "unique.mod.id" --ModName "My New Awesome Mod"
```

| パラメータ | 説明 |
|------------|------|
| `-n`       | プロジェクト / フォルダ名 |
| `--Guid`   | MOD の一意識別子（例：`com.yourname.mod`） |
| `--ModName`| MOD の表示名 |

---

## ゲームパスの設定

Elin が**デフォルト以外**の場所にインストールされている場合、環境変数 `ElinGamePath` に Elin のルートフォルダを設定してください：

```
ElinGamePath/
├─ BepInEx/
│  └─ core/
│     └─ *.dll
└─ Elin_Data/
   └─ Managed/
      └─ *.dll
```

スタートメニューで「環境変数」を検索して設定します。

---

## ビルド

```pwsh
dotnet build
```

ビルドされた MOD は自動的に以下にコピーされます：

```
{ElinGamePath}\Package\Mod_{ModName}\
```

---

## プロジェクト構成

```
MyNewMod/
├─ Plugin.cs          ← エントリポイント（BepInEx プラグイン）
├─ AsmInfo.cs         ← アセンブリメタデータ
├─ package/           ← MOD に同梱するアセット
│  ├─ package.xml
│  ├─ preview.jpg
|  ├─ LangMod/
│  ├─ Texture/
│  └─ Sound/
└─ MyNewMod.csproj    ← .NET プロジェクトファイル
```

`package/` フォルダ内のすべてのファイルはビルド時に出力先へコピーされます。以下のようなファイルを含めることができます：

- `package.xml` — Steam ワークショップ用の MOD メタデータ
- `preview.jpg` — プレビュー画像
- `LangMod` — ソースデータシート
- `Texture/` — カスタムテクスチャ
- `Sound/` — カスタム音声
- …その他 MOD に必要なアセット

---

## クイックスタート

```pwsh
# 1. テンプレートをインストール
dotnet new install ElinPluginTemplate

# 2. 新しい MOD を作成
dotnet new elinplugin -n MyMod --Guid "com.elinplugins.myid" --ModName "My Test Mod"

# 3. ビルド
cd MyMod
dotnet build
```

MOD は `ElinGamePath/Package/Mod_MyMod/` に出力されます。Elin を起動すると BepInEx が自動的に読み込みます。

---

## ライセンス

本テンプレートは現状のまま提供されます。[Elin 改造コミュニティ](https://elin-modding.net/) で関連情報をご確認ください。
