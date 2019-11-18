using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace MTOvJoyFeeder
{
    public class AppConfig
    {
        public static string ConfigPath;

        public static void AssignConfig()
        {
            ConfigPath = AppDomain.CurrentDomain.BaseDirectory + Assembly.GetExecutingAssembly().GetName().Name + ".config";
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", ConfigPath);
            CreateConfig();
        }

        public static void CreateConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                System.Text.StringBuilder oStringBuilder = new StringBuilder();
                oStringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                oStringBuilder.AppendLine("<configuration>");


                var oParser = new CommandLine.Parser(with => with.HelpWriter = null);
                var oParserResult = oParser.ParseArguments<Options>("--help".Split());
                
                string sHelpText = HelpText.AutoBuild(oParserResult, HelpTextInstance =>
                {
                    HelpTextInstance.AdditionalNewLineAfterOption = false; //remove the extra newline between options
                    HelpTextInstance.Heading = "";
                    HelpTextInstance.Copyright = "";
                    HelpTextInstance.MaximumDisplayWidth = 50000;
                    HelpTextInstance.AddDashesToOption = false;
                    HelpTextInstance.AutoVersion = false;
                    HelpTextInstance.AutoHelp = false;
                    return HelpText.DefaultParsingErrorsHandler(oParserResult, HelpTextInstance);
                }, e => e);

                sHelpText = Regex.Replace(sHelpText, @"\n  ., ", "     ");
                sHelpText = Regex.Replace(sHelpText, @" \[-. ", " [Default: ");
                                
                oStringBuilder.AppendLine("     <!--" + sHelpText.Replace("<br/>", "\n".PadRight(37)) + "\n\n");
                oStringBuilder.AppendLine("     To override a Key/Value pair, remove the leading and trailing HTML style comment tags surrounding the line (<!–– ––>)\n     -->\n");

                oParserResult = oParser.ParseArguments<Options>("".Split());
                oParserResult.WithParsed<Options>(options => CreateConfigSection(options, ref oStringBuilder));

                oStringBuilder.AppendLine("</configuration>");
                var oOptions = new Options();
                
                System.IO.File.WriteAllText(ConfigPath, oStringBuilder.ToString());
            }
        }
        
        public static StringBuilder CreateConfigSection(Options oOptions, ref StringBuilder oStringBuilder)
        {
            oStringBuilder.AppendLine("     <appSettings>");

            PropertyInfo[] properties = typeof(Options).GetProperties();
            foreach (PropertyInfo oPropertyInfo in properties)
            {
                oStringBuilder.AppendLine(String.Format(@"      <!-- <add key=""{0}"" value=""{1}"" /> -->", oPropertyInfo.Name, oPropertyInfo.GetValue(oOptions).ToString()));
            }

            oStringBuilder.AppendLine("     </appSettings>");

            return (oStringBuilder);
        }

        public static Boolean GetBoolean(String key)
        {
            Boolean.TryParse(GetString(key), out bool bAppSetting);

            return bAppSetting;
        }

        public static int GetInt(String key)
        {
            int.TryParse(GetString(key), out int iAppSetting);

            return iAppSetting;
        }

        public static Decimal GetDecimal(String key)
        {
            Decimal.TryParse(GetString(key), out decimal dAppSetting);

            return dAppSetting;
        }

        public static string GetString(string key)
        {
            if (ConfigurationManager.AppSettings[key] != null)
            {
                return ConfigurationManager.AppSettings[key];
            }

            return string.Empty;
        }

        public static void SetString(string key, string value)
        {
            if (key != null && value != null)
            {
                if (ConfigurationManager.AppSettings[key] != null)
                {
                    ConfigurationManager.AppSettings[key] = value;
                }
                else
                {
                    ConfigurationManager.AppSettings.Add(key, value);
                }
            }
        }

        public static void Save()
        {
            //ConfigurationManager.Save(ConfigurationSaveMode.Modified);
            //ConfigurationManager.RefreshSection("appSettings");
        }

    }
}


