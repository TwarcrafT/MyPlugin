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

            if (player == null)
            {
                response = "This command must be executed at the game level.";
                return false;
            }

            if (MyPlugin.Instance.SchematicsToDestroyCommand.TryGetValue(player, out SchematicObject schematic))
            {
                if (schematic != null)
                {
                    schematic.Destroy();
                }
            }

            if (arguments.IsEmpty())
            {
                var schematicsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics");
                var builder = new StringBuilder();

                builder.Append(MyPlugin.Instance.Config.emotes.ListOfAnimations);

                var displayedBaseNames = new HashSet<string>();
                bool foundAnySchematic = false;

                if (!Directory.Exists(schematicsDir))
                {
                    response = $"{MyPlugin.Instance.Config.emotes.EmptyAnwer}\n\nNo schematics directory found.";
                    return false;
                }

                foreach (var directoryPath in Directory.GetDirectories(schematicsDir))
                {
                    foreach (var jsonFilePath in Directory.GetFiles(directoryPath)
                                                     .Where(x => x.EndsWith(".json") && x.Contains('!')))
                    {
                        var fullFileName = Path.GetFileNameWithoutExtension(jsonFilePath);

                        if (HasPermission(fullFileName, player))
                        {
                            string baseName = GetBaseName(fullFileName);
                            if (!displayedBaseNames.Contains(baseName))
                            {
                                displayedBaseNames.Add(baseName);
                                builder.AppendLine();
                                builder.Append($"- {baseName}");
                                foundAnySchematic = true;
                            }
                        }
                    }
                }

                response = foundAnySchematic ? $"{MyPlugin.Instance.Config.emotes.EmptyAnwer}\n\n{builder}" : $"{MyPlugin.Instance.Config.emotes.EmptyAnwer}\n\nNo schematics found.";
                return false;
            }

            var argumentsProvided = string.Join(" ", arguments);
            string schematicNameForLookup = $"!{argumentsProvided}";
            string schematicToUse = FindSchematicWithPermission(schematicNameForLookup, player);

            if (string.IsNullOrEmpty(schematicToUse))
            {
                response = MyPlugin.Instance.Config.emotes.NoPermission;
                return false;
            }

            var spawnedSchematic = ObjectSpawner.SpawnSchematic(schematicToUse, player.Position, player.Rotation, player.Scale);
            spawnedSchematic.transform.parent = player.Transform;
            spawnedSchematic.transform.localPosition = Vector3.zero;
            spawnedSchematic.transform.localRotation = Quaternion.identity;

            LoadAndApplyRigidbodies(schematicToUse, spawnedSchematic, player.Id);
            MyPlugin.Instance.SchematicsToDestroyCommand[player] = spawnedSchematic;

            response = $"{MyPlugin.Instance.Config.emotes.PlayedAnimation}\n{argumentsProvided}";
            return true;
        }

        private bool HasPermission(string fileName, Player player)
        {
            if (fileName.Contains("[NONE]")) return true;
            foreach (var permEntry in MyPlugin.Instance.Config.emotes.Permission)
            {
                if (fileName.Contains($"[{permEntry.Key}]") && permEntry.Value.Contains(player.Role.Type)) return true;
            }
            return false;
        }

        private string GetBaseName(string fullName)
        {
            string name = fullName;
            int bracketIndex = fullName.IndexOf('[');
            if (bracketIndex > 0) name = fullName.Substring(0, bracketIndex).Trim();
            if (name.StartsWith("!")) name = name.Substring(1);
            return name;
        }

        private string FindSchematicWithPermission(string baseName, Player player)
        {
            var schematicsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics");

            if (!Directory.Exists(schematicsDir)) return null;

            foreach (var directoryPath in Directory.GetDirectories(schematicsDir))
            {
                foreach (var jsonFilePath in Directory.GetFiles(directoryPath)
                                                     .Where(x => x.EndsWith(".json") && !x.EndsWith("-Rigidbodies.json") &&
                                                                 Path.GetFileNameWithoutExtension(x).StartsWith(baseName)))
                {
                    var fullFileName = Path.GetFileNameWithoutExtension(jsonFilePath);
                    if (HasPermission(fullFileName, player)) return fullFileName;
                }
            }
            return null;
        }

        private void LoadAndApplyRigidbodies(string schematicName, SchematicObject spawnedSchematic, int playerId)
        {
            var schematicsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics");
            string rigidbodiesFilePath = Path.Combine(schematicsDir, schematicName, $"{schematicName}-Rigidbodies.json");

            if (!File.Exists(rigidbodiesFilePath)) return;

            try
            {
                var rigidbodiesData = JsonConvert.DeserializeObject<Dictionary<string, RigidbodyProperties>>(File.ReadAllText(rigidbodiesFilePath));
                if (rigidbodiesData == null) return;

                foreach (var entry in rigidbodiesData)
                {
                    Transform targetTransform = FindChildRecursive(spawnedSchematic.transform, entry.Key);
                    Rigidbody rb = targetTransform?.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = entry.Value.IsKinematic;
                        rb.useGravity = entry.Value.UseGravity;
                        rb.constraints = (RigidbodyConstraints)entry.Value.Constraints;
                        rb.mass = entry.Value.Mass;
                    }
                }
            }
            catch (Exception ex) { Log.Error($"[MeCommand] Rigidbody error: {ex}"); }
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent.name == childName) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        public class RigidbodyProperties
        {
            public bool IsKinematic { get; set; }
            public bool UseGravity { get; set; }
            public int Constraints { get; set; }
            public float Mass { get; set; }
        }
    }
}