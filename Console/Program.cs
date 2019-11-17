using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using CommandLine;
using SharpDX.DirectInput;
using vGenInterfaceWrap;
using vXboxInterfaceWrap;
using System.Linq;
using System.Runtime.InteropServices;
using CommandLine.Text;

namespace MTOvJoyFeeder
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        static public vGen oVirtualJoystick;
        static public Options goOptions;

        private delegate bool EventHandler(CtrlType oCtrlType);
        static EventHandler oEventHandler;       
        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        static void Main(string[] args)
        {
            var oParser = new CommandLine.Parser(with => with.HelpWriter = null);
            var oPparserResult = oParser.ParseArguments<Options>(args);
            oPparserResult
             .WithParsed<Options>(options => Run(options))
             .WithNotParsed(errs => DisplayHelp(oPparserResult, errs));
        } // Main
        
        static void DisplayHelp<T>(ParserResult<T> oHelpResult, IEnumerable<Error> oErrors)
        {
            string sHelpText;

            if (oErrors.IsVersion())  //check if error is version request
            {
                sHelpText = HelpText.AutoBuild(oHelpResult);
            }
            else
            {
                sHelpText = HelpText.AutoBuild(oHelpResult, oOptions =>
            {
                oOptions.AdditionalNewLineAfterOption = false; //remove the extra newline between options
                oOptions.MaximumDisplayWidth = 50000;
                return HelpText.DefaultParsingErrorsHandler(oHelpResult, oOptions);
            }, e => e);
            }

                // I do not like the default formating that CommandLine Parser provides.
                Console.WriteLine(sHelpText.Replace("<br/>", "\n".PadRight(31)));
            }

        static void Run(Options oOptions)
        {
            goOptions = oOptions;
            
            oEventHandler += new EventHandler(OnExit);
            SetConsoleCtrlHandler(oEventHandler, true);
            
            List<JoystickConfig> oJoystickConfig = Config.ReadConfigFile();

            List<JoystickInfo> oAllJoystickInfo = new List<JoystickInfo>();
            DetectPhysicalJoysticks(oAllJoystickInfo, oJoystickConfig);

            List<uint> ovJoyDeviceIdsToUse = new List<uint>();
            List<uint> oxBoxIdsToUse = new List<uint>();
            foreach (var oJoystickInfo in oAllJoystickInfo)
            {
                ovJoyDeviceIdsToUse.AddRange(oJoystickInfo.Map_To_vJoyDevice_Ids);
                oxBoxIdsToUse.AddRange(oJoystickInfo.Map_To_xBox_Ids);
            }

            List<vJoystickInfo> oAllVJoystickInfo = new List<vJoystickInfo>();
            DetectVirtualJoysticks(oAllVJoystickInfo, ovJoyDeviceIdsToUse.Distinct().ToList());
            CreatevXboxJoysticks(oAllVJoystickInfo, oxBoxIdsToUse.Distinct().ToList());

            if (oJoystickConfig == null)
            {
                oJoystickConfig = Config.CreateConfigFile(oAllJoystickInfo);
            }

            try
            {
                PollJoysticks(oAllVJoystickInfo, oAllJoystickInfo);
            }
            finally
            {
                ReleasevXboxJoysticks(oxBoxIdsToUse.Distinct().ToList());
            }
        }

        static Boolean CreatevXboxJoysticks(List<vJoystickInfo> oAllVJoystickInfo, List<uint> oVirtualJoysticksToUse)
        {
            byte Led = 0xFF;
            Boolean bReturn = false;
            
            if (vXboxInterface.isVBusExists())
            {
                WriteToEventLog("\nxBox vBus interface found...");
            }
            else
            {
                WriteToEventLog("\nvBus interface for xBox controller not enabled...");
                return false;
            }

            byte nSlots = 0xFF;
            vXboxInterface.GetNumEmptyBusSlots(ref nSlots);

            WriteToEventLog($"{nSlots} Empty xBox Controller slots found.");

            // Check for avaiable devices
            foreach (uint iSlot in oVirtualJoysticksToUse)
            {
                bool bSlotInUse = false;

                bSlotInUse = vXboxInterface.isControllerExists(iSlot);
                WriteToEventLog("\nVirtual Device (vXbox) " + iSlot + " " + (bSlotInUse ? "in use, forcing release." : "not in use."));
                if (bSlotInUse && !vXboxInterface.isControllerOwned(iSlot))
                {
                    vXboxInterface.UnPlugForce(iSlot);
                    Thread.Sleep(1000);
                }

                vXboxInterface.PlugIn(iSlot);
                bSlotInUse = vXboxInterface.isControllerOwned(iSlot);

                if (bSlotInUse != true)
                {
                    WriteToEventLog("\tFailed to acquire");
                }
                else
                {
                    vXboxInterface.GetLedNumber(iSlot, ref Led);
                    WriteToEventLog("\tAcquired\n\tLED number " + Led.ToString());

                    vJoystickInfo oNewVJoystickInfo = new vJoystickInfo();
                    oNewVJoystickInfo.id = iSlot;
                    oNewVJoystickInfo.ControlerType = DevType.vXbox;

                    oNewVJoystickInfo.lMinXVal = Range.MinXVal;
                    oNewVJoystickInfo.lMaxXVal = Range.MaxXVal;
                    oNewVJoystickInfo.lXRange  = Range.MaxXVal - Range.MinXVal;
                    oNewVJoystickInfo.lMinPlusMaxX = Range.MaxXVal + Range.MinXVal;

                    oNewVJoystickInfo.lMinYVal = Range.MinYVal;
                    oNewVJoystickInfo.lMaxYVal = Range.MaxYVal;
                    oNewVJoystickInfo.lYRange  = Range.MaxYVal - Range.MinYVal;
                    oNewVJoystickInfo.lMinPlusMaxY = Range.MaxYVal + Range.MinYVal;

                    oNewVJoystickInfo.lMinZVal = Range.MinZVal;
                    oNewVJoystickInfo.lMaxZVal = Range.MaxZVal;
                    oNewVJoystickInfo.lZRange  = Range.MaxZVal - Range.MinZVal;
                    oNewVJoystickInfo.lMinPlusMaxZ = Range.MaxZVal + Range.MinZVal;

                    oNewVJoystickInfo.lMinRXVal = Range.MinRXVal;
                    oNewVJoystickInfo.lMaxRXVal = Range.MaxRXVal;
                    oNewVJoystickInfo.lRXRange  = Range.MaxRXVal - Range.MinRXVal;
                    oNewVJoystickInfo.lMinPlusMaxRX = Range.MaxRXVal + Range.MinRXVal;

                    oNewVJoystickInfo.lMinRYVal = Range.MinRYVal;
                    oNewVJoystickInfo.lMaxRYVal = Range.MaxRYVal;
                    oNewVJoystickInfo.lRYRange  = Range.MaxRYVal - Range.MinRYVal;
                    oNewVJoystickInfo.lMinPlusMaxRY = Range.MaxRYVal + Range.MinRYVal;


                    oNewVJoystickInfo.PercentX = (oNewVJoystickInfo.lMaxXVal - oNewVJoystickInfo.lMinXVal) * oNewVJoystickInfo.Percentage_Slack;
                    oNewVJoystickInfo.PercentY = (oNewVJoystickInfo.lMaxYVal - oNewVJoystickInfo.lMinYVal) * oNewVJoystickInfo.Percentage_Slack;
                    oNewVJoystickInfo.PercentZ = (oNewVJoystickInfo.lMaxZVal - oNewVJoystickInfo.lMinZVal) * oNewVJoystickInfo.Percentage_Slack;
                    oNewVJoystickInfo.PercentRX = (oNewVJoystickInfo.lMaxRXVal - oNewVJoystickInfo.lMinRXVal) * oNewVJoystickInfo.Percentage_Slack;
                    oNewVJoystickInfo.PercentRY = (oNewVJoystickInfo.lMaxRYVal - oNewVJoystickInfo.lMinRYVal) * oNewVJoystickInfo.Percentage_Slack;
                    oNewVJoystickInfo.PercentRZ = (oNewVJoystickInfo.lMaxRZVal - oNewVJoystickInfo.lMinRZVal) * oNewVJoystickInfo.Percentage_Slack;

                    oAllVJoystickInfo.Add(oNewVJoystickInfo);

                    bReturn = true;
                }
            }

            return bReturn;
        }
        static void ReleasevXboxJoysticks(List<uint> oVirtualJoysticksToUse = null)
        {
            if (oVirtualJoysticksToUse == null)
            {
                oVirtualJoysticksToUse = new List<uint> { 1, 2, 3, 4 };
            }

            if (oVirtualJoysticksToUse.Count > 0)
            {
                WriteToEventLog(Environment.NewLine + "Releasing Virtual Device (vXBox) Controllers..." + Environment.NewLine);

                for (uint iLoop = 1; iLoop < 5; iLoop++)
                {
                    if (Program.goOptions.EnableUnPlugForce && vXboxInterface.isControllerExists(iLoop))
                    {
                        WriteToEventLog(String.Format("\tVirtual Device {0}: {1}", iLoop, vXboxInterface.UnPlugForce(iLoop) ? "Successful" : "Unsuccessful"));
                    }
                    else if (vXboxInterface.isControllerOwned(iLoop))
                    {
                        WriteToEventLog(String.Format("\tVirtual Device {0}: {1}", iLoop, vXboxInterface.UnPlug(iLoop) ? "Successful" : "Unsuccessful"));
                    }
                }

                WriteToEventLog(Environment.NewLine + "Controllers Released");
            }
        }

        static void DetectVirtualJoysticks(List<vJoystickInfo> oAllVJoystickInfo, List<uint> oVirtualJoysticksToUse)
        {
            // Create one joystick object and a position structure.
            oVirtualJoystick = new vGen();

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!oVirtualJoystick.vJoyEnabled())
            {
                WriteToEventLog($"\nvJoy driver not enabled: Failed Getting vJoy attributes.", Verbosity.Critical);
                return;
            }
            else
                WriteToEventLog($"\nProduct: {oVirtualJoystick.GetvJoyProductString()}\nVendor: {oVirtualJoystick.GetvJoyManufacturerString()}\nVersion Number: {oVirtualJoystick.GetvJoySerialNumberString()}");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = oVirtualJoystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                WriteToEventLog($"Version of Driver Matches DLL Version ({DllVer:X})\n");
            else
                WriteToEventLog($"Version of Driver ({DrvVer:X}) does NOT match DLL Version ({DllVer:X})\n");
            
            foreach (uint id in oVirtualJoysticksToUse)
            {
                vJoystickInfo oNewVJoystickInfo = new vJoystickInfo();
                oNewVJoystickInfo.id = id;
                oNewVJoystickInfo.ControlerType = DevType.vJoy;

                // Get the state of the requested device
                VjdStat status = oVirtualJoystick.GetVJDStatus(id);
                switch (status)
                {
                    case VjdStat.VJD_STAT_OWN:
                        WriteToEventLog($"Virtual Device (vJoy) {id} is already owned by this feeder.");
                        break;
                    case VjdStat.VJD_STAT_FREE:
                        WriteToEventLog($"Virtual Device (vJoy) Device {id} is free.");
                        oVirtualJoystick.AcquireVJD(id);
                        status = oVirtualJoystick.GetVJDStatus(id);
                        break;
                    case VjdStat.VJD_STAT_BUSY:
                        WriteToEventLog($"Virtual Device (vJoy) {id} is already owned by another feeder.");
                        break;
                    case VjdStat.VJD_STAT_MISS:
                        WriteToEventLog($"Virtual Device (vJoy) {id} is not installed or disabled.");
                        break;
                    default:
                        WriteToEventLog($"Virtual Device (vJoy) {id} general error.");
                        break;
                };

                if (status != VjdStat.VJD_STAT_OWN)
                {
                    WriteToEventLog($"\tFailed to acquire");
                    continue;
                }
                else
                {
                    WriteToEventLog($"\tAcquired");
                }

                if (oVirtualJoystick.IsDeviceFfb(id))
                {
                    object oFFData = null;
                    oVirtualJoystick.FfbRegisterGenCB(vJoyFfbCbFunc_CallBack, oFFData);
                }

                // Check which axes are supported
                bool AxisX = oVirtualJoystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
                bool AxisY = oVirtualJoystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
                bool AxisZ = oVirtualJoystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
                bool AxisRX = oVirtualJoystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
                bool AxisRY = oVirtualJoystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RY);
                bool AxisRZ = oVirtualJoystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);

                oVirtualJoystick.GetVJDAxisMin(id, HID_USAGES.HID_USAGE_X, ref oNewVJoystickInfo.lMinXVal);
                oVirtualJoystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref oNewVJoystickInfo.lMaxXVal);
                oVirtualJoystick.GetVJDAxisMin(id, HID_USAGES.HID_USAGE_Y, ref oNewVJoystickInfo.lMinYVal);
                oVirtualJoystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_Y, ref oNewVJoystickInfo.lMaxYVal);
                oVirtualJoystick.GetVJDAxisMin(id, HID_USAGES.HID_USAGE_Z, ref oNewVJoystickInfo.lMinZVal);
                oVirtualJoystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_Z, ref oNewVJoystickInfo.lMaxZVal);

                oVirtualJoystick.GetVJDAxisMin(id, HID_USAGES.HID_USAGE_RX, ref oNewVJoystickInfo.lMinRXVal);
                oVirtualJoystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_RX, ref oNewVJoystickInfo.lMaxRXVal);
                oVirtualJoystick.GetVJDAxisMin(id, HID_USAGES.HID_USAGE_RY, ref oNewVJoystickInfo.lMinRYVal);
                oVirtualJoystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_RY, ref oNewVJoystickInfo.lMaxRYVal);
                oVirtualJoystick.GetVJDAxisMin(id, HID_USAGES.HID_USAGE_RZ, ref oNewVJoystickInfo.lMinRZVal);
                oVirtualJoystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_RZ, ref oNewVJoystickInfo.lMaxRZVal);

                // Get the number of buttons and POV Hat switches supported by this vJoy device
                int nButtons = oVirtualJoystick.GetVJDButtonNumber(id);
                int ContPovNumber = oVirtualJoystick.GetVJDContPovNumber(id);
                int DiscPovNumber = oVirtualJoystick.GetVJDDiscPovNumber(id);

                // We make these calculations here one time for use in NormalizeRange()
                oNewVJoystickInfo.lXRange  = oNewVJoystickInfo.lMaxXVal  - oNewVJoystickInfo.lMinXVal;
                oNewVJoystickInfo.lYRange  = oNewVJoystickInfo.lMaxYVal  - oNewVJoystickInfo.lMinYVal;
                oNewVJoystickInfo.lZRange  = oNewVJoystickInfo.lMaxZVal  - oNewVJoystickInfo.lMinZVal;
                oNewVJoystickInfo.lRXRange = oNewVJoystickInfo.lMaxRXVal - oNewVJoystickInfo.lMinRXVal;
                oNewVJoystickInfo.lRYRange = oNewVJoystickInfo.lMaxRYVal - oNewVJoystickInfo.lMinRYVal;
                oNewVJoystickInfo.lRZRange = oNewVJoystickInfo.lMaxRZVal - oNewVJoystickInfo.lMinRZVal;

                oNewVJoystickInfo.lMinPlusMaxX  = oNewVJoystickInfo.lMaxXVal + oNewVJoystickInfo.lMinXVal;
                oNewVJoystickInfo.lMinPlusMaxY  = oNewVJoystickInfo.lMaxYVal + oNewVJoystickInfo.lMinYVal;
                oNewVJoystickInfo.lMinPlusMaxZ  = oNewVJoystickInfo.lMaxZVal + oNewVJoystickInfo.lMinZVal;
                oNewVJoystickInfo.lMinPlusMaxRX = oNewVJoystickInfo.lMaxRXVal + oNewVJoystickInfo.lMinRXVal;
                oNewVJoystickInfo.lMinPlusMaxRY = oNewVJoystickInfo.lMaxRYVal + oNewVJoystickInfo.lMinRYVal;
                oNewVJoystickInfo.lMinPlusMaxRZ = oNewVJoystickInfo.lMaxRZVal + oNewVJoystickInfo.lMinRZVal;

                oNewVJoystickInfo.PercentX = (oNewVJoystickInfo.lMaxXVal - oNewVJoystickInfo.lMinXVal) * oNewVJoystickInfo.Percentage_Slack;
                oNewVJoystickInfo.PercentY = (oNewVJoystickInfo.lMaxYVal - oNewVJoystickInfo.lMinYVal) * oNewVJoystickInfo.Percentage_Slack;
                oNewVJoystickInfo.PercentZ = (oNewVJoystickInfo.lMaxZVal - oNewVJoystickInfo.lMinZVal) * oNewVJoystickInfo.Percentage_Slack;
                oNewVJoystickInfo.PercentRX = (oNewVJoystickInfo.lMaxRXVal - oNewVJoystickInfo.lMinRXVal) * oNewVJoystickInfo.Percentage_Slack;
                oNewVJoystickInfo.PercentRY = (oNewVJoystickInfo.lMaxRYVal - oNewVJoystickInfo.lMinRYVal) * oNewVJoystickInfo.Percentage_Slack;
                oNewVJoystickInfo.PercentRZ = (oNewVJoystickInfo.lMaxRZVal - oNewVJoystickInfo.lMinRZVal) * oNewVJoystickInfo.Percentage_Slack;
                
                // Print results
                WriteToEventLog($"\tCapabilities:");
                WriteToEventLog($"\t\tNumber of buttons:\t\t{nButtons}");
                WriteToEventLog($"\t\tNumber of Continuous POVs:\t{ContPovNumber}");
                WriteToEventLog($"\t\tNumber of Descrete POVs:\t{DiscPovNumber}");

                WriteToEventLog(String.Format("\t\tAxis X:\t\t\t\t{0}", AxisX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\t\tX Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinXVal, oNewVJoystickInfo.lMaxXVal));
                WriteToEventLog(String.Format("\t\tAxis Y:\t\t\t\t{0}", AxisX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\t\tY Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinYVal, oNewVJoystickInfo.lMaxYVal));
                WriteToEventLog(String.Format("\t\tAxis Z:\t\t\t\t{0}", AxisX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\t\tZ Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinZVal, oNewVJoystickInfo.lMaxZVal));

                WriteToEventLog(String.Format("\t\tAxis Rx:\t\t\t{0}", AxisRX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\t\tRx Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRXVal, oNewVJoystickInfo.lMaxRXVal));
                WriteToEventLog(String.Format("\t\tAxis Ry:\t\t\t{0}", AxisRY ? "Yes" : "No"));
                WriteToEventLog(String.Format("\t\tRy Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRYVal, oNewVJoystickInfo.lMaxRYVal));
                WriteToEventLog(String.Format("\t\tAxis Rz:\t\t\t{0}", AxisRZ ? "Yes" : "No"));
                WriteToEventLog(String.Format("\t\tRz Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRZVal, oNewVJoystickInfo.lMaxRZVal));

                WriteToEventLog();

                oAllVJoystickInfo.Add(oNewVJoystickInfo);
            }
        }
        static void DetectPhysicalJoysticks(List<JoystickInfo> oAllJoystickInfo, List<JoystickConfig> oJoystickConfig)
        {
            // Initialize DirectInput
            var directInput = new DirectInput();

            List<DeviceInstance> oDeviceInstances = new List<DeviceInstance>();

            // Get all physical controllers that are available and enabled in the config file
            var oGetDevices = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            foreach (var oDeviceInstance in oGetDevices)
            {
                oDeviceInstance.InstanceName = oDeviceInstance.InstanceName.Trim();

                if (oDeviceInstance.InstanceName != "vJoy Device")
                {
                    if (oJoystickConfig == null
                        || (oJoystickConfig.Find(x => x.Instance_GUID == oDeviceInstance.InstanceGuid && x.Enabled) != null)
                        || (oJoystickConfig.Find(x => x.Product_GUID == oDeviceInstance.ProductGuid && x.Enabled) != null)
                        )
                    {
                        oDeviceInstances.Add(oDeviceInstance);
                    }
                }
            }

            // If Joystick not found, throw an error
            if (oDeviceInstances.Count == 0)
            {
                WriteToEventLog("No joystick/Gamepad found.");
                Environment.Exit(1);
            }

            List<Joystick> oJoysticks = new List<Joystick>();

            foreach (var oDeviceInstance in oDeviceInstances)
            {
                JoystickConfig oThisJoystickConfig;
                if (oJoystickConfig != null)
                {
                    oThisJoystickConfig = oJoystickConfig.Find(x => x.Product_GUID == oDeviceInstance.ProductGuid && x.Enabled);
                }
                else
                {
                    oThisJoystickConfig = new JoystickConfig();
                }

                WriteToEventLog(String.Format("\nFound {0}", oDeviceInstance.InstanceName));
                WriteToEventLog(String.Format("\tInstance_Name:\t\t\t{0}", oDeviceInstance.InstanceName));
                WriteToEventLog(String.Format("\tInstance_Type:\t\t\t{0}", oDeviceInstance.Type));
                WriteToEventLog(String.Format("\tInstance_GUID:\t\t\t{0}", oDeviceInstance.InstanceGuid));
                WriteToEventLog(String.Format("\tProduct_GUID:\t\t\t{0}", oDeviceInstance.ProductGuid));

                var oNewJoystickInfo = new JoystickInfo();

                oNewJoystickInfo.oDeviceInstance = oDeviceInstance;

                // Instantiate the joystick
                oNewJoystickInfo.oJoystick = new Joystick(directInput, oDeviceInstance.ProductGuid);

                // Query all suported ForceFeedback effects
                oNewJoystickInfo.oEffectInfo = oNewJoystickInfo.oJoystick.GetEffects();
                foreach (var oEffect in oNewJoystickInfo.oEffectInfo)
                    WriteToEventLog(String.Format("Force Feedback Effect available:\t{0}", oEffect.Name));

                // Set BufferSize in order to use buffered data.
                oNewJoystickInfo.oJoystick.Properties.BufferSize = 128;

                // Acquire the joystick
                oNewJoystickInfo.oJoystick.Acquire();

                oNewJoystickInfo.oJoystickConfig = oThisJoystickConfig;
                oNewJoystickInfo.Map_To_vJoyDevice_Ids = oThisJoystickConfig.Map_To_vJoyDevice_Ids;
                oNewJoystickInfo.Map_To_xBox_Ids = oThisJoystickConfig.Map_To_xBox_Ids;                

                WriteToEventLog(String.Format("\tNumber of buttons:\t\t{0}", oNewJoystickInfo.oJoystick.Capabilities.ButtonCount));
                WriteToEventLog(String.Format("\tNumber of POVs:\t\t\t{0}", oNewJoystickInfo.oJoystick.Capabilities.PovCount));

                var oJoystickObjects = oNewJoystickInfo.oJoystick.GetObjects(DeviceObjectTypeFlags.All);

                foreach (var oJoystickObject in oJoystickObjects)
                {
                    var oObjectProperties = oNewJoystickInfo.oJoystick.GetObjectPropertiesById(oJoystickObject.ObjectId);
                    oJoystickObject.Name += ":";
                    oJoystickObject.Name = oJoystickObject.Name.PadRight(25);
                    switch (oJoystickObject.Name.TrimEnd())
                    {
                        case "X Axis:":
                            oNewJoystickInfo.lMinXVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxXVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.lXRange  = oObjectProperties.Range.Maximum - oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.PercentX = (oNewJoystickInfo.lMaxXVal - oNewJoystickInfo.lMinXVal) * oThisJoystickConfig.Percentage_Slack;
                            oNewJoystickInfo.Map_X = StringToHID_USAGES(oThisJoystickConfig.Map_X);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;
                        case "Y Axis:":
                            oNewJoystickInfo.lMinYVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxYVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.lYRange  = oObjectProperties.Range.Maximum - oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.PercentY = (oNewJoystickInfo.lMaxYVal - oNewJoystickInfo.lMinYVal) * oThisJoystickConfig.Percentage_Slack;
                            oNewJoystickInfo.Map_Y = StringToHID_USAGES(oThisJoystickConfig.Map_Y);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;
                        case "Z Axis:":
                            oNewJoystickInfo.lMinZVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxZVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.lZRange = oObjectProperties.Range.Maximum - oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.PercentZ = (oNewJoystickInfo.lMaxZVal - oNewJoystickInfo.lMinZVal) * oThisJoystickConfig.Percentage_Slack;
                            oNewJoystickInfo.Map_Z = StringToHID_USAGES(oThisJoystickConfig.Map_Z);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;

                        case "X Rotation:":
                            oNewJoystickInfo.lMinRXVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRXVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.lRXRange = oObjectProperties.Range.Maximum - oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.PercentRX = (oNewJoystickInfo.lMaxRXVal - oNewJoystickInfo.lMinRXVal) * oThisJoystickConfig.Percentage_Slack;
                            oNewJoystickInfo.Map_RotationX = StringToHID_USAGES(oThisJoystickConfig.Map_RotationX);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;
                        case "Y Rotation:":
                            oNewJoystickInfo.lMinRYVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRYVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.lRYRange = oObjectProperties.Range.Maximum - oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.PercentRY = (oNewJoystickInfo.lMaxRYVal - oNewJoystickInfo.lMinRYVal) * oThisJoystickConfig.Percentage_Slack;
                            oNewJoystickInfo.Map_RotationY = StringToHID_USAGES(oThisJoystickConfig.Map_RotationY);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));

                            break;
                        case "Z Rotation:":
                            oNewJoystickInfo.lMinRZVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRZVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.lRZRange = oObjectProperties.Range.Maximum - oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.PercentRZ = (oNewJoystickInfo.lMaxRZVal - oNewJoystickInfo.lMinRZVal) * oThisJoystickConfig.Percentage_Slack;
                            oNewJoystickInfo.Map_RotationZ = StringToHID_USAGES(oThisJoystickConfig.Map_RotationZ);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;
                        case "Hat Switch:":
                            oNewJoystickInfo.Map_PointOfViewControllers0 = oThisJoystickConfig.Map_PointOfViewControllers0;

                            WriteToEventLog(String.Format("\t{0}\tYes", oJoystickObject.Name));
                            break;

                        default:
                            if (oJoystickObject.Name.Contains("Button "))
                            {
                                string sButtonNumber = System.Text.RegularExpressions.Regex.Match(oJoystickObject.Name, @"Button (\d+):").Groups[1].Value;
                                if (uint.TryParse(sButtonNumber, out uint iButtonNumber))
                                {
                                    if (iButtonNumber < oNewJoystickInfo.oJoystickConfig.Map_Buttons.Length)
                                    {                                    
                                        WriteToEventLog(String.Format("\t{0}\tYes", oJoystickObject.Name.PadRight(25)));
                                    }
                                    else
                                    {
                                        WriteToEventLog(String.Format("\t{0}\tAvailable, Not mapped in config file", oJoystickObject.Name.PadRight(25)));
                                    }
                                }                                
                            }
                            else
                            {
                                WriteToEventLog(String.Format("\t{0}\tAvailable, Not supported", oJoystickObject.Name.PadRight(25)));
                            }
                            break;
                    }
                }

                if (oNewJoystickInfo.Map_To_vJoyDevice_Ids.Length > 0 || oNewJoystickInfo.Map_To_xBox_Ids.Length > 0)
                {
                    oAllJoystickInfo.Add(oNewJoystickInfo);
                }
            }
        }

        private static void vJoyFfbCbFunc_CallBack(IntPtr data, object userData)
        {
            WriteToEventLog(data.ToString());
        }

        static JoystickOffset StringToOffset(string sOffset)
        {
            switch (sOffset.ToUpper())
            {
                case "X":
                    return JoystickOffset.X;
                case "Y":
                    return JoystickOffset.Y;
                case "Z":
                    return JoystickOffset.Z;
                case "ROTATIONX":
                    return JoystickOffset.RotationX;
                case "ROTATIONY":
                    return JoystickOffset.RotationY;
                case "ROTATIONZ":
                    return JoystickOffset.RotationZ;
            }

            // Object isn't nullable, instead of adding extra nullable code we select the first option which is seldom used
            return JoystickOffset.AccelerationSliders0;
        }
        static HID_USAGES StringToHID_USAGES(string sOffset)
        {
            switch (sOffset.ToUpper())
            {
                case "X":
                    return HID_USAGES.HID_USAGE_X;
                case "Y":
                    return HID_USAGES.HID_USAGE_Y;
                case "Z":
                    return HID_USAGES.HID_USAGE_Z;
                case "ROTATIONX":
                    return HID_USAGES.HID_USAGE_RX;
                case "ROTATIONY":
                    return HID_USAGES.HID_USAGE_RY;
                case "ROTATIONZ":
                    return HID_USAGES.HID_USAGE_RZ;
            }

            // Object isn't nullable, instead of adding extra nullable code we select the first option which is seldom used
            return HID_USAGES.HID_USAGE_SL0;
        }
        static HID_USAGES JoystickOffsetToHID_USAGES(JoystickOffset sOffset)
        {
            switch (sOffset)
            {
                case JoystickOffset.X:
                    return HID_USAGES.HID_USAGE_X;
                case JoystickOffset.Y:
                    return HID_USAGES.HID_USAGE_Y;
                case JoystickOffset.Z:
                    return HID_USAGES.HID_USAGE_Z;
                case JoystickOffset.RotationX:
                    return HID_USAGES.HID_USAGE_RX;
                case JoystickOffset.RotationY:
                    return HID_USAGES.HID_USAGE_RY;
                case JoystickOffset.RotationZ:
                    return HID_USAGES.HID_USAGE_RZ;
            }

            // Object isn't nullable, instead of adding extra nullable code we select the first option which is seldom used
            return HID_USAGES.HID_USAGE_SL0;
        }

        static void PollJoysticks(List<vJoystickInfo> oAllVJoystickInfo, List<JoystickInfo> oAllJoystickInfo)
        {
            if (oAllVJoystickInfo.Count == 0 || oAllJoystickInfo.Count == 0)
            {
                WriteToEventLog(String.Format("\nNo controllers to monitor. Check that controllers are plugged in, that virtual controller software is installed and working, and that physical controllers are mapped to virtual controllers in the config file ({0})", goOptions.ConfigFile, Verbosity.Warning));
                return;
            }

            WriteToEventLog("\nMonitoring for events.\n");

            JoystickUpdate[] oBufferedData;
            JoystickInfo oJoystickInfo;
            int iSleepTime = goOptions.SleepTime;

            // Poll events from joystick
            while (true)
            {
                for (int i = oAllJoystickInfo.Count; i-- > 0;)
                {
                    oJoystickInfo = oAllJoystickInfo[i];

                    try
                    {
                        //oJoystickInfo.oJoystick.SetNotification(foo);
                        oJoystickInfo.oJoystick.Poll();
                        oBufferedData = oJoystickInfo.oJoystick.GetBufferedData();
                    }
                    catch (Exception oException)
                    {
                        oBufferedData = null;

                        try
                        {
                            oJoystickInfo.oJoystick.Acquire();
                            oJoystickInfo.ErrorCount = 0;
                        }
                        catch (Exception oAcquireException)
                        {
                            if (oJoystickInfo.ErrorCount++ > goOptions.ErrorsBeforeJoystickRemoval)
                            {
                                WriteToEventLog(String.Format("\tRemoving {0}: {1} - {2}", oJoystickInfo.oDeviceInstance.InstanceName, oException.Message, oAcquireException.Message), Verbosity.Critical);
                                oAllJoystickInfo.Remove(oJoystickInfo);
                            }
                        }
                        
#if DEBUG
                        WriteToEventLog(String.Format("\tException ({0}): {1}", oJoystickInfo.ErrorCount, oException.Message), Verbosity.Debug);
#endif
                    }

                    if (oBufferedData != null)
                    {
                        foreach (uint iVJoystickId in oJoystickInfo.Map_To_xBox_Ids)
                        {
                            var oThisVJoystickInfo = oAllVJoystickInfo.Find(x => x.id == iVJoystickId && x.ControlerType == DevType.vXbox);
                            if (oThisVJoystickInfo == null) continue;

                            foreach (var oState in oBufferedData)
                            {
                                SendCommand(iVJoystickId, oJoystickInfo, oThisVJoystickInfo, oState);
                            }

                            // Check for force feedback                            
                            if (vXboxInterface.GetVibration(iVJoystickId, ref oThisVJoystickInfo.pVib))
                            {
                                //Vibration vibration = new Vibration();
                                //vibration.LeftMotorSpeed = oThisVJoystickInfo.pVib.wLeftMotorSpeed;
                                //vibration.RightMotorSpeed = oThisVJoystickInfo.pVib.wRightMotorSpeed;
                                //Controller controller = new Controller(UserIndex.Two);
                                ////controller.GetType()

                                //DeviceQueryType devicequerytype = new DeviceQueryType();
                                //devicequerytype = DeviceQueryType.Gamepad;
                                //controller.SetVibration(vibration);

#if DEBUG
                                // WriteToEventLog($"\t\tGetVibration: {oThisVJoystickInfo.pVib.wLeftMotorSpeed}, {oThisVJoystickInfo.pVib.wRightMotorSpeed}", Verbosity.Debug);
#endif
                            }
                        }

                        foreach (uint iVJoystickId in oJoystickInfo.Map_To_vJoyDevice_Ids)
                        {
                            var oThisVJoystickInfo = oAllVJoystickInfo.Find(x => x.id == iVJoystickId && x.ControlerType == DevType.vJoy);
                            if (oThisVJoystickInfo == null) continue;

                            foreach (var oState in oBufferedData)
                            {
                                SendCommand(iVJoystickId, oJoystickInfo, oThisVJoystickInfo, oState);
                            }
                        }
                    }
                }

                Thread.Sleep(iSleepTime);
            }
        }

        private static void SendCommand(uint iVJoystickId, JoystickInfo oJoystickInfo, vJoystickInfo oThisVJoystickInfo, JoystickUpdate oState)
        {
            long lNormalizedValue;

            switch (oState.Offset)
            {
                case SharpDX.DirectInput.JoystickOffset.X:
                    lNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinXVal, oJoystickInfo.lXRange, oThisVJoystickInfo.lMinXVal, oThisVJoystickInfo.lXRange);
                    if ((Math.Abs(oJoystickInfo.PreviousX - oState.Value) > oJoystickInfo.PercentX) || (Math.Abs(oThisVJoystickInfo.PreviousX - lNormalizedValue) > oThisVJoystickInfo.PercentX))
                    {
                        oJoystickInfo.PreviousX = oState.Value;
                        oThisVJoystickInfo.PreviousX = lNormalizedValue;

                        if (oJoystickInfo.oJoystickConfig.Invert_X)
                        {
                            lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxX - lNormalizedValue;
                        }

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis((int)lNormalizedValue, iVJoystickId, oJoystickInfo.Map_X);
                        }
                        else
                        {
                            vXboxInterface.SetAxis(iVJoystickId, (short)lNormalizedValue, oJoystickInfo.Map_X);
                        }

                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [{3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_X, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

#if DEBUG
                        WriteToEventLog(String.Format("\t\t{0}\r\n\t\tNormalized to: {1}", oState, lNormalizedValue), Verbosity.Debug);
#endif
                    }
                    break;
                case SharpDX.DirectInput.JoystickOffset.Y:
                    lNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinYVal, oJoystickInfo.lYRange, oThisVJoystickInfo.lMinYVal, oThisVJoystickInfo.lYRange);
                    if ((Math.Abs(oJoystickInfo.PreviousY - oState.Value) > oJoystickInfo.PercentY) || (Math.Abs(oThisVJoystickInfo.PreviousY - lNormalizedValue) > oThisVJoystickInfo.PercentY))
                    {
                        oJoystickInfo.PreviousY = oState.Value;
                        oThisVJoystickInfo.PreviousY = lNormalizedValue;

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            if (oJoystickInfo.oJoystickConfig.Invert_Y)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxY - lNormalizedValue;
                            }

                            oVirtualJoystick.SetAxis((int)lNormalizedValue, iVJoystickId, oJoystickInfo.Map_Y);
                        }
                        else
                        {
                            if (!oJoystickInfo.oJoystickConfig.Invert_Y)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxY - lNormalizedValue;
                            }

                            vXboxInterface.SetAxis(iVJoystickId, (short)lNormalizedValue, oJoystickInfo.Map_Y);
                        }

                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [{3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_Y, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

#if DEBUG
                        WriteToEventLog(String.Format("\t\t{0}\r\n\t\tNormalized to: {1}", oState, lNormalizedValue), Verbosity.Debug);
#endif
                    }
                    break;
                case SharpDX.DirectInput.JoystickOffset.Z:
                    lNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinZVal, oJoystickInfo.lZRange, oThisVJoystickInfo.lMinZVal, oThisVJoystickInfo.lZRange);
                    if ((Math.Abs(oJoystickInfo.PreviousZ - oState.Value) > oJoystickInfo.PercentZ) || (Math.Abs(oThisVJoystickInfo.PreviousZ - lNormalizedValue) > oThisVJoystickInfo.PercentZ))
                    {
                        oJoystickInfo.PreviousZ = oState.Value;
                        oThisVJoystickInfo.PreviousZ = lNormalizedValue;

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            if (oJoystickInfo.oJoystickConfig.Invert_Z)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxZ - lNormalizedValue;
                            }

                            oVirtualJoystick.SetAxis((int)lNormalizedValue, iVJoystickId, oJoystickInfo.Map_Z);
                        }
                        else
                        {
                            if (!oJoystickInfo.oJoystickConfig.Invert_Z)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxZ - lNormalizedValue;
                            }

                            vXboxInterface.SetZAxis(iVJoystickId, (short)lNormalizedValue);
                        }

                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [{3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_Z, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

#if DEBUG
                        WriteToEventLog(String.Format("\t\t{0}\r\n\t\tNormalized to: {1}", oState, lNormalizedValue), Verbosity.Debug);
#endif
                    }
                    break;

                case SharpDX.DirectInput.JoystickOffset.RotationX:
                    lNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRXVal, oJoystickInfo.lRXRange, oThisVJoystickInfo.lMinRXVal, oThisVJoystickInfo.lRXRange);
                    if ((Math.Abs(oJoystickInfo.PreviousRX - oState.Value) > oJoystickInfo.PercentRX) || (Math.Abs(oThisVJoystickInfo.PreviousRX - lNormalizedValue) > oThisVJoystickInfo.PercentRX))
                    {
                        oJoystickInfo.PreviousRX = oState.Value;
                        oThisVJoystickInfo.PreviousRX = lNormalizedValue;

                        if (oJoystickInfo.oJoystickConfig.Invert_RotationX)
                        {
                            lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxRX - lNormalizedValue;
                        }

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis((int)lNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationX);
                        }
                        else
                        {
                            vXboxInterface.SetAxis(iVJoystickId, (short)lNormalizedValue, oJoystickInfo.Map_RotationX);
                        }

                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [{3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_RotationX, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

#if DEBUG
                        WriteToEventLog(String.Format("\t\t{0}\r\n\t\tNormalized to: {1}", oState, lNormalizedValue), Verbosity.Debug);
#endif
                    }
                    break;
                case SharpDX.DirectInput.JoystickOffset.RotationY:
                    lNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRYVal, oJoystickInfo.lRYRange, oThisVJoystickInfo.lMinRYVal, oThisVJoystickInfo.lRYRange);
                    if ((Math.Abs(oJoystickInfo.PreviousRY - oState.Value) > oJoystickInfo.PercentRY) || (Math.Abs(oThisVJoystickInfo.PreviousRY - lNormalizedValue) > oThisVJoystickInfo.PercentRY))
                    {
                        oJoystickInfo.PreviousRY = oState.Value;
                        oThisVJoystickInfo.PreviousRY = lNormalizedValue;

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            if (oJoystickInfo.oJoystickConfig.Invert_RotationY)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxRY - lNormalizedValue;
                            }

                            oVirtualJoystick.SetAxis((int)lNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationY);
                        }
                        else
                        {
                            if (!oJoystickInfo.oJoystickConfig.Invert_RotationY)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxRY - lNormalizedValue;
                            }

                            vXboxInterface.SetAxis(iVJoystickId, (short)lNormalizedValue, oJoystickInfo.Map_RotationY);
                        }

                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [{3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_RotationY, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

#if DEBUG
                        WriteToEventLog(String.Format("\t\t{0}\r\n\t\tNormalized to: {1}", oState, lNormalizedValue), Verbosity.Debug);
#endif
                    }
                    break;
                case SharpDX.DirectInput.JoystickOffset.RotationZ:
                    lNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRZVal, oJoystickInfo.lRZRange, oThisVJoystickInfo.lMinRZVal, oThisVJoystickInfo.lRZRange);
                    if ((Math.Abs(oThisVJoystickInfo.PreviousRZ - oState.Value) > oJoystickInfo.PercentRZ) || (Math.Abs(oThisVJoystickInfo.PreviousRZ - lNormalizedValue) > oThisVJoystickInfo.PercentRZ))
                    {
                        oJoystickInfo.PreviousRZ = oState.Value;
                        oThisVJoystickInfo.PreviousRZ = lNormalizedValue;

                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [{3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_RotationZ, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

#if DEBUG
                        WriteToEventLog(String.Format("\t\t{0}\r\n\t\tNormalized to: {1}", oState, lNormalizedValue), Verbosity.Debug);
#endif

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            if (oJoystickInfo.oJoystickConfig.Invert_RotationZ)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxRZ - lNormalizedValue;
                            }

                            oVirtualJoystick.SetAxis((int)lNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationZ);
                        }
                        else
                        {
                            if (!oJoystickInfo.oJoystickConfig.Invert_RotationZ)
                            {
                                lNormalizedValue = oThisVJoystickInfo.lMinPlusMaxRZ - lNormalizedValue;
                            }

                            vXboxInterface.SetAxis(iVJoystickId, (short)lNormalizedValue, oJoystickInfo.Map_RotationZ);

                        }

                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [{3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_RotationZ, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

#if DEBUG
                        WriteToEventLog(String.Format("\t\t{0}\r\n\t\tNormalized to: {1}", oState, lNormalizedValue), Verbosity.Debug);
#endif
                    }
                    break;

                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers0:
                    if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                    {
                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers0);
                    }
                    else
                    {
                        vXboxInterface.SetDpadByValue(iVJoystickId, oState.Value);
                    }

                    WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [PointOfViewController {3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_PointOfViewControllers0, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

                    break;
                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers1:

                    WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [PointOfViewController {3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_PointOfViewControllers1, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

                    if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                    {
                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers1);
                    }
                    else
                    {
                        WriteToEventLog("\t\tNot supported", Verbosity.Verbose);
                    }

                    break;
                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers2:

                    WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [PointOfViewController {3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_PointOfViewControllers2, oThisVJoystickInfo.ControlerType));

                    if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                    {
                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers2);
                    }
                    else
                    {
                        WriteToEventLog("\t\tNot supported", Verbosity.Verbose);
                    }

                    break;
                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers3:

                    WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [PointOfViewController {3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.Map_PointOfViewControllers3, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);

                    if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                    {
                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers3);
                    }
                    else
                    {
                        WriteToEventLog("\t\tNot supported", Verbosity.Verbose);
                    }

                    break;

                default:

                    if (oState.Offset.ToString().Contains("Buttons"))
                    {
                        string sButtonNumber = System.Text.RegularExpressions.Regex.Match(oState.Offset.ToString(), @"\d+$").Value;

                        if (int.TryParse(sButtonNumber, out int iButtonNumber))
                        {
                            if (iButtonNumber < oJoystickInfo.oJoystickConfig.Map_Buttons.Length)
                            {
                                if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                                {
                                    oVirtualJoystick.SetBtn(oState.Value != 0, iVJoystickId, (1 + oJoystickInfo.oJoystickConfig.Map_Buttons[iButtonNumber]));
                                }
                                else
                                {
                                    vXboxInterface.SetBtn(oState.Value != 0, iVJoystickId, oJoystickInfo.oJoystickConfig.Map_Buttons[iButtonNumber]);
                                }

                                WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {4} Device {2} [Button {3}]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oJoystickInfo.oJoystickConfig.Map_Buttons[iButtonNumber], oThisVJoystickInfo.ControlerType), Verbosity.Verbose);
                            }
                            else
                            {
                                WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {3} Device {2} [Button ?] - Not mapped in config file", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);
                            }
                        }
                    }
                    else
                    {
                        WriteToEventLog(String.Format("\t{0} [{1}] --> Virtual {3} Device {2} [?]", oJoystickInfo.oDeviceInstance.InstanceName, oState.Offset.ToString(), iVJoystickId, oThisVJoystickInfo.ControlerType), Verbosity.Verbose);
                        WriteToEventLog("\t\tNot supported", Verbosity.Verbose);
                    }

                    break;
            }
        }

        static int NormalizeRange(int num, long fromMin, decimal fromRange, long toMin, long toRange)
        {
            decimal lReturn = toMin + (num - fromMin) / fromRange * toRange;
            return (int)Math.Floor(lReturn);
        }

        public static void WriteToEventLog(string sMessage = "", int iVerbosity = Verbosity.Information)

        {
            if (goOptions.Verbosity < iVerbosity) return;

            if (!goOptions.Silent)
            {
                Console.WriteLine(sMessage);
            }

            if (!goOptions.EnableEventLogging) return;

            var oEventLog = new EventLog();

            if (!EventLog.SourceExists(AppDomain.CurrentDomain.FriendlyName))
            {
                EventLog.CreateEventSource(AppDomain.CurrentDomain.FriendlyName, "Application");
            }

            oEventLog.Source = AppDomain.CurrentDomain.FriendlyName;

            EventLogEntryType oEventLogEntryType = Verbosity.GetEventLogEntryType[iVerbosity];

            oEventLog.EnableRaisingEvents = true;
            oEventLog.WriteEntry(sMessage, oEventLogEntryType);
            oEventLog.Close();
        }

        private static bool OnExit(CtrlType oCtrlType)
        {
            switch (oCtrlType)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                default:
                    ReleasevXboxJoysticks();

                    WriteToEventLog("End of line");
                    return false;
            }
        }


    } // class Program
} // namespace MTOvJoyFeeder
