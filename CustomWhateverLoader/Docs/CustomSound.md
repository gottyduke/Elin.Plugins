## How to Add Custom Sound

Sound files should be in **wav** format, with the filename serving as the sound ID. A default metadata JSON is generated upon loading, allowing you to edit and apply sound file metadata upon the next game launch. **You can override existing in-game sounds using the same ID**. 

By setting **"type"**: **"BGM"** in the metadata, the sound file will be instantiated as **BGMData** instead of **SoundData**. You can also customize the BGM parts in the metadata.

Subdirectories in the **Sound** folder will serve as ID prefixes. For example, AI_PlayMusic will use **Instrument/sound_id**, so you should place the sound file in the Instrument folder if you plan to replace instrument sounds.

## 自定义音频

音频文件应为 **wav** 格式，文件名作为音频ID。加载时会生成默认的同名元数据JSON，允许您编辑并在在下次游戏启动时应用音频文件元数据。**您可以使用相同的ID覆盖现有的游戏内音频**。

通过在元数据中设置"type": "BGM"，音频将作为**BGMData**而不是**SoundData**实例化。您还可以在元数据中自定义BGM的小节部分。

**Sound**文件夹中的子目录将作为音频ID前缀。例如，AI_PlayMusic将使用Instrument/sound_id，因此如果您打算替换乐器音乐，应该将同名音频文件放在Instrument文件夹中。

## 音声/BGM

オーディオファイルは **wav** 形式で、ファイル名がオーディオIDとして使用されます。読み込まれると、デフォルトの同名メタデータJSONが生成され、編集して次回ゲーム起動時にオーディオファイルのメタデータを適用できます。**同じIDを使用して既存のゲーム内オーディオを上書きできます**。

メタデータで"type": "BGM"を設定すると、オーディオは**BGMData**としてではなく**SoundData**としてインスタンス化されます。また、メタデータ内でBGMの小節部分をカスタマイズすることもできます。

**Sound**フォルダー内のサブディレクトリはオーディオIDのプレフィックスとして使用されます。たとえば、AI_PlayMusicはInstrument/sound_idを使用するため、楽器音楽を置き換える場合は、同名のオーディオファイルをInstrumentフォルダーに配置してください。
