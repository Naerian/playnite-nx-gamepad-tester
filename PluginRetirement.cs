using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GamepadTester
{
    public sealed class ExtensionInstallQueueItem
    {
        public int InstallType { get; set; }

        public string Path { get; set; }
    }

    public static class PluginRetirement
    {
        public const string ControllerManagerAddonId = "ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc";
        public const string ControllerManagerInstallUri =
            "playnite://playnite/installaddon/ControllerSessionManager_6f3e7a21-98f4-4f2b-92ad-3fc0e6e941dc";
        public const string QueueFileName = "extinstalls.json";
        public const int UninstallQueueType = 1;
        public const string NotificationId = "gamepad-tester-retired";

        public static bool IsControllerManagerInstalled(IEnumerable<string> installedAddonIds)
        {
            if (installedAddonIds == null)
            {
                return false;
            }

            return installedAddonIds.Any(id =>
                string.Equals(id, ControllerManagerAddonId, StringComparison.OrdinalIgnoreCase));
        }

        public static bool TryOpenControllerManagerInstall(out string error)
        {
            error = null;
            try
            {
                Process.Start(new ProcessStartInfo(ControllerManagerInstallUri)
                {
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string GetQueueFilePath(string configurationPath)
        {
            if (string.IsNullOrWhiteSpace(configurationPath))
            {
                throw new ArgumentException("Playnite configuration path is required.", nameof(configurationPath));
            }

            return Path.Combine(configurationPath, QueueFileName);
        }

        public static bool IsUninstallQueued(IEnumerable<ExtensionInstallQueueItem> items, string extensionDirectory)
        {
            if (items == null || string.IsNullOrWhiteSpace(extensionDirectory))
            {
                return false;
            }

            return items.Any(item =>
                item != null &&
                item.InstallType == UninstallQueueType &&
                string.Equals(item.Path, extensionDirectory, StringComparison.OrdinalIgnoreCase));
        }

        public static List<ExtensionInstallQueueItem> AppendUninstall(
            IEnumerable<ExtensionInstallQueueItem> existing,
            string extensionDirectory)
        {
            var items = existing == null
                ? new List<ExtensionInstallQueueItem>()
                : existing.Where(item => item != null).ToList();

            if (!IsUninstallQueued(items, extensionDirectory))
            {
                items.Add(new ExtensionInstallQueueItem
                {
                    InstallType = UninstallQueueType,
                    Path = extensionDirectory
                });
            }

            return items;
        }

        public static bool TryQueueUninstall(string configurationPath, string extensionDirectory, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(extensionDirectory) || !Directory.Exists(extensionDirectory))
            {
                error = "Extension directory was not found.";
                return false;
            }

            try
            {
                var queuePath = GetQueueFilePath(configurationPath);
                var items = new List<ExtensionInstallQueueItem>();
                if (File.Exists(queuePath))
                {
                    var json = File.ReadAllText(queuePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        items = Serialization.FromJson<List<ExtensionInstallQueueItem>>(json)
                            ?? new List<ExtensionInstallQueueItem>();
                    }
                }

                File.WriteAllText(queuePath, Serialization.ToJson(AppendUninstall(items, extensionDirectory), true));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
