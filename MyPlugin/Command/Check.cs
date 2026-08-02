using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CommandSystem;
using LabApi.Features;
using LabApi.Loader;

namespace MyPlugin.Command;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class Check : ICommand
{
    public string Command => "checkmp";
    public string[] Aliases => System.Array.Empty<string>();
    public string Description => "Check MyPlugin and LabAPI version";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== MyPlugin Diagnostic ===\n");

        if (MyPlugin.Instance != null)
        {
            builder.AppendLine($"Plugin: {MyPlugin.Instance.Name} v{MyPlugin.Instance.Version}");
            builder.AppendLine($"Enabled: {MyPlugin.Instance.Config.IsEnabled}");
        }
        else
        {
            response = "Plugin not loaded!";
            return false;
        }

        AppendLabApiStatus(builder);
        AppendProjectMerStatus(builder);
        AppendSchematicsStatus(builder);

        builder.AppendLine($"\nActive: {MyPlugin.Instance.SchematicsToDestroyCommand.Count} emotes, {MyPlugin.Instance.WearableSchematics.Count} wearables");

        response = builder.ToString();
        return true;
    }
    private void AppendLabApiStatus(StringBuilder builder)
    {
        try
        {
            Version current = LabApiProperties.CurrentVersion;
            builder.AppendLine($"\nLabAPI Current: {LabApiProperties.CompiledVersion}");

            string latestTag = GetLatestGitHubTag("northwood-studios", "LabAPI");
            if (latestTag != null)
            {
                builder.AppendLine($"LabAPI Latest: {latestTag}");
                builder.AppendLine(VersionsAreEquivalent(current, latestTag) ? "Status: Up to date" : "Status: Update available");
            }
            else
            {
                builder.AppendLine("Could not fetch latest version from GitHub.");
            }
        }
        catch (Exception ex)
        {
            builder.AppendLine($"\nLabAPI check error: {ex.Message}");
        }
    }

    private void AppendProjectMerStatus(StringBuilder builder)
    {
        try
        {
            var merPlugin = PluginLoader.Plugins.Keys.FirstOrDefault(p => p.Name == "ProjectMER");

            if (merPlugin == null)
            {
                builder.AppendLine("\nProjectMER: NOT found!");
                return;
            }

            builder.AppendLine($"\nProjectMER Current: {merPlugin.Version}");

            string latestTag = GetLatestGitHubTag("Michal78900", "ProjectMER");
            if (latestTag != null)
            {
                builder.AppendLine($"ProjectMER Latest: {latestTag}");
                builder.AppendLine(VersionsAreEquivalent(merPlugin.Version, latestTag) ? "Status: Up to date" : "Status: Update available");
            }
            else
            {
                builder.AppendLine("Could not fetch latest version from GitHub.");
            }
        }
        catch (Exception ex)
        {
            builder.AppendLine($"\nProjectMER check error: {ex.Message}");
        }
    }

    private void AppendSchematicsStatus(StringBuilder builder)
    {
        if (!Directory.Exists(PluginUtils.SchematicsDir))
        {
            builder.AppendLine("\nSchematics directory not found!");
            return;
        }

        int emoteCount = 0;
        int wearableCount = 0;

        foreach (var dir in Directory.GetDirectories(PluginUtils.SchematicsDir))
        {
            var files = Directory.GetFiles(dir, "*.json");
            wearableCount += files.Count(f => Path.GetFileName(f).StartsWith("!") && f.Contains("[WEAR]"));
            emoteCount += files.Count(f => Path.GetFileName(f).StartsWith("!") && !f.Contains("[WEAR]"));
        }

        builder.AppendLine($"\nSchematics: {emoteCount} emotes, {wearableCount} wearables");
    }

    private string GetLatestGitHubTag(string owner, string repo)
    {
        try
        {
            using var client = new WebClient();
            client.Headers.Add("User-Agent", "MyPlugin-VersionCheck");
            string html = client.DownloadString($"https://github.com/{owner}/{repo}/releases");

            var match = Regex.Match(html, $@"<a[^>]*href=""/{Regex.Escape(owner)}/{Regex.Escape(repo)}/releases/tag/([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private bool VersionsAreEquivalent(Version current, string latestTag)
    {
        string cleanTag = latestTag.TrimStart('v', 'V');

        int[] latestParts = cleanTag.Split('.')
            .Select(p => int.TryParse(p, out int n) ? n : 0)
            .ToArray();

        int[] currentParts =
        {
            current.Major,
            current.Minor,
            current.Build < 0 ? 0 : current.Build,
            current.Revision < 0 ? 0 : current.Revision,
        };

        int length = Math.Max(latestParts.Length, currentParts.Length);
        for (int i = 0; i < length; i++)
        {
            int a = i < currentParts.Length ? currentParts[i] : 0;
            int b = i < latestParts.Length ? latestParts[i] : 0;
            if (a != b) return false;
        }

        return true;
    }
}