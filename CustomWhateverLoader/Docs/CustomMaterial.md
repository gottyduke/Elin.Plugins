## How to Import Custom Material

Assumes you already have a material row defined in your Material sheet, by default game will not be able to load custom materials due to no color mapping.

With CWL, you can add custom color for custom material via tagging.

In the tag column of your material row, use **addCol_Main(color_hex)** and **addCol_Alt(color_hex)** to define the main and alt color of your material. The color is in hex format and must include alpha too, which forms an 8 bit number.

For example: **addCol_Main(ffff00ff),addCol_Alt(ff0000ff)**

![img](https://i.postimg.cc/QxRmp0ZY/image.png)

The color hex string is case insensitive, and **does not** begin with **#** or **0x**.

## 自定义材质

假设你已经在Material表中定义好了你的自定义材质，默认情况下游戏会因为缺失材质颜色而无法加载自定义材质。

使用CWL时，你可以通过标签设定自定义颜色。

在你的材质行 **tag** 单元格中, 使用 **addCol_Main(color_hex)** 和 **addCol_Alt(color_hex)** 来定义材质的主色和副色。颜色采用十六进制格式，并且必须包含通明度，形成一个 8 位数字。

例如： **addCol_Main(ffff00ff),addCol_Alt(ff0000ff)**

![img](https://i.postimg.cc/QxRmp0ZY/image.png)

颜色十六进制字符串不区分大小写，且 **不以** **#** 或 **0x** 开头。

## カスタムマテリアル

カスタムマテリアルをMaterial表で定義していると仮定しますが、デフォルトではゲームはマテリアルの色が欠落しているため、カスタムマテリアルを読み込むことができません。

CWLを使用する際は、タグを使ってカスタムカラーを設定できます。

マテリアルの行の **tag** セルで、 **addCol_Main(color_hex)** および **addCol_Alt(color_hex)** を使用してマテリアルの主色と副色を定義します。色は16進数形式で指定し、透明度を含む8桁の数値でなければなりません。

例： **addCol_Main(ffff00ff),addCol_Alt(ff0000ff)**

![img](https://i.postimg.cc/QxRmp0ZY/image.png)

カラーの16進数文字列は大文字と小文字を区別せず、 **#** または **0x** で始まってはいけません。
