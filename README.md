# Unity_FontAtlasGenerator
Font Atlas Generator is an editor-only Unity tool that you can use to generate tilesheets from any .ttf font with any characters the font supports.

Fonts must be set to dynamic and currently unity will create fallback glyphs for any character that are unsupported by the font. 

The generator takes a string of characters and generates the tilesheet in a grid format. There is a preset for Code Page 437:
![](http://i.imgur.com/INZUz4h.png)

You will almost always have to tweak the values a bit to get the glyphs to line up and to get it looking sharp:
![](https://thumbs.gfycat.com/VioletImpassionedIraniangroundjay-size_restricted.gif)
