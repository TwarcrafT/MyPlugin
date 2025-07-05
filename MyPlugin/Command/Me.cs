using System.Linq;
using System.Text;
using System;
using System.IO;
using CommandSystem;
using Exiled.API.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features;
using System.Collections.Generic;
using PlayerRoles;
using Newtonsoft.Json;
using UnityEngine;

namespace MyPlugin.Command
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class Me : ICommand
    {
        public string Command { get; } = MyPlugin.Instance.Config.emotes.CommandName;
        public string[] Aliases { get; } = { MyPlugin.Instance.Config.emotes.CommandAlias };
        public string Description { get; } = MyPlugin.Instance.Config.emotes.CommandDescription;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);

            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Executing 'Me' command by {player?.Nickname ?? "CONSOLE"}");

            if (player == null)
            {
                response = "This command must be executed at the game level.";
                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Command not executed by player (sender is null).");
                return false;
            }

            if (MyPlugin.Instance.SchematicsToDestroyCommand.TryGetValue(player, out SchematicObject schematic))
            {
                if (schematic != null)
                {
                    schematic.Destroy();
                    if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Destroyed previous schematic for {player.Nickname}.");
                }
            }

            if (arguments.IsEmpty())
            {
                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Arguments are empty. Listing available emotes for {player.Nickname}.");

                var schematicsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics");
                var builder = new StringBuilder();

                builder.Append(MyPlugin.Instance.Config.emotes.ListOfAnimations);

                var playerRole = player.Role.Type;
                var displayedBaseNames = new HashSet<string>();

                foreach (var directoryPath in Directory.GetDirectories(schematicsDir))
                {
                    foreach (var jsonFilePath in Directory.GetFiles(directoryPath)
                                                     .Where(x => x.EndsWith(".json") && x.Contains('!')))
                    {
                        var fullFileName = Path.GetFileNameWithoutExtension(jsonFilePath);
                        bool hasPermission = fullFileName.Contains("[NONE]");

                        foreach (var permEntry in MyPlugin.Instance.Config.emotes.Permission)
                        {
                            string permKey = permEntry.Key;
                            List<RoleTypeId> permRoles = permEntry.Value;

                            if (fullFileName.Contains($"[{permKey}]") && permRoles.Contains(player.Role.Type))
                            {
                                hasPermission = true;
                                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Player {player.Nickname} has permission [{permKey}] for schematic {fullFileName}.");
                                break;
                            }
                        }

                        if (hasPermission)
                        {
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

                            if (!displayedBaseNames.Contains(baseName))
                            {
                                displayedBaseNames.Add(baseName);
                                builder.AppendLine();
                                builder.Append($"- {baseName}");
                            }
                        }
                        else
                        {
                            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Player {player.Nickname} does NOT have permission for schematic {fullFileName}. Skipping.");
                        }
                    }
                }
                response = $"{MyPlugin.Instance.Config.emotes.EmptyAnwer}\n\n{builder}";
                return false;
            }

            var argumentsProvided = string.Join(" ", arguments);
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Player {player.Nickname} requested emote: '{argumentsProvided}'.");

            string schematicNameForLookup = $"!{argumentsProvided}";

            string schematicToUse = FindSchematicWithPermission(schematicNameForLookup, player);

            if (string.IsNullOrEmpty(schematicToUse))
            {
                response = MyPlugin.Instance.Config.emotes.NoPermission;
                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] No schematic found or no permission for '{schematicNameForLookup}' for {player.Nickname}.");
                return false;
            }

            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Spawning schematic '{schematicToUse}' for {player.Nickname}.");
            var spawnedSchematic = ObjectSpawner.SpawnSchematic(schematicToUse, player.Position, player.Rotation, player.Scale);

            spawnedSchematic.transform.parent = player.Transform;

            LoadAndApplyRigidbodies(schematicToUse, spawnedSchematic, player.Id);

            MyPlugin.Instance.SchematicsToDestroyCommand[player] = spawnedSchematic;

            response = $"{MyPlugin.Instance.Config.emotes.PlayedAnimation}\n{argumentsProvided}";
            return true;
        }

        private string FindSchematicWithPermission(string baseName, Player player)
        {
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Searching for schematic '{baseName}' with permissions for {player.Nickname}.");

            var schematicsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics");

            foreach (var directoryPath in Directory.GetDirectories(schematicsDir))
            {
                foreach (var jsonFilePath in Directory.GetFiles(directoryPath)
                                                     .Where(x => x.EndsWith(".json") &&
                                                                 !x.EndsWith("-Rigidbodies.json") &&
                                                                 Path.GetFileNameWithoutExtension(x).StartsWith(baseName)))
                {
                    var fullFileName = Path.GetFileNameWithoutExtension(jsonFilePath);

                    if (fullFileName.Contains("[NONE]"))
                    {
                        if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Found schematic '{fullFileName}' with [NONE] permission.");
                        return fullFileName;
                    }

                    foreach (var permEntry in MyPlugin.Instance.Config.emotes.Permission)
                    {
                        string permKey = permEntry.Key;
                        List<RoleTypeId> permRoles = permEntry.Value;

                        if (fullFileName.Contains($"[{permKey}]") && permRoles.Contains(player.Role.Type))
                        {
                            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Found schematic '{fullFileName}' with permission [{permKey}] for {player.Nickname}.");
                            return fullFileName;
                        }
                    }
                    if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Schematic '{fullFileName}' found but {player.Nickname} lacks specific permission.");
                }
            }
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] No matching schematic with permission found for '{baseName}' for {player.Nickname}.");
            return null;
        }


        private void LoadAndApplyRigidbodies(string schematicName, SchematicObject spawnedSchematic, int playerId)
        {
            var schematicsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics");

            string schematicDirectory = Path.Combine(schematicsDir, schematicName);

            string rigidbodiesFilePath = Path.Combine(schematicDirectory, $"{schematicName}-Rigidbodies.json");

            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Attempting to load Rigidbodies file: {rigidbodiesFilePath}");

            if (!File.Exists(rigidbodiesFilePath))
            {
                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] No Rigidbodies file found for {schematicName}. Skipping physics application.");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(rigidbodiesFilePath);

                var rigidbodiesData = JsonConvert.DeserializeObject<Dictionary<string, RigidbodyProperties>>(jsonContent);

                if (rigidbodiesData == null || rigidbodiesData.Count == 0)
                {
                    if (MyPlugin.Instance.Config.Debug) Log.Warn($"[MeCommand] Rigidbodies data is null or empty after deserialization for {schematicName}.");
                    return;
                }

                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Found {rigidbodiesData.Count} rigidbodies in {schematicName}-Rigidbodies.json.");

                foreach (var entry in rigidbodiesData)
                {
                    string objectId = entry.Key;
                    RigidbodyProperties rbProps = entry.Value;

                    Rigidbody schematicRigidbody = spawnedSchematic.gameObject.GetComponent<Rigidbody>();
                    if (schematicRigidbody != null)
                    {
                        schematicRigidbody.isKinematic = rbProps.IsKinematic;
                        schematicRigidbody.useGravity = rbProps.UseGravity;
                        schematicRigidbody.constraints = (RigidbodyConstraints)rbProps.Constraints;
                        schematicRigidbody.mass = rbProps.Mass;
                        if (MyPlugin.Instance.Config.Debug) Log.Debug($"[MeCommand] Applied Rigidbody data from ID {objectId} to schematic root for {schematicName}.");
                    }
                    else
                    {
                        if (MyPlugin.Instance.Config.Debug) Log.Warn($"[MeCommand] Schematic root for {schematicName} has no Rigidbody component. ID: {objectId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MeCommand] Failed to load or apply Rigidbodies for {schematicName}: {ex}");
            }
        }

        public class RigidbodyProperties
        {
            [JsonProperty("IsKinematic")]
            public bool IsKinematic { get; set; }

            [JsonProperty("UseGravity")]
            public bool UseGravity { get; set; }

            [JsonProperty("Constraints")]
            public int Constraints { get; set; }

            [JsonProperty("Mass")]
            public float Mass { get; set; }
        }
    }
}