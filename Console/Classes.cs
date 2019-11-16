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

    class Options
    {
        private Nullable<uint> _Verbosity { get; set; }
        [Option('v', "Verbosity", HelpText =
              "The amount of verbosity to use. [-v 3]<br/><br/>"
            + "      Debug (5): Super detail.<br/>"
            + "    Verbose (4): Almost everything.<br/>"
            + "Information (3): Generally useful information.<br/>"
            + "    Warning (2): Expected problems.<br/>"
            + "      Error (1): Errors that are recoverable.<br/>"
            + "   Critical (0): Errors that stop execution.<br/>")]
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

        private string _ConfigFile { get; set; }
        [Option('c', "config", Required = false, HelpText = "Override location of the config file.<br/>Full or relative path must be provided.<br/>[-c c:\\configs\\MTOvJoyFeeder.json]")]
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

        private Nullable<uint> _ErrorsBeforeJoystickRemoval { get; set; }
        [Option('m', "MaxErrors", Required = false, HelpText = "Maximum number of errors before removing a Joystick from monitoring. [-m 500]")]
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

        private Nullable<Boolean> _Silent { get; set; }
        [Option('s', "silent", Required = false, HelpText = "If set to true, no messages will be sent to the console. [-s true]")]
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

        private Nullable<Boolean> _EnableEventLogging { get; set; }
        [Option('e', "EnableEventLogging", Required = false, HelpText = "If set to true messages will be sent to the event log.<br/>Requires Admin privalages for the first run to generate the initial log. [-e true]")]
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

        private Nullable<Boolean> _EnableUnPlugForce { get; set; }
        [Option('f', "EnableUnPlugForce", Required = false, HelpText = "On exit unplugs ALL virtual controllers controlled<br/>by any application, default is false. [-f true]")]
        public Boolean EnableUnPlugForce
        {
            get
            {
                if (_EnableUnPlugForce.HasValue) return (Boolean)_EnableUnPlugForce;

                _EnableUnPlugForce = Boolean.TryParse(ConfigurationManager.AppSettings["EnableUnPlugForce"], out Boolean bReturn) ? bReturn : false;

                return (Boolean)_EnableUnPlugForce;
            }
            set { _EnableUnPlugForce = value; }
        }

        private Nullable<int> _SleepTime { get; set; }
        [Option('t', "SleepTime", Required = false, HelpText = "Milliseconds to sleep between buffer checks. [-t 50]")]
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

        public double Percentage_Slack = .01;

        public double PercentX;
        public double PercentY;
        public double PercentZ;
        public double PercentRX;
        public double PercentRY;
        public double PercentRZ;

        public long lMinXVal;
        public long lMaxXVal;
        public long lXRange;
        public long lMinPlusMaxX;
        
        public long lMinYVal;
        public long lMaxYVal;
        public long lYRange;
        public long lMinPlusMaxY;

        public long lMinZVal;
        public long lMaxZVal;
        public long lZRange;
        public long lMinPlusMaxZ;

        public long lMinRXVal;
        public long lMaxRXVal;
        public long lRXRange;
        public long lMinPlusMaxRX;

        public long lMinRYVal;
        public long lMaxRYVal;
        public long lRYRange;
        public long lMinPlusMaxRY;

        public long lMinRZVal;
        public long lMaxRZVal;
        public long lRZRange;
        public long lMinPlusMaxRZ;

        public long PreviousX, PreviousY, PreviousZ;
        public long PreviousRX, PreviousRY, PreviousRZ;
    }

    public class JoystickInfo
    {
        public DeviceInstance oDeviceInstance;
        public Joystick oJoystick;
        public IList<EffectInfo> oEffectInfo;
        public JoystickConfig oJoystickConfig;

        public uint[] Map_To_vJoyDevice_Ids = { 1 };
        public uint[] Map_To_xBox_Ids = { 1 };

        public uint ErrorCount = 0;

        public long lMinXVal;
        public long lMaxXVal;
        public long lMinPlusMaxX;
        public long lXRange;
        
        public long lMinYVal;
        public long lMaxYVal;
        public long lMinPlusMaxY;
        public long lYRange;

        public long lMinZVal;
        public long lMaxZVal;
        public long lMinPlusMaxZY;
        public long lZRange;        

        public long lMinRXVal;
        public long lMaxRXVal;
        public long lMinPlusMaxRX;
        public long lRXRange;        

        public long lMinRYVal;
        public long lMaxRYVal;
        public long lMinPlusMaxRY;
        public long lRYRange;

        public long lMinRZVal;
        public long lMaxRZVal;
        public long lMinPlusMaxRZ;
        public long lRZRange;
        
        public double PercentX;
        public double PercentY;
        public double PercentZ;
        public double PercentRX;
        public double PercentRY;
        public double PercentRZ;

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

        public long PreviousX, PreviousY, PreviousZ;
        public long PreviousRX, PreviousRY, PreviousRZ;
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
        public bool   Invert_X = false;
        public string Map_Y = "Y"; // SharpDX.DirectInput.JoystickOffset.Y;
        public bool   Invert_Y = true;
        public string Map_Z = "Z"; // SharpDX.DirectInput.JoystickOffset.Z;
        public bool   Invert_Z = false;

        public string Map_RotationX = "RotationX"; // SharpDX.DirectInput.JoystickOffset.RotationX;
        public bool   Invert_RotationX = false;
        public string Map_RotationY = "RotationY"; // SharpDX.DirectInput.JoystickOffset.RotationY;
        public bool   Invert_RotationY = false;
        public string Map_RotationZ = "RotationZ"; // SharpDX.DirectInput.JoystickOffset.RotationZ;
        public bool   Invert_RotationZ = false;

        public uint Map_PointOfViewControllers0 = 0; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers0;
        public uint Map_PointOfViewControllers1 = 1; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers1;
        public uint Map_PointOfViewControllers2 = 2; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers2;
        public uint Map_PointOfViewControllers3 = 3; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers3;

        public uint[] Map_Buttons = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }; // SharpDX.DirectInput.JoystickOffset.Buttons0;
    }
}
