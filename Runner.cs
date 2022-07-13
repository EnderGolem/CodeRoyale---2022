using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Threading;

namespace AiCup22
{


    public static class VirtualInnput
    {
        private enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        private const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        private const int VK_RCONTROL = 0x1B; //Right Control key code
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        public static void SetCursorClick(int x, int y)
        {

            Thread.Sleep(100);
            SetCursorPos(x, y);
            Thread.Sleep(100);
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            Thread.Sleep(100);
            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);
        }
        public static void PressEsk()
        {
            Thread.Sleep(500);
            keybd_event(VK_RCONTROL, 0, KEYEVENTF_EXTENDEDKEY, 0);
            //Thread.Sleep(500);
            //keybd_event(VK_RCONTROL, 0, KEYEVENTF_KEYUP, 0);
        }
    }

    public class Runner
    {
        private BinaryReader reader;
        private BinaryWriter writer;
        public Runner(string host, int port, string token)
        {
            while (true)
            {
                try
                {
                    var client = new TcpClient(host, port) { NoDelay = true };
                    var stream = new BufferedStream(client.GetStream());
                    reader = new BinaryReader(stream);
                    writer = new BinaryWriter(stream);
                    var tokenData = System.Text.Encoding.UTF8.GetBytes(token);
                    writer.Write(tokenData.Length);
                    writer.Write(tokenData);
                    writer.Write((int)1);
                    writer.Write((int)1);
                    writer.Write((int)1);
                    writer.Flush();

                    VirtualInnput.SetCursorClick(1070, 595);

                    break;
                }
                catch (Exception)
                {

                }
            }
        }
        public void Run()
        {
            MyStrategy myStrategy = null;
            var debugInterface = new DebugInterface(reader, writer);
            var running = true;
            int maxWithoutOrder = 100;
            int withoutOrder = 0;
            while (running)
            {
                withoutOrder++;
                switch (AiCup22.Codegame.ServerMessage.ReadFrom(reader))
                {
                    case AiCup22.Codegame.ServerMessage.UpdateConstants message:
                        myStrategy = new MyStrategy(message.Constants);
                        break;
                    case AiCup22.Codegame.ServerMessage.GetOrder message:
                        withoutOrder = 0;
                        new AiCup22.Codegame.ClientMessage.OrderMessage(myStrategy.GetOrder(message.PlayerView, message.DebugAvailable ? debugInterface : null)).WriteTo(writer);
                        writer.Flush();
                        break;
                    case AiCup22.Codegame.ServerMessage.Finish message:
                        running = false;
                        myStrategy.Finish();
                        break;
                    case AiCup22.Codegame.ServerMessage.DebugUpdate message:
                        myStrategy.DebugUpdate(message.DisplayedTick, debugInterface);
                        new AiCup22.Codegame.ClientMessage.DebugUpdateDone().WriteTo(writer);
                        writer.Flush();
                        break;
                    default:
                        throw new Exception("Unexpected server message");
                }
                if (withoutOrder > maxWithoutOrder)
                {
                    System.Console.WriteLine("My_strategy Finish");
                    VirtualInnput.PressEsk();
                    System.Console.WriteLine("Esc_click");
                }

            }
            myStrategy.addText();
            System.Console.WriteLine("Add_file");
        }
        public static void Main(string[] args)
        {
            for (int i = 1; i <= 11; i++)
            {
                System.Console.WriteLine($"Round {i}");
                Thread.Sleep(2000);
                System.Console.WriteLine("Create");
                string host = args.Length < 1 ? "127.0.0.1" : args[0];
                int port = args.Length < 2 ? 31001 : int.Parse(args[1]);
                string token = args.Length < 3 ? "0000000000000000" : args[2];
                new Runner(host, port, token).Run();
            }
        }
    }
}