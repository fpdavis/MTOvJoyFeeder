﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using CommandLine;
using SharpDX.DirectInput;
using vGenInterfaceWrap;
using vXboxInterfaceWrap;
using System.Linq;
using System.Runtime.InteropServices;

namespace MTOvJoyFeeder
{
    class Program
    {
        //private static StringBuilder sbgMessageLog = new StringBuilder();

        //static public vJoy.JoystickState iReport;

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
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(x => { Run(x); })
                .WithNotParsed((errs) => { return; });

        } // Main

        static void Run(Options oOptions)
        {
            goOptions = oOptions;

            oEventHandler += new EventHandler(OnExit);
            SetConsoleCtrlHandler(oEventHandler, true);

            ReleasevXboxJoysticks();

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
                ReleasevXboxJoysticks();
            }
        }

        static void CreatevXboxJoysticks(List<vJoystickInfo> oAllVJoystickInfo, List<uint> oVirtualJoysticksToUse)
        {
            byte Led = 0xFF;

            if (vXboxInterface.isVBusExists())
            {
                WriteToEventLog("vBus found...");
            }
            else
            {
                WriteToEventLog("vBus for xBox controller not enabled...");
                return;
            }

            byte nSlots = 0xFF;
            vXboxInterface.GetNumEmptyBusSlots(ref nSlots);

            WriteToEventLog($"{nSlots} Empty Controller slots found.");

            // Check for avaiable devices
            foreach (uint iSlot in oVirtualJoysticksToUse)
            {
                bool bSlotInUse = false;

                bSlotInUse = vXboxInterface.isControllerExists(iSlot);
                WriteToEventLog("Device ID : " + iSlot + " ( " + bSlotInUse.ToString() + " )");
                if (bSlotInUse && !vXboxInterface.isControllerOwned(iSlot))
                {
                    vXboxInterface.UnPlugForce(iSlot);
                }

                vXboxInterface.PlugIn(iSlot);
                bSlotInUse = vXboxInterface.isControllerOwned(iSlot);

                if (bSlotInUse != true)
                {
                    WriteToEventLog("Failed to acquire vXBox device");
                    return;
                }
                else
                {
                    vXboxInterface.GetLedNumber(iSlot, ref Led);
                    WriteToEventLog("Acquired :: vXbox ID : " + iSlot.ToString() + "\n" + "\tLED number : " + Led.ToString());

                    vJoystickInfo oNewVJoystickInfo = new vJoystickInfo();
                    oNewVJoystickInfo.id = iSlot;
                    oNewVJoystickInfo.ControlerType = DevType.vXbox;

                    oNewVJoystickInfo.lMinXVal = Range.MinXVal;
                    oNewVJoystickInfo.lMaxXVal = Range.MaxXVal;
                    oNewVJoystickInfo.lMinYVal = Range.MinYVal;
                    oNewVJoystickInfo.lMaxYVal = Range.MaxYVal;
                    oNewVJoystickInfo.lMinZVal = Range.MinZVal;
                    oNewVJoystickInfo.lMaxZVal = Range.MaxZVal;

                    oNewVJoystickInfo.lMinRXVal = Range.MinRXVal;
                    oNewVJoystickInfo.lMaxRXVal = Range.MaxRXVal;
                    oNewVJoystickInfo.lMinRYVal = Range.MinRYVal;
                    oNewVJoystickInfo.lMaxRYVal = Range.MaxRYVal;

                    oAllVJoystickInfo.Add(oNewVJoystickInfo);
                }
            }
        }
        static void ReleasevXboxJoysticks()
        {
            vXboxInterface.UnPlugForce(1);
            vXboxInterface.UnPlugForce(2);
            vXboxInterface.UnPlugForce(3);
            vXboxInterface.UnPlugForce(4);
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
                oNewVJoystickInfo.ControlerType = DevType.vJoy;

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
                            oNewJoystickInfo.Map_RotationY = StringToHID_USAGES(oThisJoystickConfig.Map_RotationY);

                            WriteToEventLog(String.Format("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum));

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
#if DEBUG
                             //   WriteToEventLog($"\t\tGetVibration: {oThisVJoystickInfo.pVib.wLeftMotorSpeed}, {oThisVJoystickInfo.pVib.wRightMotorSpeed}");
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
            int iNormalizedValue;

            switch (oState.Offset)
            {
                case SharpDX.DirectInput.JoystickOffset.X:
                    if (Math.Abs(oJoystickInfo.lPreviousX - oState.Value) > oJoystickInfo.PercentX)
                    {
                        oJoystickInfo.lPreviousX = oState.Value;
                        iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinXVal, oJoystickInfo.lMaxXVal, oThisVJoystickInfo.lMinXVal, oThisVJoystickInfo.lMaxXVal);

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_X);
                        }
                        else
                        {
                            vXboxInterface.SetAxis(iVJoystickId, (short)iNormalizedValue, oJoystickInfo.Map_X);
                        }
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

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_Y);
                        }
                        else
                        {
                            vXboxInterface.SetAxis(iVJoystickId, (short)iNormalizedValue, oJoystickInfo.Map_Y);
                        }
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

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_Z);
                        }
                        else
                        {
                            vXboxInterface.SetZAxis(iVJoystickId, (short)iNormalizedValue);
                        }
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

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationX);
                        }
                        else
                        {
                            vXboxInterface.SetAxis(iVJoystickId, (short)iNormalizedValue, oJoystickInfo.Map_RotationX);
                        }
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

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationY);
                        }
                        else
                        {
                            vXboxInterface.SetAxis(iVJoystickId, (short)iNormalizedValue, oJoystickInfo.Map_RotationY);
                        }
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

                        if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                        {
                            oVirtualJoystick.SetAxis(iNormalizedValue, iVJoystickId, oJoystickInfo.Map_RotationZ);
                        }
                        else
                        {
                            WriteToEventLog("\t\tNot supported");
                        }
#if DEBUG
                        WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                        WriteToEventLog(String.Format("\t\tNormalized to: {0}", iNormalizedValue));
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
#if DEBUG
                    WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                    WriteToEventLog(String.Format("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers0));
#endif
                    break;
                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers1:
                    if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                    {
                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers1);
                    }
                    else
                    {
                        WriteToEventLog("\t\tNot supported");
                    }
#if DEBUG
                    WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                    WriteToEventLog(String.Format("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers1));
#endif
                    break;
                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers2:
                    if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                    {
                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers2);
                    }
                    else
                    {
                        WriteToEventLog("\t\tNot supported");
                    }
#if DEBUG
                    WriteToEventLog(String.Format("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState));
                    WriteToEventLog(String.Format("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers2));
#endif
                    break;
                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers3:
                    if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                    {
                        oVirtualJoystick.SetContPov(oState.Value, iVJoystickId, oJoystickInfo.Map_PointOfViewControllers3);
                    }
                    else
                    {
                        WriteToEventLog("\t\tNot supported");
                    }
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
                            if (oThisVJoystickInfo.ControlerType == DevType.vJoy)
                            {
                                oVirtualJoystick.SetBtn(oState.Value != 0, iVJoystickId, (1 + oJoystickInfo.Map_Buttons[iButtonNumber]));
                            }
                            else
                            {
                                vXboxInterface.SetBtn(oState.Value != 0, iVJoystickId, oJoystickInfo.Map_Buttons[iButtonNumber]);
                            }
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
                    return false;
            }
        }


    } // class Program
} // namespace MTOvJoyFeeder
