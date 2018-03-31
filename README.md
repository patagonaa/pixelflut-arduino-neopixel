# pixelflut-arduino-neopixel
Pixelfluts RGB LED Strings by receiving commands with a .NET Core client and sending them to an Arduino via Serial

## Arduino setup
Adjust Arduino code to your output pin and LED count and upload

## .NET Setup
With the current (2.x) .NET Core version installed, enter the "dotnet" directory, adjust pixel count and TCP Port to your needs in `Program.cs` and run
```
dotnet run
```
The server supports `PX X Y RRGGBB`, `PX X Y RRGGBBAA` (Pixel colors are not currently mixed if alpha not 0 or 255) and `SIZE`.

There is also a rudimentary HTTP Server (same port as Pixelflut) to show some usage info.
