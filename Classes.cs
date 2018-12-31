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
}
