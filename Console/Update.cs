using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;


namespace MTOvJoyFeeder
{
    public static class Update
    {
        public static void Check()
        {
            string sApplicationName = Assembly.GetExecutingAssembly().GetName().Name;
            System.Version oThisVersion = Assembly.GetEntryAssembly().GetName().Version;
            (Version oLatestVersion, string sLatestFilename) = GetLatestVersionData();

            if (oLatestVersion != null &&  oThisVersion.CompareTo(oLatestVersion) < 0)
            {
                Program.WriteToEventLog("A newer version of " + sApplicationName + " is available (" + oLatestVersion.ToString() + ").", Verbosity.Warning);

                string sSaveLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (GetCurrentVersion(oLatestVersion, sLatestFilename, ref sSaveLocation))
                {
                    try
                    {
                        var oUpdateDirectory = new FileInfo(sSaveLocation).Directory;

                        DirectoryInfo oPreviousVersionDirectory;
                        if (!Directory.Exists(oUpdateDirectory.Parent.FullName + "\\PreviousVersion\\"))
                        {
                            oPreviousVersionDirectory = Directory.CreateDirectory(oUpdateDirectory.Parent.FullName + "\\PreviousVersion\\");
                        }
                        else
                        {
                            oPreviousVersionDirectory = new FileInfo(oUpdateDirectory.Parent.FullName + "\\PreviousVersion\\").Directory;
                        }

                        foreach (FileInfo oFileInfo in oPreviousVersionDirectory.GetFiles())
                        {
                            oFileInfo.Delete();
                        }

                        try
                        {
                            foreach (FileInfo oFileInfo in oPreviousVersionDirectory.Parent.GetFiles())
                            {
                                // Move files to the PreviousVersion folder
                                if (oFileInfo.Name.Contains(".config") || oFileInfo.Name.Contains(".json"))
                                {
                                    File.Copy(oFileInfo.FullName, oPreviousVersionDirectory.FullName + "\\" + oFileInfo.Name);
                                }
                                else
                                {
                                    File.Move(oFileInfo.FullName, oPreviousVersionDirectory.FullName + "\\" + oFileInfo.Name);
                                }
                            }

                            Decompress(sSaveLocation, oPreviousVersionDirectory.Parent.FullName);
                        }
                        catch
                        {
                            // Rollback
                            foreach (FileInfo oFileInfo in oPreviousVersionDirectory.GetFiles())
                            {
                                // Move files back to the main plugin folder
                                File.Move(oFileInfo.FullName, oPreviousVersionDirectory.Parent.FullName + "\\" + oFileInfo.Name);
                            }

                            throw;
                        }
                        
                        Program.WriteToEventLog(sApplicationName + " has been updated. Update will be applied on next restart of application", Verbosity.Warning);
                    }
                    catch (Exception exception)
                    {
                        Program.WriteToEventLog(sApplicationName + " could not be updated. Exception: " + exception.Message + " This may be due to a network issue, a configuration issue, lack of file permissions, or a too out of date install that can not be auto updated. If this error persists please update manually by downloading the latest version from https://github.com/fpdavis/MTOvJoyFeeder/releases", Verbosity.Error);
                    }
                }
            }
            else if (oLatestVersion == null)
            {
                Program.WriteToEventLog("A version number could not be found while checking for an update.", Verbosity.Critical);
            }
            else
            {
                Program.WriteToEventLog(String.Format("{0} ({1}) is up to date.", sApplicationName, oThisVersion.ToString()), Verbosity.Information);
            }
        }

        private static void Decompress(string zipFilePath, string extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                string sEntryPath;

                foreach (ZipArchiveEntry oEntry in archive.Entries)
                {
                    sEntryPath = Path.Combine(extractPath, GetRightPartOfPath(oEntry.FullName));
                    Directory.CreateDirectory(Path.GetDirectoryName(sEntryPath));

                    oEntry.ExtractToFile(sEntryPath);
                }
            }
        }

        private static string GetRightPartOfPath(string path)
        {
            string[] pathParts;

            // use the correct seperator for the environment
            if (path.Contains(Path.DirectorySeparatorChar))
            {
                pathParts = path.Split(Path.DirectorySeparatorChar);
            }
            else
            {
                pathParts = path.Split(Path.AltDirectorySeparatorChar);
            }

            return string.Join(
                Path.DirectorySeparatorChar.ToString(),
                pathParts, 1,
                pathParts.Length - 1);
        }

        private static (Version, string) GetLatestVersionData()
        {
            Version oLatestVersion = null;
            string sLatestFilename = string.Empty;

            byte[] oBytes = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(Program.goOptions.VersionUrl))
                {
                    oBytes = new WebClient().DownloadData(Program.goOptions.VersionUrl);
                }

                if (oBytes != null)
                {
                    string sWebResponse = Encoding.UTF8.GetString(oBytes);

                    //<span class="css-truncate-target">1.0.0</span>
                    //Match match = Regex.Match(sWebResponse, @"<span\s*class=""css-truncate-target"">(.*)<\/span>", RegexOptions.IgnoreCase);

                    // /fpdavis/MTOvJoyFeeder/releases/download/1.0.0/MTOvJoyFeeder.zip
                    // \/fpdavis\/MarquesasServer\/releases\/download\/(.*\/.*)[^""]
                    Match match = Regex.Match(sWebResponse, @"\/fpdavis\/MTOvJoyFeeder\/releases\/download\/(.*)\/([^""]*)", RegexOptions.IgnoreCase);

                    // Here we check the Match instance.
                    if (match.Success)
                    {
                        // Finally, we get the Group value representing the version number.
                        System.Version.TryParse(match.Groups[1].Value.Trim(), out oLatestVersion);
                        sLatestFilename = match.Groups[2].Value.Trim();
                    }
                }
            }
            catch
            {
                // ignored
            }

            return (oLatestVersion, sLatestFilename);
        }

        private static Boolean GetCurrentVersion(Version oLatestVersion, string sLatestFilename, ref string sSaveLocation)
        {
            Boolean bSuccess = false;
            sSaveLocation += "\\Updates\\" + sLatestFilename.Replace(".", "-" + oLatestVersion.ToString() + ".");

            if (File.Exists(sSaveLocation))
            {
                Program.WriteToEventLog("New version previously downloaded to " + sSaveLocation, Verbosity.Critical);
                return true;
            }

            try
            {
                byte[] oBytes = new WebClient().DownloadData(Program.goOptions.DownloadUrl + "/" + oLatestVersion.ToString() + "/" + sLatestFilename);

                if (oBytes != null && oBytes.Length > 20000)
                {
                    new FileInfo(sSaveLocation).Directory?.Create();

                    File.WriteAllBytes(sSaveLocation, oBytes);
                    bSuccess = true;
                }
            }
            catch
            {
                Program.WriteToEventLog("New version could not be downloaded. This may be due to a network issue, a configuration issue, lack of file permissions, or a too out of date install that can not be auto updated. If this error persists please update manually by downloading the latest version from https://github.com/fpdavis/MTOvJoyFeeder/releases");
            }

            return bSuccess;
        }
    }
}