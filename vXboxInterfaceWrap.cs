using System.Runtime.InteropServices;

namespace vXboxInterfaceWrap
{
    public struct PXINPUT_VIBRATION
    {
        ushort wLeftMotorSpeed;
        ushort wRightMotorSpeed;
    }

    public enum DPadState
    {
              Off = -1,

            North = 0,
        NorthEast = 4500,
             East = 9000,
        SouthEast = 13500,
            South = 18000,
        SouthWest = 22500,
             West = 27000,
        NorthWest = 31500
    };

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
                    return SetBtnY(UserIndex, Press);
                case 5:
                    return SetBtnLT(UserIndex, Press);
                case 6:
                    return SetBtnRT(UserIndex, Press);
                case 7:
                    return SetBtnStart(UserIndex, Press);
                case 8:
                    return SetBtnBack(UserIndex, Press);
                case 9:
                    return SetBtnLB(UserIndex, Press);
                case 10:
                    return SetBtnRB(UserIndex, Press);
                default:
                    return false;
            }
        }

        #endregion

        [DllImport("vXboxInterface.dll")]
        public static extern bool SetTriggerL(uint UserIndex, byte Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool SetTriggerR(uint UserIndex, byte Value);
        
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
            if (Value == (int)DPadState.North)
            {
                return SetDpadUp(UserIndex);
            }
            else if (Value == (int)DPadState.NorthEast)
            {
                return SetDpadUp(UserIndex) && SetDpadRight(UserIndex);
            }
            else if (Value == (int)DPadState.South)
            {
                return SetDpadDown(UserIndex);
            }
            else if (Value == (int)DPadState.SouthEast)
            {
                return SetDpadDown(UserIndex) && SetDpadRight(UserIndex);
            }
            else if (Value == (int)DPadState.West)
            {
                return SetDpadLeft(UserIndex);
            }
            else if (Value == (int)DPadState.SouthWest)
            {
                return SetDpadDown(UserIndex) && SetDpadLeft(UserIndex);
            }
            else if (Value == (int)DPadState.East)
            {
                return SetDpadRight(UserIndex);
            }
            else if (Value == (int)DPadState.NorthWest)
            {
                return SetDpadUp(UserIndex) && SetDpadLeft(UserIndex);
            }
            else if (Value == (int)DPadState.Off)
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
