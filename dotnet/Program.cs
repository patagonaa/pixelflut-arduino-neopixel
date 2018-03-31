using System;

namespace Pixelflut.NeoPixel
{
    class Program
    {
        static void Main(string[] args)
        {
            //You might have to do `stty -F /dev/ttyUSB0 115200` before running to set the baud rate
            var server = new Server("/dev/ttyUSB0", 50);
            server.Accept();
        }
    }
}
