﻿using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace MTOvJoyFeeder
{
    public class Config
    {
        public static List<JoystickConfig> ReadConfigFile()
        {
            List<JoystickConfig> oAllJoystickInfo = null;
            
            if (File.Exists(Program.goOptions.ControllerConfig))
            {
                string sAllJoystickInfo = File.ReadAllText(Program.goOptions.ControllerConfig);

                oAllJoystickInfo = JsonConvert.DeserializeObject<List<JoystickConfig>>(sAllJoystickInfo);
            }

            return oAllJoystickInfo;            
        }

        public static List<JoystickConfig> CreateConfigFile(List<JoystickInfo> oAllJoystickInfo)
        {
            List<JoystickConfig> oJoystickConfig = new List<JoystickConfig>();

            foreach (var oJoystickInfo in oAllJoystickInfo)
            {
                JoystickConfig oConfigSetting = new JoystickConfig();

                oConfigSetting.Instance_Name = oJoystickInfo.oDeviceInstance.InstanceName.Trim();
                oConfigSetting.Instance_Type = oJoystickInfo.oDeviceInstance.Type.ToString();
                oConfigSetting.Instance_GUID = oJoystickInfo.oDeviceInstance.InstanceGuid;
                oConfigSetting.Product_GUID = oJoystickInfo.oDeviceInstance.ProductGuid;
                oConfigSetting.Map_Buttons = new uint[oJoystickInfo.oJoystick.Capabilities.ButtonCount];

                for (uint i = 0; i < oJoystickInfo.oJoystick.Capabilities.ButtonCount; i++)
                {
                    oConfigSetting.Map_Buttons[i] = i;
                }
                
                oJoystickConfig.Add(oConfigSetting);
            }

            WriteConfigFile(oJoystickConfig);

            return oJoystickConfig;
        }

        private static void WriteConfigFile(List<JoystickConfig> oJoystickConfig)
        {
            File.WriteAllText(Program.goOptions.ControllerConfig, Regex.Replace(JsonConvert.SerializeObject(oJoystickConfig, Formatting.Indented), @"(\s+(\d))|((?<=\d)\s+([\]]))", "$2$4", RegexOptions.Multiline));            
        }

    }
}
