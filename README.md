# Unity_FontAtlasGenerator
Font Atlas Generator is an editor-only Unity tool that you can use to generate tilesheets from any .ttf font with any characters the font supports.

NOTE: Please be aware of the license for any fonts you use with this tool. Most fonts have very restrictive licenses, just because you generate a tilesheet from it doesn't change the license requirements.

The generator takes a string of characters and generates the tilesheet in a grid format. There is a preset for Code Page 437:

![](http://i.imgur.com/fJZ80d4.png)

You will almost always have to tweak the values a bit to get the glyphs to line up and to get it looking sharp:

![](https://thumbs.gfycat.com/SparklingBowedGannet-size_restricted.gif)

Most truetype fonts you'll find only support a very limited range of characters. If you change the dynamic font setting you can use a fallback font to replace any missing glyphs:

![](https://thumbs.gfycat.com/RespectfulFlawedIridescentshark-size_restricted.gif)


You can also adjust the text and background colors for your output texture as needed - including transparent backgrounds:

![](https://thumbs.gfycat.com/FakeDigitalCardinal-size_restricted.gif)
