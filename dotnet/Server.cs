using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Pixelflut.NeoPixel
{
    class Server
    {
        private readonly string _serialPort;
        private readonly IPAddress _listenIp;
        private readonly int _listenPort;
        private readonly int _pixelCount;
        private readonly Color[] _pixels;

        public Server(string serialPort, int pixelCount, IPAddress listenIp = null, int listenPort = 1234)
        {
            if (serialPort == null)
                throw new ArgumentNullException(nameof(serialPort));

            _serialPort = serialPort;

            _pixelCount = pixelCount;
            _pixels = new Color[pixelCount];

            _listenIp = listenIp ?? IPAddress.Any;
            _listenPort = listenPort;
        }

        public void Accept(CancellationToken ct = default(CancellationToken))
        {
            TcpListener listener = new TcpListener(_listenIp, _listenPort);
            Thread drawThread = new Thread(() => HandleDrawing(ct));
            drawThread.Start();

            listener.Start();
            while (!ct.IsCancellationRequested)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }
                TcpClient tcpClient = listener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(tcpClient));
                var ipAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                clientThread.Start();
                Console.WriteLine("Connection from {0}", ipAddress);
            }
            listener.Stop();
        }

        private void HandleDrawing(CancellationToken ct)
        {
            using (var sw = new StreamWriter(_serialPort))
            {
                sw.Write("!");
                sw.Flush();
                Thread.Sleep(5000);
                while (!ct.IsCancellationRequested)
                {
                    var anySet = false;
                    for (int i = 0; i < _pixelCount; i++)
                    {
                        var color = _pixels[i];
                        _pixels[i] = Color.FromArgb(0, 0, 0, 0);
                        if (color.A == 0)
                            continue;
                        var serialMsg = $"{i},{color.R},{color.G},{color.B};";
                        sw.Write(serialMsg);
                        Thread.Sleep(1); // TODO: Arduino code seems to break if pixels are sent too fast
                        // Console.WriteLine(serialMsg);
                        anySet = true;
                    }
                    if (anySet)
                    {
                        sw.Write('!');
                        sw.Flush();
                        continue;
                    }
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            StreamReader sr = new StreamReader(stream, Encoding.UTF8);

            try
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var response = HandleMessage(line);
                    if (response != null)
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(responseBytes, 0, responseBytes.Length);
                    }
                }
            }
            finally
            {
                sr.Close();
                client.Close();
            }
        }

        private string HandleMessage(string msg)
        {
            var parts = msg.Split(' ');
            switch (parts[0])
            {
                case "PX":// "PX 20 0 FF00FF"
                    if (parts.Length != 4)
                        break;
                    int x, y;
                    if (!int.TryParse(parts[1], out x) ||
                    !int.TryParse(parts[2], out y) ||
                    x >= _pixelCount ||
                    y != 0)
                        break;
                    Color c;
                    if (!TryParseColor(parts[3], out c))
                        break;
                    _pixels[x] = c;
                    break;
                case "SIZE":// "SIZE\n"
                    return $"SIZE {_pixelCount} 1\n";
                case "GET":
                    var httpmsg = "This is a Pixelflut server (supports TCP 'PX $x $y $hexColor\\n' and 'SIZE\\n')! Example usage: 'echo PX 10 0 FF00FF | nc 192.168.178.123 1234'";
                    return $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {Encoding.UTF8.GetByteCount(httpmsg)}\r\n\r\n{httpmsg}";
                default:
                    Console.WriteLine($"Unknown Command {parts[0]}");
                    break;
            }
            return null;
        }

        public static bool TryParseColor(string colorStr, out Color color)
        {
            uint colorInt;
            if (!uint.TryParse(colorStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out colorInt))
                return false;
            if (colorStr.Length == 6) // FF00FF
            {
                color = Color.FromArgb((int)(colorInt | 0xFF000000));
                return true;
            }

            if (colorStr.Length == 8) // FF00FFFF
            {
                var a = colorInt & 0xFF;
                color = Color.FromArgb((int)(colorInt >> 8 | a << 24));
                return true;
            }
            return false;
        }
    }
}