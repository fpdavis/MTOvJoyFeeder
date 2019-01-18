using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CommandLine;
using SharpDX.DirectInput;
//using vJoyInterfaceWrap;
using vGenInterfaceWrap;
using System.Linq;

namespace MTOvJoyFeeder
{
    class Program
    {
        //private static StringBuilder sbgMessageLog = new StringBuilder();

        //static public vJoy.JoystickState iReport;
        static public vGen oVirtualJoystick;
        static public Options goOptions;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(x => { Run(x); })
                .WithNotParsed((errs) => { return; });

        } // Main

        static void Run(Options oOptions)
        {
            goOptions = oOptions;
            
            List<JoystickConfig> oJoystickConfig = Config.ReadConfigFile();

            List<JoystickInfo> oAllJoystickInfo = new List<JoystickInfo>();
            DetectPhysicalJoysticks(oAllJoystickInfo, oJoystickConfig);

            List<uint> oVirtualJoysticksToUse = new List<uint>();
            foreach (var oJoystickInfo in oAllJoystickInfo)
            {
                oVirtualJoysticksToUse.AddRange(oJoystickInfo.Map_To_Virtual_Ids); 
            }

            List<vJoystickInfo> oAllVJoystickInfo = new List<vJoystickInfo>();
            DetectVirtualJoysticks(oAllVJoystickInfo, oVirtualJoysticksToUse.Distinct().ToList());

            if (oJoystickConfig == null)
            {
                oJoystickConfig = Config.CreateConfigFile(oAllJoystickInfo);
            }

            TestXBox();
            PollJoysticks(oAllVJoystickInfo, oAllJoystickInfo);
        }

        static void TestXBox()
        {
            uint id = 1;
            byte Led = 0xFF;

            if (oVirtualJoystick.isVBusExist() != NTSTATUS.Value.STATUS_SUCCESS)
            {
                WriteToEventLog("vBus for xBox controller not enabled...");
            }
            else
                WriteToEventLog("vBus found...");

            byte nSlots = 0xFF;
            oVirtualJoystick.GetNumEmptyBusSlots(ref nSlots);

            WriteToEventLog($"{nSlots} Empty Controller slots found.");

            if (nSlots < 1)
            {
                WriteToEventLog("No available vJoy Device found :( \t Cannot continue");
                Console.ReadKey();
                Environment.Exit(1);
            }            

            //Check for avaiable devices
            bool findid = false;
            for (id = 1; id <= 4; id++)
            {
                //Not Working
                oVirtualJoystick.isControllerPluggedIn(id, ref findid);
                WriteToEventLog("Device ID : " + id + " ( " + findid.ToString() + " )");
                if (!findid)
                    break;
            }

            if (findid == true)
            {
                WriteToEventLog("No available vJoy Device found :( \t Cannot continue");
                Console.ReadKey();
                Environment.Exit(1);
            }

            // Not Working
            //oVirtualJoystick.PlugIn(id);
            id = 0;
            var Value = oVirtualJoystick.PlugInNext(ref id);            
            var NTSTATUS_Name = NTSTATUS.Get.NameByValue(Value);
            var NTSTATUS_Value = NTSTATUS.Get.ValueByName(NTSTATUS_Name);
            var NTSTATUS_description = NTSTATUS.Get.Description(NTSTATUS_Name);
                NTSTATUS_description = NTSTATUS.Get.Description(Value);
            Console.WriteLine(NTSTATUS.Description.STATUS_UNSUCCESSFUL);

            var Severity = NTSTATUS.Get.Severity.Status(Value);
            
            
            oVirtualJoystick.isControllerOwned(id, ref findid);
            if (findid != true)
                WriteToEventLog("Failed to acquire vJoy device");
            else
            {
                oVirtualJoystick.GetLedNumber(id, ref Led);
                WriteToEventLog("Acquired :: vXbox ID : " + id.ToString() + "\n" + "\tLED number : " + Led.ToString());
            }

            Console.ReadKey();
            Environment.Exit(1);
        }
        static void DetectVirtualJoysticks(List<vJoystickInfo> oAllVJoystickInfo, List<uint> oVirtualJoysticksToUse)
        {
            // Create one joystick object and a position structure.
            oVirtualJoystick = new vGen();

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!oVirtualJoystick.vJoyEnabled())
            {
                WriteToEventLog($"\nvJoy driver not enabled: Failed Getting vJoy attributes.");
                return;
            }
            else
                WriteToEventLog($"\nProduct: {oVirtualJoystick.GetvJoyProductString()}\nVendor: {oVirtualJoystick.GetvJoyManufacturerString()}\nVersion Number: {oVirtualJoystick.GetvJoySerialNumberString()}");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = oVirtualJoystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                WriteToEventLog($"Version of Driver Matches DLL Version ({DllVer:X})");
            else
                WriteToEventLog($"Version of Driver ({DrvVer:X}) does NOT match DLL Version ({DllVer:X})");

            WriteToEventLog();

            foreach (uint id in oVirtualJoysticksToUse)
            {
                vJoystickInfo oNewVJoystickInfo = new vJoystickInfo();
                oNewVJoystickInfo.id = id;

                // Get the state of the requested device
                VjdStat status = oVirtualJoystick.GetVJDStatus(id);
                switch (status)
                {
                    case VjdStat.VJD_STAT_OWN:
                        WriteToEventLog($"vJoy Device {id} is already owned by this feeder");
                        break;
                    case VjdStat.VJD_STAT_FREE:
                        WriteToEventLog($"vJoy Device {id} is free");
                        break;
                    case VjdStat.VJD_STAT_BUSY:
                        WriteToEventLog($"vJoy Device {id} is already owned by another feeder\nCannot continue");
                        return;
                    case VjdStat.VJD_STAT_MISS:
                        WriteToEventLog($"vJoy Device {id} is not installed or disabled\nCannot continue");
                        return;
                    default:
                        WriteToEventLog($"vJoy Device {id} general error\nCannot continue");
                        return;
                };
                
                // Acquire the target
                if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!oVirtualJoystick.AcquireVJD(id))))
                {
                    WriteToEventLog($"Failed to acquire vJoy device number {id}.");
                    return;
                }
                else
                    WriteToEventLog($"Acquired: vJoy device number {id}.");

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

                // Print results
                WriteToEventLog($"\nvJoy Device {id} capabilities:\n");
                WriteToEventLog($"\tNumber of buttons:\t\t{nButtons}");
                WriteToEventLog($"\tNumber of Continuous POVs:\t{ContPovNumber}");
                WriteToEventLog($"\tNumber of Descrete POVs:\t{DiscPovNumber}");

                WriteToEventLog(String.Format("\tAxis X:\t\t\t\t{0}", AxisX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\tX Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinXVal, oNewVJoystickInfo.lMaxXVal));
                WriteToEventLog(String.Format("\tAxis Y:\t\t\t\t{0}", AxisX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\tY Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinYVal, oNewVJoystickInfo.lMaxYVal));
                WriteToEventLog(String.Format("\tAxis Z:\t\t\t\t{0}", AxisX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\tZ Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinZVal, oNewVJoystickInfo.lMaxZVal));

                WriteToEventLog(String.Format("\tAxis Rx:\t\t\t{0}", AxisRX ? "Yes" : "No"));
                WriteToEventLog(String.Format("\tRx Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRXVal, oNewVJoystickInfo.lMaxRXVal));
                WriteToEventLog(String.Format("\tAxis Ry:\t\t\t{0}", AxisRY ? "Yes" : "No"));
                WriteToEventLog(String.Format("\tRy Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRYVal, oNewVJoystickInfo.lMaxRYVal));
                WriteToEventLog(String.Format("\tAxis Rz:\t\t\t{0}", AxisRZ ? "Yes" : "No"));
                WriteToEventLog(String.Format("\tRz Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRZVal, oNewVJoystickInfo.lMaxRZVal));

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
                if (oDeviceInstance.InstanceName.Trim() != "vJoy Device")
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

                WriteToEventLog(String.Format("\nFound {0}", oDeviceInstance.InstanceName.Trim()));
                WriteToEventLog(String.Format("\tInstance_Name:\t\t\t{0}", oDeviceInstance.InstanceName.Trim()));
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

                oNewJoystickInfo.Map_To_Virtual_Ids = oThisJoystickConfig.Map_To_Virtual_Ids;

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
                            oNewJoystickInfo.PercentX = (oNewJoystickInfo.lMaxXVal - oNewJoystickInfo.lMinXVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_X = StringToHID_USAGES(oThisJoystickConfig.Map_X);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;
                        case "Y Axis:":
                            oNewJoystickInfo.lMinYVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxYVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentY = (oNewJoystickInfo.lMaxYVal - oNewJoystickInfo.lMinYVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_Y = StringToHID_USAGES(oThisJoystickConfig.Map_Y);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;
                        case "Z Axis:":
                            oNewJoystickInfo.lMinZVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxZVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentZ = (oNewJoystickInfo.lMaxZVal - oNewJoystickInfo.lMinZVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_Z = StringToHID_USAGES(oThisJoystickConfig.Map_Z);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;

                        case "X Rotation:":
                            oNewJoystickInfo.lMinRXVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRXVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentRX = (oNewJoystickInfo.lMaxRXVal - oNewJoystickInfo.lMinRXVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_RotationX = StringToHID_USAGES(oThisJoystickConfig.Map_RotationX);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            break;
                        case "Y Rotation:":
                            oNewJoystickInfo.lMinRYVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRYVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentRY = (oNewJoystickInfo.lMaxRYVal - oNewJoystickInfo.lMinRYVal) * oNewJoystickInfo.PercentageSlack;
                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));
                            oNewJoystickInfo.Map_RotationY = StringToHID_USAGES(oThisJoystickConfig.Map_RotationY);

                            break;
                        case "Z Rotation:":
                            oNewJoystickInfo.lMinRZVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRZVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentRZ = (oNewJoystickInfo.lMaxRZVal - oNewJoystickInfo.lMinRZVal) * oNewJoystickInfo.PercentageSlack;
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
                                    oNewJoystickInfo.Map_Buttons[iButtonNumber] = oThisJoystickConfig.Map_Buttons[iButtonNumber];
                                }

                                WriteToEventLog(String.Format("\t{0}\tYes", oJoystickObject.Name.PadRight(25)));
                            }
                            else
                            {
                                WriteToEventLog(String.Format("\t{0}\tAvailable, Not supported", oJoystickObject.Name.PadRight(25)));
                            }
                            break;
                    }
                }

                oAllJoystickInfo.Add(oNewJoystickInfo);
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
                case "RotationX":
                    return JoystickOffset.RotationX;
                case "RotationY":
                    return JoystickOffset.RotationY;
                case "RotationZ":
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
                case "RotationX":
                    return HID_USAGES.HID_USAGE_RX;
                case "RotationY":
                    return HID_USAGES.HID_USAGE_RY;
                case "RotationZ":
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
            if (oAllVJoystickInfo.Count == 0 || oAllJoystickInfo.Count == 0) return;

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
                                WriteToEventLog(String.Format("\tRemoving {0}: {1} - {2}", oJoystickInfo.oDeviceInstance.InstanceName, oException.Message, oAcquireException.Message));
                                oAllJoystickInfo.Remove(oJoystickInfo);
                            }
                        }

#if DEBUG
                        WriteToEventLog(String.Format("\tException ({0}): {1}", oJoystickInfo.ErrorCount, oException.Message));
#endif
                    }

                    if (oBufferedData != null)
                    {
                        foreach (uint iVJoystickId in oJoystickInfo.Map_To_Virtual_Ids)
                        {
                            var oThisVJoystickInfo = oAllVJoystickInfo.Find(x => x.id == iVJoystickId);
                            if (oThisVJoystickInfo == null) continue;

                            foreach (var oState in oBufferedData)
                            {
                                int iNormalizedValue;

                                switch (oState.Offset)
                                {
                                    case SharpDX.DirectInput.JoystickOffset.X:
                                        if (Math.Abs(oJoystickInfo.lPreviousX - oState.Value) > oJoystickInfo.PercentX)
                                        {
                                            oJoystickInfo.lPreviousX = oState.Value;
                                            iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinXVal, oJoystickInfo.lMaxXVal, oThisVJoystickInfo.lMinXVal, oThisVJoystickInfo.lMaxXVal);
                                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_X);
#if DEBUG
                                            WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                            WriteToEventLog(String.Format("\t\tNormalized to: {0}", iNormalizedValue));
#endif
                                        }
                                        break;
                                    case SharpDX.DirectInput.JoystickOffset.Y:
                                        if (Math.Abs(oJoystickInfo.lPreviousY - oState.Value) > oJoystickInfo.PercentY)
                                        {
                                            oJoystickInfo.lPreviousY = oState.Value;
                                            iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinYVal, oJoystickInfo.lMaxYVal, oThisVJoystickInfo.lMinYVal, oThisVJoystickInfo.lMaxYVal);
                                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_Y);
#if DEBUG
                                            WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                            WriteToEventLog(String.Format("\t\tNormalized to: {0}", iNormalizedValue));
#endif
                                        }
                                        break;
                                    case SharpDX.DirectInput.JoystickOffset.Z:
                                        if (Math.Abs(oJoystickInfo.lPreviousZ - oState.Value) > oJoystickInfo.PercentZ)
                                        {
                                            oJoystickInfo.lPreviousZ = oState.Value;
                                            iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinZVal, oJoystickInfo.lMaxZVal, oThisVJoystickInfo.lMinZVal, oThisVJoystickInfo.lMaxZVal);
                                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_Z);
#if DEBUG
                                            WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                            WriteToEventLog(String.Format("\t\tNormalized to: {0}", iNormalizedValue));
#endif
                                        }
                                        break;

                                    case SharpDX.DirectInput.JoystickOffset.RotationX:
                                        if (Math.Abs(oJoystickInfo.PreviousRX - oState.Value) > oJoystickInfo.PercentRX)
                                        {
                                            oJoystickInfo.PreviousRX = oState.Value;
                                            iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRXVal, oJoystickInfo.lMaxRXVal, oThisVJoystickInfo.lMinRXVal, oThisVJoystickInfo.lMaxRXVal);
                                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationX);
#if DEBUG
                                            WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                            WriteToEventLog(String.Format("\t\tNormalized to: {0}", iNormalizedValue));
#endif
                                        }
                                        break;
                                    case SharpDX.DirectInput.JoystickOffset.RotationY:
                                        if (Math.Abs(oJoystickInfo.PreviousRY - oState.Value) > oJoystickInfo.PercentRY)
                                        {
                                            oJoystickInfo.PreviousRY = oState.Value;
                                            iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRYVal, oJoystickInfo.lMaxRYVal, oThisVJoystickInfo.lMinRYVal, oThisVJoystickInfo.lMaxRYVal);
                                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationY);
#if DEBUG
                                            WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                            WriteToEventLog(String.Format("\t\tNormalized to: {0}", iNormalizedValue));
#endif
                                        }
                                        break;
                                    case SharpDX.DirectInput.JoystickOffset.RotationZ:
                                        if (Math.Abs(oJoystickInfo.PreviousRZ - oState.Value) > oJoystickInfo.PercentRZ)
                                        {
                                            oJoystickInfo.PreviousRZ = oState.Value;
                                            iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRZVal, oJoystickInfo.lMaxRZVal, oThisVJoystickInfo.lMinRZVal, oThisVJoystickInfo.lMaxRZVal);
                                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationZ);
#if DEBUG
                                            WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                            WriteToEventLog(String.Format("\t\tNormalized to: {0}", iNormalizedValue));
#endif
                                        }
                                        break;

                                    case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers0:
                                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers0);
#if DEBUG
                                        WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                        WriteToEventLog(String.Format("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers0));
#endif
                                        break;
                                    case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers1:
                                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers1);
#if DEBUG
                                        WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                        WriteToEventLog(String.Format("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers1));
#endif
                                        break;
                                    case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers2:
                                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers2);
#if DEBUG
                                        WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                        WriteToEventLog(String.Format("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers2));
#endif
                                        break;
                                    case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers3:
                                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers3);
#if DEBUG
                                        WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                                        WriteToEventLog(String.Format("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers3));
#endif
                                        break;

                                    default:
#if DEBUG
                                        WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
#endif
                                        if (oState.Offset.ToString().Contains("Buttons"))
                                        {
                                            string sButtonNumber = System.Text.RegularExpressions.Regex.Match(oState.Offset.ToString(), @"\d+$").Value;
                                            if (int.TryParse(sButtonNumber, out int iButtonNumber))
                                            {
                                                oVirtualJoystick.SetBtn(oState.Value != 0, iVJoystickId, (1 + oJoystickInfo.Map_Buttons[iButtonNumber]));
#if DEBUG
                                                WriteToEventLog(String.Format("\t\tMapped to Button {0}", oJoystickInfo.Map_Buttons[iButtonNumber]));
#endif
                                            }
                                        }
#if DEBUG
                                        else
                                        {
                                            WriteToEventLog("\t\tNot supported");
                                        }
#endif

                                        break;
                                }
                            }
                        }
                    }
                }

                Thread.Sleep(iSleepTime);
            }
        }

        static int NormalizeRange(int num, long fromMin, long fromMax, long toMin, long toMax)
        {
            // TODO: Cache these ranges for each device
            long lFromRange = fromMax - fromMin;
            long lToRange = toMax - toMin;

            // TODO: Combine calculations to one line once caching is done
            long lNumerater = num - fromMin;
            decimal lReturn = toMin + lNumerater / (decimal)lFromRange * lToRange;

            return (int)Math.Floor(lReturn);
        }

        public static void WriteToEventLog(string sMessage = "", int iVerbosity = Verbosity.Information)
            
        {
            if (goOptions.Verbosity < iVerbosity) return;

            //sbgMessageLog.AppendLine(sMessage);

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

    } // class Program
} // namespace MTOvJoyFeeder
