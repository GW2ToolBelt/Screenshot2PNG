# 1.0.0

_Released 2020 Dec 03_

### Overview

A simple (silly) command-line tool to automatically convert Guild Wars 2
screenshots from Bitmaps (`.bmp`) to `png` in real-time. The most recent
screenshot PNGs is can then also be made available in the clipboard.

#### How it works?

This program does not hook into the game in any way, instead it uses a
[FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-5.0)
to listen for new screenshots. Whenever a new screenshot is detected, it is
converted, and (optionally) copied to the clipboard.

Additionally, the screenshots are renamed to a saner naming scheme
(`yyyy-MM-dd_HH-mm-ss.fff.png` instead of `gw001.png`).

The tool exits once there it cannot detect any running GW2 process.