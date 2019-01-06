using System;
using System.Collections.Generic;
using System.Threading;
using CommandLine;
using SharpDX.DirectInput;
using vJoyInterfaceWrap;

namespace MTOvJoyFeeder
{
    class Program
    {
        public const int giVerbosity_Debug = 5;       // Debug (5): Super detail
        public const int giVerbosity_Verbose = 4;     // Verbose (4): Almost everything
        public const int giVerbosity_Information = 3; // Information (3): Information that might be useful to user
        public const int giVerbosity_Warning = 2;     // Warning (2): Bad things that happen, but are expected
        public const int giVerbosity_Error = 1;       // Error (1): Errors that are recoverable
        public const int giVerbosity_Critical = 0;    // Critical (0): Errors that stop execution

        //static public vJoy.JoystickState iReport;
        static public vJoy oVirtualJoystick;
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

            List<vJoystickInfo> oAllVJoystickInfo = new List<vJoystickInfo>();
            DetectVirtualJoysticks(oAllVJoystickInfo);

            if (oJoystickConfig == null)
            {
                oJoystickConfig = Config.CreateConfigFile(oAllJoystickInfo);
            }

            PollJoysticks(oAllVJoystickInfo, oAllJoystickInfo);
        }

        static void DetectVirtualJoysticks(List<vJoystickInfo> oAllVJoystickInfo)
        {
            vJoystickInfo oNewVJoystickInfo = new vJoystickInfo();

            // Create one joystick object and a position structure.
            oVirtualJoystick = new vJoy();

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!oVirtualJoystick.vJoyEnabled())
            {
                Console.WriteLine("\nvJoy driver not enabled: Failed Getting vJoy attributes.");
                return;
            }
            else
                Console.WriteLine("\nProduct: {0}\nVendor: {1}\nVersion Number: {2}", oVirtualJoystick.GetvJoyProductString(), oVirtualJoystick.GetvJoyManufacturerString(), oVirtualJoystick.GetvJoySerialNumberString());

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = oVirtualJoystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                Console.WriteLine("Version of Driver Matches DLL Version ({0:X})", DllVer);
            else
                Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})", DrvVer, DllVer);

            Console.WriteLine();

            uint id = 1;
            oNewVJoystickInfo.id = id;

            // Get the state of the requested device
            VjdStat status = oVirtualJoystick.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue", id);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue", id);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue", id);
                    return;
            };

            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!oVirtualJoystick.AcquireVJD(id))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.", id);
                return;
            }
            else
                Console.WriteLine("Acquired: vJoy device number {0}.", id);

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

            // Get the number of buttons and POV Hat switchessupported by this vJoy device
            int nButtons = oVirtualJoystick.GetVJDButtonNumber(id);
            int ContPovNumber = oVirtualJoystick.GetVJDContPovNumber(id);
            int DiscPovNumber = oVirtualJoystick.GetVJDDiscPovNumber(id);

            // Print results
            Console.WriteLine("\nvJoy Device {0} capabilities:\n", id);
            Console.WriteLine("\tNumber of buttons:\t\t{0}", nButtons);
            Console.WriteLine("\tNumber of Continuous POVs:\t{0}", ContPovNumber);
            Console.WriteLine("\tNumber of Descrete POVs:\t{0}", DiscPovNumber);

            Console.WriteLine("\tAxis X:\t\t\t\t{0}", AxisX ? "Yes" : "No");
            Console.WriteLine("\tX Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinXVal, oNewVJoystickInfo.lMaxXVal);
            Console.WriteLine("\tAxis Y:\t\t\t\t{0}", AxisX ? "Yes" : "No");
            Console.WriteLine("\tY Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinYVal, oNewVJoystickInfo.lMaxYVal);
            Console.WriteLine("\tAxis Z:\t\t\t\t{0}", AxisX ? "Yes" : "No");
            Console.WriteLine("\tZ Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinZVal, oNewVJoystickInfo.lMaxZVal);

            Console.WriteLine("\tAxis Rx:\t\t\t{0}", AxisRX ? "Yes" : "No");
            Console.WriteLine("\tRx Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRXVal, oNewVJoystickInfo.lMaxRXVal);
            Console.WriteLine("\tAxis Ry:\t\t\t{0}", AxisRY ? "Yes" : "No");
            Console.WriteLine("\tRy Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRYVal, oNewVJoystickInfo.lMaxRYVal);
            Console.WriteLine("\tAxis Rz:\t\t\t{0}", AxisRZ ? "Yes" : "No");
            Console.WriteLine("\tRz Axis Range:\t\t\t{0}, {1}", oNewVJoystickInfo.lMinRZVal, oNewVJoystickInfo.lMaxRZVal);

            Console.WriteLine();

            oAllVJoystickInfo.Add(oNewVJoystickInfo);
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
                Console.WriteLine("No joystick/Gamepad found.");
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

                Console.WriteLine("\nFound {0}", oDeviceInstance.InstanceName.Trim());
                Console.WriteLine("\tInstance_Name:\t\t\t{0}", oDeviceInstance.InstanceName.Trim());
                Console.WriteLine("\tInstance_Type:\t\t\t{0}", oDeviceInstance.Type);
                Console.WriteLine("\tInstance_GUID:\t\t\t{0}", oDeviceInstance.InstanceGuid);
                Console.WriteLine("\tProduct_GUID:\t\t\t{0}", oDeviceInstance.ProductGuid);
                
                var oNewJoystickInfo = new JoystickInfo();

                oNewJoystickInfo.oDeviceInstance = oDeviceInstance;

                // Instantiate the joystick
                oNewJoystickInfo.oJoystick = new Joystick(directInput, oDeviceInstance.ProductGuid);

                // Query all suported ForceFeedback effects
                oNewJoystickInfo.oEffectInfo = oNewJoystickInfo.oJoystick.GetEffects();
                foreach (var oEffect in oNewJoystickInfo.oEffectInfo)
                    Console.WriteLine("Force Feedback Effect available:\t{0}", oEffect.Name);

                // Set BufferSize in order to use buffered data.
                oNewJoystickInfo.oJoystick.Properties.BufferSize = 128;

                // Acquire the joystick
                oNewJoystickInfo.oJoystick.Acquire();

                Console.WriteLine("\tNumber of buttons:\t\t{0}", oNewJoystickInfo.oJoystick.Capabilities.ButtonCount);
                Console.WriteLine("\tNumber of POVs:\t\t\t{0}", oNewJoystickInfo.oJoystick.Capabilities.PovCount);

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

                            Console.WriteLine("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum);
                            break;
                        case "Y Axis:":
                            oNewJoystickInfo.lMinYVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxYVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentY = (oNewJoystickInfo.lMaxYVal - oNewJoystickInfo.lMinYVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_Y = StringToHID_USAGES(oThisJoystickConfig.Map_Y);

                            Console.WriteLine("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum);
                            break;
                        case "Z Axis:":
                            oNewJoystickInfo.lMinZVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxZVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentZ = (oNewJoystickInfo.lMaxZVal - oNewJoystickInfo.lMinZVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_Z = StringToHID_USAGES(oThisJoystickConfig.Map_Z);

                            Console.WriteLine("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum);
                            break;

                        case "X Rotation:":
                            oNewJoystickInfo.lMinRXVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRXVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentRX = (oNewJoystickInfo.lMaxRXVal - oNewJoystickInfo.lMinRXVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_RotationX = StringToHID_USAGES(oThisJoystickConfig.Map_RotationX);

                            Console.WriteLine("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum);
                            break;
                        case "Y Rotation:":
                            oNewJoystickInfo.lMinRYVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRYVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentRY = (oNewJoystickInfo.lMaxRYVal - oNewJoystickInfo.lMinRYVal) * oNewJoystickInfo.PercentageSlack;
                            Console.WriteLine("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum);
                            oNewJoystickInfo.Map_RotationY = StringToHID_USAGES(oThisJoystickConfig.Map_RotationY);

                            break;
                        case "Z Rotation:":
                            oNewJoystickInfo.lMinRZVal = oObjectProperties.Range.Minimum;
                            oNewJoystickInfo.lMaxRZVal = oObjectProperties.Range.Maximum;
                            oNewJoystickInfo.PercentRZ = (oNewJoystickInfo.lMaxRZVal - oNewJoystickInfo.lMinRZVal) * oNewJoystickInfo.PercentageSlack;
                            oNewJoystickInfo.Map_RotationZ = StringToHID_USAGES(oThisJoystickConfig.Map_RotationZ);

                            Console.WriteLine("\t{0}\tYes ({1}, {2})", oJoystickObject.Name, oObjectProperties.Range.Minimum, oObjectProperties.Range.Maximum);
                            break;
                        case "PointOfViewControllers0:":
                            oNewJoystickInfo.Map_PointOfViewControllers0 = oThisJoystickConfig.Map_PointOfViewControllers0;

                            Console.WriteLine("\t{0}\tYes", oJoystickObject.Name);
                            break;

                        default:
                            if (oJoystickObject.Name.Contains("Button "))
                            {
                                string sButtonNumber = System.Text.RegularExpressions.Regex.Match(oJoystickObject.Name, @"Button (\d+):").Groups[1].Value;
                                if (uint.TryParse(sButtonNumber, out uint iButtonNumber))
                                {
                                    oNewJoystickInfo.Map_Buttons[iButtonNumber] = oThisJoystickConfig.Map_Buttons[iButtonNumber];
                                }

                                Console.WriteLine("\t{0}\tYes", oJoystickObject.Name.PadRight(25));
                            }
                            else
                            {
                                Console.WriteLine("\t{0}\tAvailable, Not supported", oJoystickObject.Name.PadRight(25));
                            }
                            break;
                    }
                }

                oAllJoystickInfo.Add(oNewJoystickInfo);
            }
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
            Console.WriteLine("\nShowing Events:\n");

            // Poll events from joystick
            while (true)
            {
                foreach (var oJoystickInfo in oAllJoystickInfo)
                {
                    //oJoystickInfo.oJoystick.SetNotification(foo);
                    oJoystickInfo.oJoystick.Poll();
                    var oBufferedData = oJoystickInfo.oJoystick.GetBufferedData();

                    if (oBufferedData != null)
                    {
                        var oThisVJoystickInfo = oAllVJoystickInfo.Find(x => x.id == oJoystickInfo.id);
                        foreach (var oState in oBufferedData)
                        {
                            int iNormalizedValue;

                            switch (oState.Offset)
                            {
                                case SharpDX.DirectInput.JoystickOffset.X:
                                    if (Math.Abs(oJoystickInfo.lPreviousX - oState.Value) > oJoystickInfo.PercentX)
                                    {
                                        Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                        oJoystickInfo.lPreviousX = oState.Value;
                                        iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinXVal, oJoystickInfo.lMaxXVal, oThisVJoystickInfo.lMinXVal, oThisVJoystickInfo.lMaxXVal);
                                        Console.WriteLine("\t\tNormalized to: {0}", iNormalizedValue);
                                        oVirtualJoystick.SetAxis(iNormalizedValue, oJoystickInfo.id, oJoystickInfo.Map_X);
                                    }
                                    break;
                                case SharpDX.DirectInput.JoystickOffset.Y:
                                    if (Math.Abs(oJoystickInfo.lPreviousY - oState.Value) > oJoystickInfo.PercentY)
                                    {
                                        Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                        oJoystickInfo.lPreviousY = oState.Value;
                                        iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinYVal, oJoystickInfo.lMaxYVal, oThisVJoystickInfo.lMinYVal, oThisVJoystickInfo.lMaxYVal);
                                        Console.WriteLine("\t\tNormalized to: {0}", iNormalizedValue);
                                        oVirtualJoystick.SetAxis(iNormalizedValue, oJoystickInfo.id, oJoystickInfo.Map_Y);
                                    }
                                    break;
                                case SharpDX.DirectInput.JoystickOffset.Z:
                                    if (Math.Abs(oJoystickInfo.lPreviousZ - oState.Value) > oJoystickInfo.PercentZ)
                                    {
                                        Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                        oJoystickInfo.lPreviousZ = oState.Value;
                                        iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinZVal, oJoystickInfo.lMaxZVal, oThisVJoystickInfo.lMinZVal, oThisVJoystickInfo.lMaxZVal);
                                        Console.WriteLine("\t\tNormalized to: {0}", iNormalizedValue);
                                        oVirtualJoystick.SetAxis(iNormalizedValue, oJoystickInfo.id, oJoystickInfo.Map_Z);
                                    }
                                    break;

                                case SharpDX.DirectInput.JoystickOffset.RotationX:
                                    if (Math.Abs(oJoystickInfo.PreviousRX - oState.Value) > oJoystickInfo.PercentRX)
                                    {
                                        Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                        oJoystickInfo.PreviousRX = oState.Value;
                                        iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRXVal, oJoystickInfo.lMaxRXVal, oThisVJoystickInfo.lMinRXVal, oThisVJoystickInfo.lMaxRXVal);
                                        Console.WriteLine("\t\tNormalized to: {0}", iNormalizedValue);
                                        oVirtualJoystick.SetAxis(iNormalizedValue, oJoystickInfo.id, oJoystickInfo.Map_RotationX);
                                    }
                                    break;
                                case SharpDX.DirectInput.JoystickOffset.RotationY:
                                    if (Math.Abs(oJoystickInfo.PreviousRY - oState.Value) > oJoystickInfo.PercentRY)
                                    {
                                        Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                        oJoystickInfo.PreviousRY = oState.Value;
                                        iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRYVal, oJoystickInfo.lMaxRYVal, oThisVJoystickInfo.lMinRYVal, oThisVJoystickInfo.lMaxRYVal);
                                        Console.WriteLine("\t\tNormalized to: {0}", iNormalizedValue);
                                        oVirtualJoystick.SetAxis(iNormalizedValue, oJoystickInfo.id, oJoystickInfo.Map_RotationY);
                                    }
                                    break;
                                case SharpDX.DirectInput.JoystickOffset.RotationZ:
                                    if (Math.Abs(oJoystickInfo.PreviousRZ - oState.Value) > oJoystickInfo.PercentRZ)
                                    {
                                        Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                        oJoystickInfo.PreviousRZ = oState.Value;
                                        iNormalizedValue = NormalizeRange(oState.Value, oJoystickInfo.lMinRZVal, oJoystickInfo.lMaxRZVal, oThisVJoystickInfo.lMinRZVal, oThisVJoystickInfo.lMaxRZVal);
                                        Console.WriteLine("\t\tNormalized to: {0}", iNormalizedValue);
                                        oVirtualJoystick.SetAxis(iNormalizedValue, oJoystickInfo.id, oJoystickInfo.Map_RotationZ);
                                    }
                                    break;

                                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers0:
                                    Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                    Console.WriteLine("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers0);
                                    oVirtualJoystick.SetContPov(oState.Value, oJoystickInfo.id, oJoystickInfo.Map_PointOfViewControllers0);
                                    break;
                                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers1:
                                    Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                    Console.WriteLine("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers1);
                                    oVirtualJoystick.SetContPov(oState.Value, oJoystickInfo.id, oJoystickInfo.Map_PointOfViewControllers1);
                                    break;
                                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers2:
                                    Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                    Console.WriteLine("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers2);
                                    oVirtualJoystick.SetContPov(oState.Value, oJoystickInfo.id, oJoystickInfo.Map_PointOfViewControllers2);
                                    break;
                                case SharpDX.DirectInput.JoystickOffset.PointOfViewControllers3:
                                    Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);
                                    Console.WriteLine("\t\tMapped to PointOfViewControllers{0}", oJoystickInfo.Map_PointOfViewControllers3);
                                    oVirtualJoystick.SetContPov(oState.Value, oJoystickInfo.id, oJoystickInfo.Map_PointOfViewControllers3);
                                    break;

                                default:
                                    Console.WriteLine("\t{0} - {1}", oJoystickInfo.oDeviceInstance.InstanceName.Trim(), oState);

                                    if (oState.Offset.ToString().Contains("Buttons"))
                                    {
                                        string sButtonNumber = System.Text.RegularExpressions.Regex.Match(oState.Offset.ToString(), @"\d+$").Value;
                                        if (int.TryParse(sButtonNumber, out int iButtonNumber))
                                        {
                                            oVirtualJoystick.SetBtn(oState.Value != 0, oJoystickInfo.id, (1 + oJoystickInfo.Map_Buttons[iButtonNumber]));
                                            Console.WriteLine("\t\tMapped to Button {0}", oJoystickInfo.Map_Buttons[iButtonNumber]);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("\t\tNot supported");
                                    }
                                    break;
                            }
                        }
                    }
                }

                Thread.Sleep(100);
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

    } // class Program
} // namespace MTOvJoyFeeder
