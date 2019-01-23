using System.Runtime.InteropServices;

namespace vXboxInterfaceWrap
{
    class vXboxInterface
    {
        [DllImport("vXboxInterface.dll")]
        public static extern bool isVBusExists();
        [DllImport("vXboxInterface.dll")]
        public static extern bool GetNumEmptyBusSlots(ref byte SlotsByRef);
        [DllImport("vXboxInterface.dll")]
        public static extern bool isControllerExists(uint Slot);
        [DllImport("vXboxInterface.dll")]
        public static extern bool isControllerOwned(uint Slot);
        [DllImport("vXboxInterface.dll")]
        public static extern bool PlugIn(uint Slot);
        [DllImport("vXboxInterface.dll")]
        public static extern bool GetLedNumber(uint Slot, ref byte pLed);
    }
}
