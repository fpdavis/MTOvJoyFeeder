using System.Runtime.InteropServices;

namespace vXboxInterfaceWrap
{
    public struct PXINPUT_VIBRATION
    {
        ushort wLeftMotorSpeed;
        ushort wRightMotorSpeed;
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

    public struct Range
    {
        public const int MinXVal = -32768;
        public const int MaxXVal = 32768;
        public const int MinYVal = -32768;
        public const int MaxYVal = 32768;
        public const int MinZVal = -255;
        public const int MaxZVal = 255;

        public const int MinRXVal = -32768;
        public const int MaxRXVal = 32768;
        public const int MinRYVal = -32768;
        public const int MaxRYVal = 32768;        
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
        public static extern bool SetDpadOff(uint UserIndex);

        public static bool SetDpad(uint UserIndex, int Value)
        {
            if (Value == DPadState.North)
            {
                return SetDpadUp(UserIndex);
            }
            else if (Value == DPadState.NorthEast)
            {
                return SetDpadUp(UserIndex) && SetDpadRight(UserIndex);
            }
            else if (Value == DPadState.South)
            {
                return SetDpadDown(UserIndex);
            }
            else if (Value == DPadState.SouthEast)
            {
                return SetDpadDown(UserIndex) && SetDpadRight(UserIndex);
            }
            else if (Value == DPadState.West)
            {
                return SetDpadLeft(UserIndex);
            }
            else if (Value == DPadState.SouthWest)
            {
                return SetDpadDown(UserIndex) && SetDpadLeft(UserIndex);
            }
            else if (Value == DPadState.East)
            {
                return SetDpadRight(UserIndex);
            }
            else if (Value == DPadState.NorthWest)
            {
                return SetDpadUp(UserIndex) && SetDpadLeft(UserIndex);
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
        public static extern bool GetVibration(uint UserIndex, PXINPUT_VIBRATION pVib);
        #endregion
    }
}
