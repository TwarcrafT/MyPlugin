using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using UnityEngine;

namespace MyPlugin.Command

// debug command for checking plugin status
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class MyPluginTest : ICommand
    {
        public string Command => "testmp";
        public string[] Aliases => System.Array.Empty<string>();
        public string Description => "Diagnose MyPlugin configuration and dependencies";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            var builder = new StringBuilder();

            builder.AppendLine("=== MyPlugin Diagnostic Test ===\n");

            // 1. Plugin Instance Check
            builder.AppendLine("1. Plugin Instance:");
            if (MyPlugin.Instance != null)
            {
                builder.AppendLine($"   ✓ Instance exists");
                builder.AppendLine($"   ✓ Version: {MyPlugin.Instance.Version}");
                builder.AppendLine($"   ✓ Enabled: {MyPlugin.Instance.Config.IsEnabled}");
                builder.AppendLine($"   ✓ Debug: {MyPlugin.Instance.Config.Debug}");
            }
            else
            {
                builder.AppendLine("   ✗ Instance is NULL - Plugin not loaded!");
                response = builder.ToString();
                return false;
            }

            // 2. Config Check
            builder.AppendLine("\n2. Configuration:");
            try
            {
                builder.AppendLine($"   ✓ Config loaded");
                builder.AppendLine($"   ✓ Command Name: {MyPlugin.Instance.Config.emotes.CommandName}");
                builder.AppendLine($"   ✓ Command Alias: {MyPlugin.Instance.Config.emotes.CommandAlias}");
                builder.AppendLine($"   ✓ Permissions count: {MyPlugin.Instance.Config.emotes.Permission.Count}");
            }
            catch (Exception ex)
            {
                builder.AppendLine($"   ✗ Config error: {ex.Message}");
            }

            // 3. Exiled Version Check
            builder.AppendLine("\n3. Exiled Version:");
            try
            {
                var exiledVersion = Exiled.Loader.Loader.Version;
                builder.AppendLine($"   Current version: {exiledVersion}");
                builder.AppendLine($"   Required version: 9.12.1.0");

                if (exiledVersion.ToString() == "9.12.1.0")
                {
                    builder.AppendLine($"   ✓ Compatible version");
                }
                else
                {
                    builder.AppendLine($"   ⚠ Version mismatch - May cause compatibility issues");
                }
            }
            catch (Exception ex)
            {
                builder.AppendLine($"   ✗ Error checking Exiled version: {ex.Message}");
            }

            // 4. ProjectMER Check
            builder.AppendLine("\n4. ProjectMER:");
            try
            {
                var merAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "ProjectMER");

                if (merAssembly != null)
                {
                    var merVersion = merAssembly.GetName().Version;
                    var versionString = $"{merVersion.Major}.{merVersion.Minor}.{merVersion.Build}.{merVersion.Revision}.{merVersion.MinorRevision}";

                    builder.AppendLine($"   ✓ ProjectMER loaded");
                    builder.AppendLine($"   Current version: ... idk {merVersion.Major}.{merVersion.Minor}.{merVersion.Build}.{merVersion.Revision}.{merVersion.MinorRevision} ");
                    builder.AppendLine($"   Required version: 2025.11.2.1");

                    
                }
                else
                {
                    builder.AppendLine("   ✗ ProjectMER NOT found!");
                    builder.AppendLine("   ! Install ProjectMER version 2025.11.2.1");
                }
            }
            catch (Exception ex)
            {
                builder.AppendLine($"   ✗ Error checking ProjectMER: {ex.Message}");
            }

            // 5. Schematics Directory Check
            builder.AppendLine("\n5. Schematics Directory:");
            var schematicsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics"
            );

            builder.AppendLine($"   Path: {schematicsDir}");

            if (Directory.Exists(schematicsDir))
            {
                builder.AppendLine($"   ✓ Directory exists");

                var directories = Directory.GetDirectories(schematicsDir);
                builder.AppendLine($"   ✓ Subdirectories: {directories.Length}");

                int schematicCount = 0;
                int emoteCount = 0;

                foreach (var dir in directories)
                {
                    var jsonFiles = Directory.GetFiles(dir, "*.json");
                    schematicCount += jsonFiles.Length;
                    emoteCount += jsonFiles.Count(f => Path.GetFileName(f).StartsWith("!"));
                }

                builder.AppendLine($"   ✓ Total JSON files: {schematicCount}");
                builder.AppendLine($"   ✓ Emote files (starting with !): {emoteCount}");

                if (emoteCount == 0)
                {
                    builder.AppendLine("   ⚠ No emote schematics found! Add files starting with '!'");
                }
            }
            else
            {
                builder.AppendLine("   ✗ Directory does NOT exist!");
                builder.AppendLine($"   ! Create: {schematicsDir}");
            }

            // 6. Player-Specific Check
            if (player != null)
            {
                builder.AppendLine("\n6. Player Status:");
                builder.AppendLine($"   ✓ Player: {player.Nickname}");
                builder.AppendLine($"   ✓ Role: {player.Role.Type}");
                builder.AppendLine($"   ✓ Position: {player.Position}");

                // Check available emotes for player
                builder.AppendLine("\n7. Available Emotes for Current Role:");
                int availableEmotes = 0;

                if (Directory.Exists(schematicsDir))
                {
                    foreach (var directoryPath in Directory.GetDirectories(schematicsDir))
                    {
                        foreach (var jsonFilePath in Directory.GetFiles(directoryPath)
                                                         .Where(x => x.EndsWith(".json") && x.Contains('!')))
                        {
                            var fullFileName = Path.GetFileNameWithoutExtension(jsonFilePath);
                            bool hasPermission = fullFileName.Contains("[NONE]");

                            foreach (var permEntry in MyPlugin.Instance.Config.emotes.Permission)
                            {
                                if (fullFileName.Contains($"[{permEntry.Key}]") &&
                                    permEntry.Value.Contains(player.Role.Type))
                                {
                                    hasPermission = true;
                                    break;
                                }
                            }

                            if (hasPermission)
                            {
                                availableEmotes++;
                                string baseName = fullFileName;
                                int bracketIndex = fullFileName.IndexOf('[');
                                if (bracketIndex > 0)
                                {
                                    baseName = fullFileName.Substring(0, bracketIndex).Trim();
                                }
                                if (baseName.StartsWith("!"))
                                {
                                    baseName = baseName.Substring(1);
                                }
                                builder.AppendLine($"   - {baseName}");
                            }
                        }
                    }
                }

                if (availableEmotes == 0)
                {
                    builder.AppendLine("   ⚠ No emotes available for your current role!");
                }
                else
                {
                    builder.AppendLine($"\n   ✓ Total available: {availableEmotes}");
                }
            }
            else
            {
                builder.AppendLine("\n6. Player Status:");
                builder.AppendLine("   ⚠ Command executed from console");
            }

            // 8. Active Schematics Check
            builder.AppendLine("\n8. Active Schematics:");
            if (MyPlugin.Instance.SchematicsToDestroyCommand != null)
            {
                builder.AppendLine($"   ✓ Dictionary exists");
                builder.AppendLine($"   ✓ Active schematics: {MyPlugin.Instance.SchematicsToDestroyCommand.Count}");

                foreach (var kvp in MyPlugin.Instance.SchematicsToDestroyCommand)
                {
                    builder.AppendLine($"   - {kvp.Key.Nickname}: {(kvp.Value != null ? "Active" : "NULL")}");
                }
            }
            else
            {
                builder.AppendLine("   ✗ Dictionary is NULL!");
            }

            // Summary
            builder.AppendLine("\n=== Summary ===");
            builder.AppendLine("If all checks show ✓, the plugin should work correctly.");
            builder.AppendLine("Any ✗ or ⚠ indicates potential issues.");

            response = builder.ToString();
            return true;
        }
    }
}