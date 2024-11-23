# Custom Dialog Loader

Allows the game to load modders' custom `Dialog/Drama` sheets from mod folder.

# Localization

Because of how game handles localizations and such, to make a localized version(that's not [b]EN[/b] or [b]JP[/b]) of your dialog sheet, you cannot use the same folder structure as the game's built-in translation mods, instead you need to suffix the language code to that sheet.
```
└── Mod_MyDialogMod/
    ├── Lang/
    │   └── Dialog/
    │       └── Drama/
    │           ├── mycustomNPC.xlsx
    │           ├── mycustomNPC_CN.xlsx
    │           └── mycustomNPC_KR.xlsx
    │           └── mycustomNPC_XX.xlsx
    └── package.xml
``` 

> [!WARNING]  
> Do not put a sub folder under `Lang` with the language code name, like `Lang/CN`. Game will duplicate entire translation folder in your mod folder.

[sauce](https://github.com/gottyduke/Elin.Plugins/tree/master/CustomDialogLoader)
