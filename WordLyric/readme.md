挖坑真好玩。

开发中

|项目名|项目说明|
|------|--------|
|WordLyric|C#实现的WordLyric解析库，初始版本已完成|
|SimpleLyricConsole|一个基于控制台的*普通*歌词解析播放器实现(仅实现了进度条和歌词)|
|WordLyricConsole|一个基于控制台的*分字*歌词解析播放器实现(仅实现了进度条和歌词)|
|WordLyricGUIEditor|图形化的歌词编辑工坊，正在编写中|

# 这是什么
这是一个能够解析、编辑LRC格式的`C#`库，不依赖任何其它第三方库，同时它还扩充了标准LRC功能，支持扩展LRC格式，添加分字显示、翻译记录等功能。
此外还提供了两个适配器，将进度条同步到行号及激活组号上，可用于标准LRC和分字LRC的UI显示，前者逻辑简单，后者逻辑比较复杂。经过配置后，这些适配器可以通过播放进度直接接管歌词相关的UI控件。
如果只想解析标准的LRC格式，可以使用`SimpleLyricAdapter`。详见下文相关内容。  
这里的分字并非是按从左到右的顺序分字，这旨在为翻译文本提供最大限度的视觉支持
本文为每行歌词都加入了style标记，可以被解析，但这个标记的具体内容目前可以是任意的，还没有具体的实现。在未来的版本中，这个标记将被用来定义字体、动画行为。

# 扩充LRC格式
定义`分字lrc`格式。

分字lrc格式是更加严格的lrc文件格式，提供分字、翻译、文件编码、渲染建议等信息。
一个标准的LRC解析器可以解析分字lrc文件(只会普通地显示歌词)，一个分字LRC解析器也可以解析标准的LRC文件。

- 一个歌词由若干行构成。
- 每行歌词可以有一个翻译语言。
- 每行歌词（及翻译语言）分段，由若干个“激活组”构成。
- 每个激活组由若干个字符构成，一个字符可能包含多个字节，具体取决于歌词文件的存储方式，实际上，一个激活组就是一个`string`。
- 激活组可以被激活，激活时间相对于该行歌词显示时间记录，不一定是递增的，但必须大于0，同一时刻可以激活多个激活组，一个激活组只有一个激活时间。
- 歌词行出现时，激活时间为0的组被立即激活。
- 歌词行消失时，该行全部的激活组不一定都是被激活的，这些组永远不会被激活

# 与标准LRC格式兼容 #

当标准LRC的解析程序能正确处理下列特性时，分字lrc文件可以被其兼容地解析：

* 文件定义按行为单位，每行可以包含注释标签、标识标签、时间标签，其中时间标签之后紧跟歌词。至少能够处理每行出现一个标识标签或一个时间标签，时间标签位于行首的情况。
* 能够忽略[注释标签][百度百科_软件开发标准]`[:]`开头的行即可
* 能够忽略未知的标识标签，如`[st:@]`
* 正确识别`[mm:ss.ff]`格式的时间标签

如果要按照普通的LRC解析，这个库生成的所有的分字LRC文件都满足如下性质：

- 每行都以一个标签开头，最多包含一个标签
- 以注释标签`[:]`开头的行应被忽略
- 以标识标签如`[by:someone]`开头的行，标签名一定是英文开头的，且标签之后不含其它信息，立即换行，不认识的标签名应该被忽略
- 以时间标签如`[01:5.120]`开头的行，之后立即紧跟一个歌词，直到换行，小数点一定存在，小数位数至少有一位
- 所有位置的换行符与具体环境有关，Windows下是`\r\n`
- 提供生成不含多余信息的严格标准的LRC格式的方法(换行符与环境有关)

这个库的解析程序使用正则表达式来解析歌词，同时支持处理同一行混合各种标签、不统一的换行符、注释标签、不同格式的时间等信息。

# 解析扩展LRC格式

TODO(就是没写)

# 解析QRC格式和KRC格式 #

TODO(就是没写)

## 注释标签 ##
注释标签为`[:]`，注释标签起直到行末的内容需要被忽略。

## 时间标签(TimeTag) ##

`[mm:ss.ff]`时间标签被转换为毫秒数，对应时间为`s = mm*60 + ss.ff`。  
至少要能处理点号存在，严格按照`[??:??.??]`的编写的情况。
这个正则可以解析时间标签：
```
\[(\d+):(\d+.?\d*)\]
```  
|分组|说明|
|---------:|-----------|
|1|分钟，保证是正整数|
|2|秒，保证是正浮点|

## 标识标签(IdTag) ##

至少要能忽略未知的**标识标签**，能够处理所有的**标识标签**都出现在**时间标签**之前的情况,标签名称是字母开头的  
这个正则可以解析标识标签：
```
\[(\w+):(.*?)\]
```
|分组|说明|
|---------:|-----------|
|1|标签名|
|2|标签内容|


# 分字LRC文件扩展 #

## 扩展的标识标签定义 ##
- `[sm:*]` 此标签定义**分字解析符**(split mark)  
	例：`[sm:++]`定义分字解析符为`++`
- `[ttm:*]` 此标签定义**翻译解析符**(translate text mark)
- `[ttsm:*]` 此标签定义翻译分字解析符(translate text split mark)
- `[st:*]` 此标签定义**样式style解析符**。样式是建议性的，可以忽略。样式和播放控件配合起来可以让歌词显示更加丰富多彩。
- `[encoding:*]` 定义文件的编码，可以没有，为了确保解析，必须放在文件首部，全小写无空格，标签内只能出现ascii打印字符。主要用于文件在介质或网络上按字节方式传输时记录编码，解析标签时可忽略。建议使用utf-8。  
	例：`[encoding:utf-8]`

## 注释解析 ##
当注释符号`[:]`之后紧跟**解析符**时，之后的内容会被相对应的解析程序解析，注释标签一般位于行首。

如：若分字解析符为`++`，元数据为`1|2|4|7|3|`，则按以下格式表示此分字解析元数据：
`[:]++1|2|4|7|3|`，分字解析元数据描述其后紧跟的第一个非注释行**激活组**的情况。
	
每个字符都有激活时间，激活时间是相对于当前行歌词显示时间的，沿进度条前进方向的偏移。

假设有如下歌词：
```
[1:02.30]你好，世界
```
则歌词`你好，世界`将在1分2.3秒，即62.3秒被显示，这符合标准lrc解析规范。
这行歌词有五个字符，即`你`、`好`、`，`、`世`、`界`，空格也被算作一个字符，
每个字符默认的激活时间为0，即在歌词行显示时立即被激活。

定义**元数据**为以下格式：
```
time[,count]|time[,count]|time[,count]|...|time[,count]
```
`time`表示激活时间，`count`表示激活组的字符数，当`count`为1时，可以省略。这里的`count`单位是可打印字符，并非一个字节。

一个分字解析元数据由多个激活组定义构成，并由符号`|`分割。  
如：如果要将歌词`你好，世界`拆分成三个激活组，`你好，`，`世`，`界`，可按以下格式书写
```
[sm:++]
[:]++2,3|4.3,1|5,1
[1:02.30]你好，世界
```
这表示：
有三个激活组，第一个激活组在歌词显示后`2`秒激活，这个激活组有`3`个字符，第二个激活组在歌词显示后`4.3`秒激活，有`1`个字符，第三个激活组在歌词显示后`5`秒激活，有`1`个字符。

当count为1时可以省略，因此可以记作：
```
[sm:++]
[:]++2,3|4.3|5
[1:02.30]你好，世界
```
这样，该行歌词将在62.3秒时显示，`你好，`将在64.3秒激活，`世`将在66.6秒激活，`界`将在67.3秒激活。

## 例子 ##
```
[encoding:utf-8]
[ti:this is a title]
[ttm:=]
[ttsm:-]
[sm:++]
[st:@]
[:]@style1
[:]-2,6|99|4.3,3|5,2
[:]=hello, world
[:]++2,3|4.3|5
[1:02.30]你好，世界
[:]@style2
[:]=other lrc
[:]++1|3|4|2
[1:10.68]其他歌词
[1:15.68]其他歌词2
```
- `hello,`之后有一个空格。
- `hello, world`将作为`你好，世界`的翻译文本显示，且在64.3 66.6 67.3秒时依次激活：“hello," "wor" "ld"。  
- `|99|`说的是`hello,`后面的空格，表示空格在99秒后才会被激活。但此时歌词已经换行了，故永远不会被激活。
- `你好，世界`将套用样式style1，`其他歌词`将套用样式style2，`其他歌词2`也将套用样式style2

当不支持分字LRC的解析器解析时，忽略注释标签和未知标签时，上述歌词文件将解析为
```
[ti:this is a title]
[1:02.30]你好，世界
[1:10.68]其他歌词
[1:15.68]其他歌词2
```

---------

## 编程记录 ##

这是我的`C#`实现过程的一些记录

|类名|功能|
|------|------|
|WordLyric|分字LRC，表示一个歌词|
|LyricLine|一行|
|WordGroupCollection|表示一组(一行)歌词，是若干歌词组的集合|
|WordGroup|表示某一行中的某一个激活组和它的激活时间|
|MetaData|私有，辅助从LRC格式读到内存|
|LyricBuildTags|私有，辅助编码为可读的LRC格式|

- 引用的坑：当通过程序手动构建一个歌词时，不要把一个LyricLine对象多次加入WordLyric，也不要把同一个WordGroup对象多次加入LyricLine，而是要new出新的对象。在内存中，导入文件之后，歌词总是以树的形式存储，直接持有传入对象的引用，而非拷贝。如果有两行歌词指向同一个对象，那修改一行歌词也会导致另一行歌词被修改。当然如果已经这样做了，也不会影响到导出函数，将歌词重新导出解析一次就可以解开环了。
- 支持忽略未知标签
- 程序读入时支持`\r\n`、`\r`和`\n`三种换行，导出时与具体环境有关
- 注释优先于其它标签被处理，因此除解析符号开头之外的注释中出现的标签没有意义
- 支持注释外任意位置出现标识标签、时间标签
- 一行可以出现多个标识标签
- 出现标识标签后，该行的时间标签将失效
- 导出时相同的歌词不会被合并到同一行，所以导出格式每行最多只会有一个时间标签
- 导出为`byte[]`会添加encoding标签，从`byte[]`读入会使用encoding标签，如果不存在encoding标签，则默认使用utf-8编码。如果不想添加encoding标签，可以用`ToLyric`而非`ToBytes`
- 通过`WordLyrics.ToLyric`传入`true`可以转换为普通的lrc格式，由于普通LRC不含编码标签，`ToBytes`不做实现
- 可以直接设置`LyricLine`的`TranslateText`或`Text`，从而处理普通的Lrc歌词，避开激活组的处理
- 读入时支持在任意位置出现标识标签，但导出时标识标签是放在所有时间标签之前的
- 读入过程，解析符可以被重新定义，从而覆盖旧的解析符定义，旧的解析附在被重新定义之前都有效，但导出时，解析符使用的是`WordLyric.StyleMark`等定义的，一般是读入时最后一个定义的解析符
- 不同功能的解析符不可以相同，一个解析符也不能成为另一个的前缀，否则将解析错误
- 解析错误的行将被忽略

## LyricAdapterBase ##

提供简单的歌词播放实现的基类，可以用于显示普通的滚动歌词和翻译文本。提供从`SeekTo(float sec)`到特定事件的适配器。



### SimpleLyricAdapter ###


提供从`SeekTo(float sec)`到歌词`行`相关事件的适配器模式(即效果与普通的LRC等同)。项目`SimpleLyricConsole`提供其简单实现。

简言之，是这样的：

```
       LoadLyric(WordLyric) SeekTo(sec)
               ↓              ↓
               SimpleLyricAdapter
                      ↓
OnAddLyricLine() OnActiveLine() OnUnActiveLine()

```
需要先调用`SimpleLyricAdapter`的`LoadLyric`，提供一个WordLyric歌词，再调用`SeekTo`，提供当前播放进度，
此时就会通过后面的回调来更新UI或者做其他事情了。


使用歌词解析，需要先引用namespace。
```
using WordLyric;
```
在这之后，先准备好一个歌词，歌词可以通过`WordLyric`类解析。
```
WordLyric.WordLyric wordLyric = WordLyric.WordLyric.FromLrc(LyricPreset);
```
之后建立适配器`SimpleListAdapter`
```
SimpleLyricAdapter simpleLyricAdapter = new SimpleLyricAdapter();
```
适配器`SimpleListAdapter`实现抽象类`LyricAdapterBase`，提供`LoadLyric(WordLyric)`和`SeekTo(float time)`函数，
调用这两个函数将立即触发回调，因此需要在此之前先设置回调，提供的回调有三个，使用示例如下
```
simpleLyricAdapter.OnAddLyricLine += SimpleLyricAdapter_OnAddLyricLine;
simpleLyricAdapter.OnUnActiveLine += SimpleLyricAdapter_OnUnActiveLine;
simpleLyricAdapter.OnActiveLine += SimpleLyricAdapter_OnActiveLine;
```
其中`OnAddLyricLine`会在`LoadLyric(WordLyric)`被调用时触发，请在这里初始化控件或记录相关信息，传入的`bundles`是已经排好序的歌词每行的信息。另外不要持有或修改传入的列表本身。可以持有列表中的元素，但修改这些元素的内容是没有意义的，之后这些元素会再次被传入`OnActiveLine`和`OnUnActiveLine`。
```
List<string> LyricList = new List<string>();
List<bool> LyricIsActive = new List<bool>();

void SimpleLyricAdapter_OnAddLyricLine(IList<SimpleLyricAdapter.LyricLineBundle> bundles)
{
    LyricList.Clear();
    LyricIsActive.Clear();

    foreach(var bundle in bundles)
    {
        LyricList.Add(bundle.LineText);
        LyricIsActive.Add(false);
        //assert(LyricList.size == linecode + 1)
    }
}
```
`OnActiveLine`将在`SeekTo`被调用时、指针指向某行时触发(如果可能)，参数是当前时间指针应该指向的行。
并不是每次调用`SeekTo`都会触发这个回调，`SimpleLyricAdapter`记录历史`SeekTo`的信息，同一个行被激活后不会被再次激活。
一般歌词跳转的事件在这里处理。传入的`LyricLineBundle`可以被持有，与之前`OnAddLyricLine`传入的列表中元素是同一个。
```
void SimpleLyricAdapter_OnActiveLine(SimpleLyricAdapter.LyricLineBundle bundle)
{
    LyricIsActive[bundle.LineNumber] = true;
}
```
`OnUnActiveLine`将在`SeekTo`被调用时、指针离开某行时触发(如果可能)，参数是指针离开的那一行。
并不是所有的实现都需要重写这个函数
```
void SimpleLyricAdapter_OnUnActiveLine(SimpleLyricAdapter.LyricLineBundle bundle)
{
    LyricIsActive[bundle.LineNumber] = false;
}
```
加载歌词时，使用`LoadLyric`函数。此时就会触发`OnAddLyricLine`回调，故在此之前需要先准备好其他的控件布局。
可以把这个函数视作歌词行UI初始化的函数。同一个`LyricAdapterBase`可以多次调用LoadLyric，新的歌词会替换旧的歌词，
但应在`OnAddLyricLine`中对旧的UI进行处理。歌词被加载后，`SimpleLyricAdapter`的行光标默认指向`-1`行，即不存在。
```
simpleLyricAdapter.LoadLyric(wordLyric);
```
歌词播放时，通过不断调用`SeekTo(float)`来使得歌词进度跟随播放器，假定`CurrentTime`存储当前歌词播放的秒数，
则需要在一定周期内使用定时器或其他方式执行以下语句
```
simpleLyricAdapter.SeekTo((float)CurrentTime);
```
- 多线程处理：`SeekTo`将触发`OnActiveLine`和`OnUnActiveLine`，因此如果这两个函数有UI更新，则`SeekTo`有必要放在UI线程中触发。  
- 避免频繁检查歌词状态：
SeekTo并不是每次调用都会触发`OnActiveLine`和`OnUnActiveLine`，这个函数的返回值表示如果按照正常播放进度，下次调用SeekTo
的时间点，只需要在播放进度过了这个时间后再调用SeekTo就可以了。相对应的，当播放暂停时，只要停止调用SeekTo即可，即便不停止调用
SeekTo，只要传入的时间保持不变的话，也不会触发歌词更新。而当播放进度条改变后，应当调用一次SeekTo来触发歌词更新。

### WordLyricAdapter ###

提供从`SeekTo(float sec)`到歌词`行`和`激活组`相关事件的适配器模式。即对分字LRC格式解析的适配器模式完全实现。

播放器还是使用的SimpleLyricAdapter的逻辑。

文档还没写

# License #

MIT License

[百度百科_软件开发标准]: https://baike.baidu.com/item/lrc/46935?fr=aladdin#5