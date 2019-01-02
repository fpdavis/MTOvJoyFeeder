﻿using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;

namespace MTOvJoyFeeder
{
    public class Config
    {
        public static List<JoystickConfig> ReadConfigFile()
        {
            List<JoystickConfig> oAllJoystickInfo = null;

            string sCurrentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            sCurrentPath += "/MTOvJoyFeeder.json";

            if (File.Exists(sCurrentPath))
            {
                string sAllJoystickInfo = File.ReadAllText(sCurrentPath);

                oAllJoystickInfo = (List<JoystickConfig>)JsonConvert.DeserializeObject(sAllJoystickInfo);
            }

            return oAllJoystickInfo;            
        }

        public static List<JoystickConfig> CreateConfigFile(List<JoystickInfo> oAllJoystickInfo)
        {
            List<JoystickConfig> oJoystickConfig = new List<JoystickConfig>();

            foreach (var oJoystickInfo in oAllJoystickInfo)
            {
                JoystickConfig oConfigSetting = new JoystickConfig();

                oConfigSetting.Joystick_Name = oJoystickInfo.oDeviceInstance.InstanceName.Trim();
                oConfigSetting.Joystick_Type = oJoystickInfo.oDeviceInstance.Type.ToString();
                oConfigSetting.Joystick_GUID = oJoystickInfo.oDeviceInstance.InstanceGuid;

                oJoystickConfig.Add(oConfigSetting);
            }

            WriteConfigFile(oJoystickConfig);

            return oJoystickConfig;
        }

        private static void WriteConfigFile(List<JoystickConfig> oJoystickConfig)
        {
            string sCurrentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            sCurrentPath += "\\MTOvJoyFeeder.json";

            File.WriteAllText(sCurrentPath, JsonConvert.SerializeObject(oJoystickConfig));
        }

    }
}