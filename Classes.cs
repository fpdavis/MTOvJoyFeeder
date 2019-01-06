using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using CommandLine;
using CommandLine.Text;

namespace MTOvJoyFeeder
{
    public class Options
    {
        [Option('v', "Verbosity", Default = Program.giVerbosity_Error, HelpText =
              "The amount of verbosity to use. [-v 4]. "
            + "Debug (5): Super detail. "
            + "Verbose (4): Almost everything. "
            + "Information (3): Generally useful information. "
            + "Warning (2): Expected problems. "
            + "Error (1): Errors that are recoverable. "
            + "Critical (0): Errors that stop execution. ")]
        public int iVerbosity { get; set; }

        [Option('c', "config", Required = false, HelpText = "Override location of the config file. Full or relative path must be provided.")]        
        public string ConfigFile { get; set; }

        [Option('e', "MaxErrors", Required = false, HelpText = "Maximum number of errors before removing a Joystick from monitoring.")]
        public uint ErrorsBeforeJoystickRemoval { get; set; }
    }

    public class vJoystickInfo
    {
        public uint id = 1;

        public long lMinXVal = int.MaxValue;
        public long lMaxXVal = int.MaxValue;
        public long lMinYVal = int.MaxValue;
        public long lMaxYVal = int.MaxValue;
        public long lMinZVal = int.MaxValue;
        public long lMaxZVal = int.MaxValue;

        public long lMinRXVal = int.MaxValue;
        public long lMaxRXVal = int.MaxValue;
        public long lMinRYVal = int.MaxValue;
        public long lMaxRYVal = int.MaxValue;
        public long lMinRZVal = int.MaxValue;
        public long lMaxRZVal = int.MaxValue;
    }

    public class JoystickInfo
    {
        public DeviceInstance oDeviceInstance;
        public Joystick oJoystick;
        public IList<EffectInfo> oEffectInfo;
        public uint id = 1;
        public uint ErrorCount = 0;

        public long lMinXVal = int.MinValue;
        public long lMaxXVal = int.MaxValue;
        public long lMinYVal = int.MinValue;
        public long lMaxYVal = int.MaxValue;
        public long lMinZVal = int.MinValue;
        public long lMaxZVal = int.MaxValue;

        public long lMinRXVal = int.MinValue;
        public long lMaxRXVal = int.MaxValue;
        public long lMinRYVal = int.MinValue;
        public long lMaxRYVal = int.MaxValue;
        public long lMinRZVal = int.MinValue;
        public long lMaxRZVal = int.MaxValue;

        public long lPreviousX, lPreviousY, lPreviousZ;
        public long PreviousRX, PreviousRY, PreviousRZ;

        public double PercentX;
        public double PercentY;
        public double PercentZ;
        public double PercentRX;
        public double PercentRY;
        public double PercentRZ;

        public double PercentageSlack = .01;

        public int[] Map_To_Virtual_Ids = { 1 };

        public HID_USAGES Map_X = HID_USAGES.HID_USAGE_X;
        public HID_USAGES Map_Y = HID_USAGES.HID_USAGE_Y;
        public HID_USAGES Map_Z = HID_USAGES.HID_USAGE_Z;

        public HID_USAGES Map_RotationX = HID_USAGES.HID_USAGE_RX;
        public HID_USAGES Map_RotationY = HID_USAGES.HID_USAGE_RY;
        public HID_USAGES Map_RotationZ = HID_USAGES.HID_USAGE_RZ;

        public uint Map_PointOfViewControllers0 = 0;
        public uint Map_PointOfViewControllers1 = 1;
        public uint Map_PointOfViewControllers2 = 2;
        public uint Map_PointOfViewControllers3 = 3;

        public uint[] Map_Buttons = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }; // SharpDX.DirectInput.JoystickOffset.Buttons0;
    }

    public class JoystickConfig
    {
        public Boolean Enabled = true;
        public string Instance_Name;
        public string Instance_Type;
        public Guid Instance_GUID;
        public Guid Product_GUID;
        public int[] Map_To_Virtual_Ids = { 1 };
        
        public double Percentage_Slack = .01;

        public string Map_X = "X"; // SharpDX.DirectInput.JoystickOffset.X;
        public string Map_Y = "Y"; // SharpDX.DirectInput.JoystickOffset.Y;
        public string Map_Z = "Z"; // SharpDX.DirectInput.JoystickOffset.Z;

        public string Map_RotationX = "RotationX"; // SharpDX.DirectInput.JoystickOffset.RotationX;
        public string Map_RotationY = "RotationY"; // SharpDX.DirectInput.JoystickOffset.RotationY;
        public string Map_RotationZ = "RotationZ"; // SharpDX.DirectInput.JoystickOffset.RotationZ;

        public uint Map_PointOfViewControllers0 = 0; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers0;
        public uint Map_PointOfViewControllers1 = 1; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers1;
        public uint Map_PointOfViewControllers2 = 2; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers2;
        public uint Map_PointOfViewControllers3 = 3; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers3;

        public uint[] Map_Buttons = { 0,1,2,3,4,5,6,7,8,9,10,11,12 }; // SharpDX.DirectInput.JoystickOffset.Buttons0;
    }
}
