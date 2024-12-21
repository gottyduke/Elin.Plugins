### Logging.Verbose = false
Verbose information that may be helpful(spamming) for debugging  
Debug用的信息输出  

### Logging.Execution = true
Measure the extra loading time added by CWL, this is displayed in Player.log  
记录CWL运行时间  

### Caching.Paths = true
Cache paths relocated by CWL instead of iterating new paths  
缓存CWL重定向的路径而不是每次重新搜索  

### Caching.Sprites = true
Cache sprites created by CWL instead of creating new from textures  
缓存CWL生成的贴图而不是每次重新构建  

### Dialog.VariableQuote = true
For talk texts, allow both JP quote 「」 and EN quote "" with current language quote to be used as `Msg.colors.Talk` identifier  
对话文本允许日本引号和英语引号以及当前语言的引号同时作为Talk颜色检测词  

### Patches.FixBaseGameAvatar = true
When repositioning custom character icon positions, let CWL fix base game characters too  
E.g. fairy icons are usually clipping through upper border  
在重新定位自定义角色头像位置时，让CWL也修复游戏本体角色头像位置。例如，妖精角色的头像通常会超出边界  

### Patches.QualifyTypeName = true
When importing custom classes for class cache, let CWL qualify its type name  
Act, Condition, Trait  
当为类缓存导入自定义类时，让CWL为其生成限定类型名  

### Patches.SafeCreateClass = true
When custom classes fail to create from save, let CWL replace it with a safety cone and keep the game going  
当自定义类无法加载时，让CWL将其替换为安全锥以保持游戏进行  

### Source.AllowProcessors = true
Allow CWL to run pre/post processors for workbook, sheet, and cells.  
允许CWL为工作簿、工作表、单元格执行预/后处理  

### Source.NamedImport = true
When importing incompatible source sheets, try importing via column name instead of order  
当导入可能不兼容的源表时，允许CWL使用列名代替列序导入  

### Source.RethrowException = true
Rethrow the excel exception as SourceParseException with more details attached  
当捕获Excel解析异常时，生成当前单元格详细信息并重抛为SourceParseException  

### Source.SheetInspection = true
When importing incompatible source sheets, dump headers for debugging purposes  
当导入可能不兼容的源表时，吐出该表的详细信息  

### Source.SheetMigrate = false
(Experimental, disabled due to frequent game update)  
When importing incompatible source sheets, generate migrated file in the same directory  
(实验性, 游戏更新频繁暂时禁用)  
当导入可能不兼容的源表时，在同一目录生成当前版本的升级表  

### Source.TrimSpaces = true
Trim all leading and trailing spaces from cell value  
Requires Source.AllowProcessors to be true  
移除单元格数据的前后空格文本，需要允许执行单元格后处理  
