## How to Create Custom Ability

Assumes you have setup your ability in an Element sheet already, the following entries are important:

**id**: this should be a unique number, this is the ability id.  
**alias**: the actual string id of your element.  
**type**: this C# type name corresponding to this ability.  
**group**: be it **ABILITY** or **SPELL**.  
**tag**: add **addEleOnLoad** if you want your ability to be applied to player on game load.  

The rest are up to you to define. You may take references from [Elin Modding Wiki](https://elin-modding-resources.github.io/Elin.Docs/) or Elin Sources.

For example, we want to add an ability **ActLionDance**, it should look like this:

![img](https://i.postimg.cc/90PTN1r1/doc-custom-ele.png)

![img](https://i.postimg.cc/XY6Nv31Z/image.png)

Your alias and type does not need to be the same, however, the texture of the ability icon will be using the **alias**, and the element object will be linked to **type**.

In your script dll, you should have the following code:
```cs
internal class ActLionDance : Act
{
    public override bool Perform()
    {
        pc.Say("Lion Dance!!");
        return true;
    }
}
```

The class must derive from **Element**, common ones are **Act**, **AIAct`**, **Ability**, **Spell**, depending on the usage and intention.

You can declare your class in any namespace, CWL will qualify the type name for you, so the element type only needs to be the class name itself.

Your ability icon needs to be placed within **Texture** folder, using the same alias as the file name, such as **ActLionDance.png**. If the texture size is not 48x48, CWL will resize it for you.

With the tag **addEleOnLoad**, player will gain this ability automatically upon loading.

If you do not need to utilize CWL API, then no need to reference CustomWhateverLoader.dll.

## 自定义能力

随便加载器可以导入您的自定义元素表（表名: Element）并将其添加到游戏中。然而，您的自定义元素有以下几项需要注意：

**id**：一个唯一的数字，这是元素的ID。  
**alias**：元素的别名，字符串ID。  
**type**：与此元素对应的C#类型名称。  
**group**：可以是**ABILITY**或**SPELL**。  
**tag**：如果您希望在游戏加载时将元素自动赋予给玩家，请添加**addEleOnLoad**。  

其余的由您定义。您可以参考[Elin Modding Wiki](https://elin-modding-resources.github.io/Elin.Docs/)或Elin Sources。

例如，我们想添加一个能力**ActLionDance**，它应该看起来像这样：

![img](https://i.postimg.cc/90PTN1r1/doc-custom-ele.png)

![img](https://i.postimg.cc/XY6Nv31Z/image.png)

**alias**和**type**不需要相同，但是，能力图标的纹理将参照**alias**，而元素对象示例将链接到**type**。

在您的脚本dll中，您应该有以下代码：
```cs
internal class ActLionDance : Act
{
    public override bool Perform()
    {
        pc.Say("Lion Dance!!");
        return true;
    }
}
```

该类必须从**Element**派生，常见的有**Act**、**AIAct**、**Ability**、**Spell**，具体取决于使用和意图。

您可以在任何命名空间中声明您的类，CWL会自动为您限定类型名称，因此**type**只需要是类名本身。

您的能力图标需要放置在**Texture**文件夹中，使用与**alias**相同的文件名，例如**ActLionDance.png**。如果纹理大小不是48x48，CWL会将其调整为48x48。

使用标签**addEleOnLoad**，玩家角色将自动获得此能力。

如果不需要使用CWL的API，那么无需引用CustomWhateverLoader.dll。

## カスタム能力

随便ロードは、あなたのカスタム要素テーブル（テーブル名: Element）をインポートし、ゲームに追加することができます。ただし、あなたのカスタム要素には以下の点に注意する必要があります：

**id**：ユニークな数字で、これは要素のIDです。  
**alias**：要素のエイリアス、文字列ID。  
**type**：この要素に対応するC#のタイプ名。  
**group**：**ABILITY**または**SPELL**のいずれか。  
**tag**：ゲームのロード時に要素を自動的にプレイヤーに付与したい場合は、**addEleOnLoad**を追加してください。  

残りはあなたが定義します。あなたは[Elin Modding Wiki](https://elin-modding-resources.github.io/Elin.Docs/)やElin Sourcesを参考にできます。

例えば、私たちは能力**ActLionDance**を追加したいと思っています。これは以下のようになります：

![img](https://i.postimg.cc/90PTN1r1/doc-custom-ele.png)

![img](https://i.postimg.cc/XY6Nv31Z/image.png)

**alias**と**type**は同じである必要はありませんが、能力アイコンのテクスチャは**alias**を参照し、要素オブジェクトの例は**type**にリンクされます。

あなたのスクリプトdllには、以下のコードが必要です：
```cs
internal class ActLionDance : Act
{
    public override bool Perform()
    {
        pc.Say("ライオンダンス!!");
        return true;
    }
}
```

このクラスは**Element**から派生しなければなりません。一般的には**Act**、**AIAct**、**Ability**、**Spell**などがあり、使用目的によって異なります。

あなたのクラスは任意の名前空間で宣言できます。CWLは自動的にタイプ名を制限するため、**type**はクラス名そのものだけで構いません。

あなたの能力アイコンは**Texture**フォルダに配置する必要があり、**alias**と同じファイル名を使用します。たとえば、**ActLionDance.png**です。テクスチャサイズが48x48でない場合、CWLはそれを48x48に調整します。

**addEleOnLoad**タグを使用すると、プレイヤーキャラクターはこの能力を自動的に獲得します。

CWLのAPIを使用しない場合は、CustomWhateverLoader.dllを参照する必要はありません。
