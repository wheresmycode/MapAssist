using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SendInputs
{

    public class InputSender
    {
        //private Random rnd;
        private static Random _rnd = new Random();

        #region Imports/Structs/Enums
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            //public POINT pos;
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public HardwareInput hi;
        }

        public struct Input
        {
            public int type;
            public InputUnion u;
        }

        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }

        [Flags]
        public enum MouseEventF
        {
            Absolute = 0x8000,
            HWheel = 0x01000,
            Move = 0x0001,
            MoveNoCoalesce = 0x2000,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            VirtualDesk = 0x4000,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100
        }

        [DllImport("user32.dll")] static extern short VkKeyScan(char ch);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int x, int y);
        #endregion

        #region Wrapper Methods
        public static POINT GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return point;
        }

        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void SendKeyboardInput(KeyboardInput[] kbInputs)
        {
           var inputs = new Input[kbInputs.Length];

            for (var i = 0; i < kbInputs.Length; i++)
            {
                inputs[i] = new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = kbInputs[i]
                    }
                };
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        //public static void ClickKey(ushort scanCode,int time)
        public static void ClickKey(string key, int time)
        {
            ushort vKey = 0;
            //var keyboardInput = new KeyboardInput();

            if (key.Length == 1)
            {
                var str2char = char.Parse(key.Substring(0, 1));
                vKey = (ushort)(Keys)str2char;
            }
            else if (key.Length > 2 & key.Contains("VK_"))
            {
                //VK_Key handling
            }
            else if (key.Length > 1 & !key.Contains("VK_"))
            {
                //wrong input
            };

            if(vKey != 0)
            {
                var input1 = new KeyboardInput[]
                {
                new KeyboardInput
                {
                    wVk = vKey,
                    //wScan = scanCode,
                    dwFlags = (uint)(KeyEventF.ExtendedKey | KeyEventF.KeyDown),
                    dwExtraInfo = GetMessageExtraInfo()
                },
                };
                SendKeyboardInput(input1);
                Thread.Sleep(_rnd.Next(25 + time, 150 + time));
                var input2 = new KeyboardInput[]
                {
                new KeyboardInput
                {
                    wVk = vKey,
                    //wScan = scanCode,
                    dwFlags = (uint)(KeyEventF.ExtendedKey| KeyEventF.KeyUp),
                    //dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                    dwExtraInfo = GetMessageExtraInfo()
                }
                };
                SendKeyboardInput(input2);

            }

        }

        public static Keys ConvertCharToVirtualKey(char ch)
        {
            var vkey = VkKeyScan(ch);
            var retval = (Keys)(vkey & 0xff);
            var modifiers = vkey >> 8;
            Console.WriteLine($"vkey: {vkey}");
            Console.WriteLine($"retval: {retval}");
            Console.WriteLine($"modifiers: {modifiers}");

            if ((modifiers & 1) != 0) retval |= Keys.Shift;
            if ((modifiers & 2) != 0) retval |= Keys.Control;
            if ((modifiers & 4) != 0) retval |= Keys.Alt;
            Console.WriteLine($"retval: {retval}");
            return retval;
        }

        public static void LeftClick(int X, int Y,int time)
        {
            var rnd = new Random(); 
            InputSender.SetCursorPosition((X), (Y));
            InputSender.SendMouseInput(new InputSender.MouseInput[]
            {
                new InputSender.MouseInput
                         {
                         dx = (int)X,
                         dy = (int)Y,
                         time = (uint)rnd.Next(25+time,190+time),
                         dwFlags = (uint)(InputSender.MouseEventF.LeftDown)
                         },
            });
            Thread.Sleep(_rnd.Next(25 + time, 150 + time));
            InputSender.SendMouseInput(new InputSender.MouseInput[]
            {
                new InputSender.MouseInput
                         {
                         dx = (int)X,
                         dy = (int)Y,
                         time = (uint)rnd.Next(25+time,190+time),
                         dwFlags = (uint)(InputSender.MouseEventF.LeftUp)
                         }
            });
        }

        public static void RightClick(int X, int Y, int time)
        {
            
            InputSender.SetCursorPosition((X), (Y));
            InputSender.SendMouseInput(new InputSender.MouseInput[]
            {
                         new InputSender.MouseInput
                         {
                         dx = (int)X,
                         dy = (int)Y,
                         dwFlags = (uint)(InputSender.MouseEventF.RightDown)
                         },
            });
            Thread.Sleep(_rnd.Next(25 + time, 150 + time));
            InputSender.SendMouseInput(new InputSender.MouseInput[]
            {
                         new InputSender.MouseInput
                         {
                         dx = (int)X,
                         dy = (int)Y,
                         dwFlags = (uint)(InputSender.MouseEventF.RightUp)
                         }
            });
        }


        public static void SendMouseInput(MouseInput[] mInputs)
        {
            var inputs = new Input[mInputs.Length];

            for (var i = 0; i < mInputs.Length; i++)
            {
                inputs[i] = new Input
                {
                    type = (int)InputType.Mouse,
                    u = new InputUnion
                    {
                        mi = mInputs[i]
                    }
                };
            }
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }
        #endregion
    }
}
