using System;
using System.Collections.Generic;
using SharpDX.DirectInput;

namespace MTOvJoyFeeder
{
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
    }

    public class JoystickConfig
    {
        public string Joystick_Name;
        public string Joystick_Type;
        public Guid Joystick_GUID;
        public int[] Map_To_Virtual_Ids = { 1 };
        
        public double Percentage_Slack = .01;

        public string Map_X = "X"; // SharpDX.DirectInput.JoystickOffset.X;
        public string Map_Y = "Y"; // SharpDX.DirectInput.JoystickOffset.Y;
        public string Map_Z = "Z"; // SharpDX.DirectInput.JoystickOffset.Z;

        public string Map_RotationX = "RotationX"; // SharpDX.DirectInput.JoystickOffset.RotationX;
        public string Map_RotationY = "RotationY"; // SharpDX.DirectInput.JoystickOffset.RotationY;
        public string Map_RotationZ = "RotationZ"; // SharpDX.DirectInput.JoystickOffset.RotationZ;

        public string Map_PointOfViewControllers0 = "PointOfViewControllers0"; // SharpDX.DirectInput.JoystickOffset.PointOfViewControllers0;

        public int[] Map_Buttons = { 0,1,2,3,4,5,6,7,8,9,10,11,12 }; // SharpDX.DirectInput.JoystickOffset.Buttons0;
    }
}
