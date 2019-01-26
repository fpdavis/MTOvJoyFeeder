using System.Runtime.InteropServices;

namespace vXboxInterfaceWrap
{
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
		  
        #region Data Feeding Functions
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnA(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnB(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnX(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnY(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnStart(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnBack(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnLT(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnRT(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnLB(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetBtnRB(uint UserIndex, bool Press);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetTriggerL(uint UserIndex, byte Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetTriggerR(uint UserIndex, byte Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetAxisX(uint UserIndex, short Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetAxisY(uint UserIndex, short Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetAxisRx(uint UserIndex, short Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetAxisRy(uint UserIndex, short Value);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetDpadUp(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetDpadDown(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetDpadRight(uint UserIndex);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	SetDpadOff(uint UserIndex);
        #endregion
		 
        #region Feedback Functions                        
        [DllImport("vXboxInterface.dll")]
        public static extern bool GetLedNumber(uint UserIndex, ref byte pLed);
        [DllImport("vXboxInterface.dll")]
        public static extern bool 	GetVibration(uint UserIndex,  PXINPUT_VIBRATION pVib);            
        #endregion
    }
}
