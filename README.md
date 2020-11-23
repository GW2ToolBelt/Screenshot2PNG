# Screenshot2PNG

A simple (silly) command-line tool to automatically convert Guild Wars 2
screenshots from Bitmaps (`.bmp`) to `png` in real-time. The most recent
screenshot PNGs is can then also be made available in the clipboard.


## How it works?

This program does not hook into the game in any way, instead it uses a
[FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-5.0)
to listen for new screenshots. Whenever a new screenshot is detected, it is
converted, and (optionally) copied to the clipboard.

Additionally, the screenshots are renamed to a saner naming scheme
(`yyyy-MM-dd_HH-mm-ss.fff.png` instead of `gw001.png`).

The tool exits once there it cannot detect any running GW2 process.


### Why should I use this?

Honestly, I'm not quite sure yet if you should. I wrote this mostly to learn a
bit more about C# and this program is not exactly sophisticated. However, it
does what it's supposed to and it shouldn't break anything. :)


## Usage

```
  -o, --output-dir      Specifies the directory in which the converted screenshots should be saved.

  --convert-existing    (Default: true) Enables or disables conversion of existing screenshots.

  --use-clipboard       (Default: true) Enables or disables clipboard integration.

  --help                Display this help screen.

  --version             Display version information.

  input-dir (pos. 0)    Required. Specifies the directory from which the screenshot bitmaps are read.
```


## License

```
Copyright (c) 2020 Leon Linhart

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```