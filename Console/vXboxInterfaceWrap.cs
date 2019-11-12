using System.Runtime.InteropServices;

namespace vXboxInterfaceWrap
{
    public struct PXINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed;
        public ushort wRightMotorSpeed;
    }

    public struct DPadState
    {
        public const int Off = -1;

        public const int North = 0;
        public const int NorthEast = 4500;
        public const int East = 9000;
        public const int SouthEast = 13500;
        public const int South = 18000;
        public const int SouthWest = 22500;
        public const int West = 27000;
        public const int NorthWest = 31500;

    };

    // https://docs.microsoft.com/en-us/windows/desktop/api/xinput/ns-xinput-_xinput_gamepad
    //
    //
    //  Bits that are set but not defined below are reserved, and their state is undefined.
    //
    //  bLeftTrigger
    //
    //      The current value of the left trigger analog control.The value is between 0 and 255.
    //
    //  bRightTrigger
    //
    //      The current value of the right trigger analog control.The value is between 0 and 255.
    //
    //  sThumbLX
    //
    //      Left thumbstick x-axis value. Each of the thumbstick axis members is a signed value 
    //      between -32768 and 32767 describing the position of the thumbstick. A value of 0 is 
    //      centered. Negative values signify down or to the left. Positive values signify up or
    //      to the right. The constants XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE or 
    //      XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE can be used as a positive and negative value
    //      to filter a thumbstick input.
    //
    //  sThumbLY
    //
    //      Left thumbstick y-axis value. The value is between -32768 and 32767.
    //
    //  sThumbRX
    //
    //      Right thumbstick x-axis value. The value is between -32768 and 32767.
    //
    //  sThumbRY
    //
    //      Right thumbstick y-axis value. The value is between -32768 and 32767.
    public struct XINPUT_GAMEPAD
    {
        public const int DPAD_UP = 0x0001;
        public const int DPAD_DOWN = 0x0002;
        public const int DPAD_LEFT = 0x0004;
        public const int DPAD_RIGHT = 0x0008;
        public const int START = 0x0010;
        public const int BACK = 0x0020;
        public const int LEFT_THUMB = 0x0040;
        public const int RIGHT_THUMB = 0x0080;
        public const int LEFT_SHOULDER = 0x0100;
        public const int RIGHT_SHOULDER = 0x0200;
        public const int A = 0x1000;
        public const int B = 0x2000;
        public const int X = 0x4000;
        public const int Y = 0x8000;
    }

    public struct Range
    {
        public const int MinXVal = -32768;
        public const int MaxXVal = 32767;
        public const int MinYVal = -32768;
        public const int MaxYVal = 32767;
        public const int MinZVal = -255;
        public const int MaxZVal = 255;

        public const int MinRXVal = -32768;
        public const int MaxRXVal = 32767;
        public const int MinRYVal = -32768;
        public const int MaxRYVal = 32767;        
    }

    class vXboxInterface
    {
        #region Status Functions
        [DllImport("vXboxInterface.dll")]
        public static extern bool isVBusExists();
        [DllImport("vXboxInterface.dll")]
        public static extern bool GetNumEmptyBusSlots(ref byte SlotsByRef);
        [DllImport("vXboxInterface.dll")]
        public static extern bool isControllerExists(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool isControllerOwned(uint UserIndex);                
        #endregion

        #region Plugged-in/Unplug Functions
        [DllImport("vXboxInterface.dll")]
        public static extern bool PlugIn(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool UnPlug(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool UnPlugForce(uint UserIndex);
        #endregion

        #region Data Feeding Functions - Buttons
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnA(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnB(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnX(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnY(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnStart(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnBack(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnLT(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnRT(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnLB(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetBtnRB(uint UserIndex, bool Press);

        public static bool SetBtn(bool Press, uint UserIndex, uint nBtn)
        {
            switch (nBtn)
            {
                case 0:
                    return SetBtnA(UserIndex, Press);
                case 1:
                    return SetBtnB(UserIndex, Press);
                case 2:
                    return SetBtnX(UserIndex, Press);
                case 3:
                    return SetBtnY(UserIndex, Press);
                case 4:
                    return SetBtnLT(UserIndex, Press);
                case 5:
                    return SetBtnRT(UserIndex, Press);
                case 6:
                    return SetBtnRT(UserIndex, Press);
                case 7:
                    return SetBtnStart(UserIndex, Press);
                case 8:                    
                    return SetBtnLB(UserIndex, Press);
                case 9:
                    return SetBtnRB(UserIndex, Press);
                case 10:
                    return SetBtnBack(UserIndex, Press);
                default:
                    return false;
            }
        }

        #endregion

        #region Data Feeding Functions - SetZAxis
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetTriggerL(uint UserIndex, byte Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetTriggerR(uint UserIndex, byte Value);

        public static bool SetZAxis(uint UserIndex, short Value)
        {
            if (Value < -2)
            {
                SetTriggerL(UserIndex, (byte)System.Math.Abs(Value));
            }
            else if (Value > 2)
            {
                SetTriggerR(UserIndex, (byte)Value);
            }
            else
            {
                SetTriggerL(UserIndex, 0);
                SetTriggerR(UserIndex, 0);
            }

            return true;
        }
        #endregion

        #region Data Feeding Functions - SetAxis
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetAxisX(uint UserIndex, short Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetAxisY(uint UserIndex, short Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetAxisRx(uint UserIndex, short Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetAxisRy(uint UserIndex, short Value);

        public static bool SetAxis(uint UserIndex, short Value, HID_USAGES Map_To)
        {
            switch (Map_To)
            {
                case HID_USAGES.HID_USAGE_X:
                    return SetAxisX(UserIndex, Value);
                case HID_USAGES.HID_USAGE_Y:
                    return SetAxisY(UserIndex, Value);
                case HID_USAGES.HID_USAGE_RX:
                    return SetAxisRx(UserIndex, Value);
                case HID_USAGES.HID_USAGE_RY:
                    return SetAxisRy(UserIndex, Value);
                default:
                    return false;
            }
        }
        #endregion

        #region Data Feeding Functions - SetDpad
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetDpadUp(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetDpadDown(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetDpadLeft(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetDpadRight(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        private static extern bool SetDpad(uint UserIndex, int Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetDpadOff(uint UserIndex);

        public static bool SetDpadByValue(uint UserIndex, int Value)
        {
            if (Value == DPadState.North)
            {
                return SetDpadUp(UserIndex);
            }
            else if (Value == DPadState.NorthEast)
            {
                return SetDpad(UserIndex, XINPUT_GAMEPAD.DPAD_UP | XINPUT_GAMEPAD.DPAD_RIGHT);
            }
            else if (Value == DPadState.South)
            {
                return SetDpadDown(UserIndex);
            }
            else if (Value == DPadState.SouthEast)
            {
                return SetDpad(UserIndex, XINPUT_GAMEPAD.DPAD_DOWN | XINPUT_GAMEPAD.DPAD_RIGHT);
            }
            else if (Value == DPadState.West)
            {
                return SetDpadLeft(UserIndex);
            }
            else if (Value == DPadState.SouthWest)
            {
                return SetDpad(UserIndex, XINPUT_GAMEPAD.DPAD_DOWN | XINPUT_GAMEPAD.DPAD_LEFT);
            }
            else if (Value == DPadState.East)
            {
                return SetDpadRight(UserIndex);
            }
            else if (Value == DPadState.NorthWest)
            {
                return SetDpad(UserIndex, XINPUT_GAMEPAD.DPAD_UP | XINPUT_GAMEPAD.DPAD_LEFT);
            }
            else if (Value == DPadState.Off)
                return SetDpadOff(UserIndex);
            else
            {
                return false;
            }
        }
        #endregion

        #region Feedback Functions                        
        [DllImport("vXboxInterface.dll")]
        public static extern bool GetLedNumber(uint UserIndex, ref byte pLed);
        [DllImport("vXboxInterface.dll")]
        public static extern bool GetVibration(uint UserIndex, ref PXINPUT_VIBRATION pVib);
        #endregion
    }
}
