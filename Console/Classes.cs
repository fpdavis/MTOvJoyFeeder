using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace MTOvJoyFeeder
{
    public static class Verbosity
    {
        public const int Debug = 5;       // Debug (5): Super detail
        public const int Verbose = 4;     // Verbose (4): Almost everything
        public const int Information = 3; // Information (3): Information that might be useful to user
        public const int Warning = 2;     // Warning (2): Bad things that happen, but are expected
        public const int Error = 1;       // Error (1): Errors that are recoverable
        public const int Critical = 0;    // Critical (0): Errors that stop execution

        public static EventLogEntryType[] GetEventLogEntryType = {
            EventLogEntryType.Error,       // Critical (0) - EventLogEntryType.Error = 1
            EventLogEntryType.Error,       // Error (1)
            EventLogEntryType.Warning,     // Warning (2) - EventLogEntryType.Warning = 2
            EventLogEntryType.Information, // Information (3) - EventLogEntryType.Information = 4
            EventLogEntryType.Information, // Verbose (4)
            EventLogEntryType.Information, // Debug (5)
        };

    }

        public class Options
    {
        [Option('v', "Verbosity", HelpText =
              "The amount of verbosity to use. [-v 3]. "
            + "Debug (5): Super detail. "
            + "Verbose (4): Almost everything. "
            + "Information (3): Generally useful information. "
            + "Warning (2): Expected problems. "
            + "Error (1): Errors that are recoverable. "
            + "Critical (0): Errors that stop execution. ")]
        private Nullable<uint> _Verbosity { get; set; }
        public uint Verbosity
        {
            get
            {
                if (_Verbosity.HasValue) return (uint)_Verbosity;

                _Verbosity = uint.TryParse(ConfigurationManager.AppSettings["Verbosity"], out uint iReturn) ? iReturn : 3;
                
                return (uint)_Verbosity;
            }
            set { _Verbosity = value; }
        }

        [Option('c', "config", Required = false, HelpText = "Override location of the config file. Full or relative path must be provided. [-c c:\\configs\\MTOvJoyFeeder.json]")]
        private string _ConfigFile { get; set; }
        public string ConfigFile
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(_ConfigFile)) return _ConfigFile;

                string sConfigPath = ConfigurationManager.AppSettings["ConfigPath"];

                if (string.IsNullOrWhiteSpace(sConfigPath))
                {
                    sConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                }

                string sConfigFile = ConfigurationManager.AppSettings["ConfigFile"];
                if (string.IsNullOrWhiteSpace(sConfigFile))
                {
                    sConfigFile += "MTOvJoyFeeder.json";
                }

                _ConfigFile = sConfigPath + "\\" + sConfigFile;

                return _ConfigFile;
            }
            set { _ConfigFile = value; }
        }

        [Option('m', "MaxErrors", Required = false, HelpText = "Maximum number of errors before removing a Joystick from monitoring. [-m 500]")]
        private Nullable<uint> _ErrorsBeforeJoystickRemoval { get; set; }
        public uint ErrorsBeforeJoystickRemoval
        {
            get
            {
                if (_ErrorsBeforeJoystickRemoval.HasValue) return (uint)_ErrorsBeforeJoystickRemoval;

                _ErrorsBeforeJoystickRemoval = uint.TryParse(ConfigurationManager.AppSettings["ErrorsBeforeJoystickRemoval"], out uint iReturn) ? iReturn : 50;

                return (uint)_ErrorsBeforeJoystickRemoval;
            }
            set { _ErrorsBeforeJoystickRemoval = value; }
        }

        [Option('s', "silent", Required = false, HelpText = "If set to true, no messages will be sent to the console. [-s true]")]
        private Nullable<Boolean> _Silent { get; set; }
        public Boolean Silent
        {
            get
            {
                if (_Silent.HasValue) return (Boolean)_Silent;

                _Silent = Boolean.TryParse(ConfigurationManager.AppSettings["Silent"], out Boolean bReturn) ? bReturn : false;
                
                return (Boolean)_Silent;
            }
            set { _Silent = value; }
        }

        [Option('e', "EnableEventLogging", Required = false, HelpText = "If set to true messages will be sent to the event log. Requires Admin privalages for the first run to generate the initial log. [-e true]")]
        private Nullable<Boolean> _EnableEventLogging { get; set; }
        public Boolean EnableEventLogging
        {
            get
            {
                if (_EnableEventLogging.HasValue) return (Boolean)_EnableEventLogging;

                _EnableEventLogging = Boolean.TryParse(ConfigurationManager.AppSettings["EnableEventLogging"], out Boolean bReturn) ? bReturn : false;               

                return (Boolean)_EnableEventLogging;
            }
            set { _EnableEventLogging = value; }
        }

        [Option('t', "SleepTime", Required = false, HelpText = "Milliseconds to sleep between buffer checks. [-t 50]")]
        private Nullable<int> _SleepTime { get; set; }
        public int SleepTime
        {
            get
            {
                if (_SleepTime.HasValue) return (int)_SleepTime;

                _SleepTime = int.TryParse(ConfigurationManager.AppSettings["SleepTime"], out int iReturn) ? iReturn : 50;

                return (int)_SleepTime;
            }
            set { _SleepTime = value; }
        }
    }

    public class vJoystickInfo
    {
        public uint id = 1;

        public DevType ControlerType = DevType.vXbox;

        public vXboxInterfaceWrap.PXINPUT_VIBRATION pVib = new vXboxInterfaceWrap.PXINPUT_VIBRATION();

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

        public uint[] Map_To_vJoyDevice_Ids = { 1 };
        public uint[] Map_To_xBox_Ids = { 1 };

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

        public uint[] Map_To_vJoyDevice_Ids = { };
        public uint[] Map_To_xBox_Ids = { 1 };

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

        public uint[] Map_Buttons = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }; // SharpDX.DirectInput.JoystickOffset.Buttons0;
    }
}
