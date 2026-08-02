using CommandSystem;
using LabApi.Features.Enums;
using MyPlugin.EventHandlers;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Player = LabApi.Features.Wrappers.Player;

namespace MyPlugin.Command;

public class Me : ICommand
{
    public string Command => "me";
    public string[] Aliases => new[] { "express", ".m", "emote" };

    public string Description => MyPlugin.Instance?.Config?.EmotesCfg?.CommandDescription ?? "RPExpress";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player player = Player.Get(sender);

        if (player == null)
        {
            response = "This command must be executed at the game level.";
            return false;
        }

        if (MyPlugin.Instance.SchematicsToDestroyCommand.TryGetValue(player, out SchematicObject schematic))
        {
            schematic?.Destroy();
            MyPlugin.Instance.SchematicsToDestroyCommand.Remove(player);
            MyPlugin.Instance.EmoteAnimationClips.Remove(player);
            PlayerSSSSync.Refresh(player);
        }

        if (arguments.Count == 0)
        {
            response = ListEmotes(player);
            return false;
        }

        var argumentsProvided = string.Join(" ", arguments);
        string schematicNameForLookup = $"!{argumentsProvided}";
        string schematicToUse = FindSchematicWithPermission(schematicNameForLookup, player);

        if (string.IsNullOrEmpty(schematicToUse))
        {
            response = MyPlugin.Instance.Config.EmotesCfg.NoPermission;
            return false;
        }

        var spawnedSchematic = ObjectSpawner.SpawnSchematic(schematicToUse, player.Position, player.Rotation, player.Scale);
        spawnedSchematic.transform.parent = player.GameObject.transform;
        spawnedSchematic.transform.localPosition = Vector3.zero;
        spawnedSchematic.transform.localRotation = Quaternion.identity;

        MyPlugin.Instance.SchematicsToDestroyCommand[player] = spawnedSchematic;

        PlayerSSSSync.Refresh(player);

        response = $"{MyPlugin.Instance.Config.EmotesCfg.PlayedAnimation}\n{argumentsProvided}";
        return true;
    }

    private string ListEmotes(Player player)
    {
        var builder = new StringBuilder();
        builder.Append(MyPlugin.Instance.Config.EmotesCfg.ListOfAnimations);

        var displayedBaseNames = new HashSet<string>();
        bool foundAnySchematic = false;

        foreach (var (fullName, baseName) in PluginUtils.EnumerateSchematics())
        {
            if (!HasPermission(fullName, player))
                continue;

            if (displayedBaseNames.Add(baseName))
            {
                builder.AppendLine();
                builder.Append($"- {baseName}");
                foundAnySchematic = true;
            }
        }

        return foundAnySchematic ? $"{MyPlugin.Instance.Config.EmotesCfg.EmptyAnwer}\n\n{builder}" : $"{MyPlugin.Instance.Config.EmotesCfg.EmptyAnwer}\n\nNo schematics found.";
    }

    private bool HasPermission(string fileName, Player player)
    {
        if (fileName.Contains("[NONE]")) return true;
        foreach (var permEntry in MyPlugin.Instance.Config.EmotesCfg.Permission)
        {
            if (fileName.Contains($"[{permEntry.Key}]") && permEntry.Value.Contains(player.Role))
                return true;
        }
        return false;
    }

    private string FindSchematicWithPermission(string lookupPrefix, Player player)
    {
        return PluginUtils.EnumerateSchematics()
            .FirstOrDefault(w => w.FullName.StartsWith(lookupPrefix) && HasPermission(w.FullName, player))
            .FullName;
    }

    public static void Register(Me meCommand, ICommand command, params CommandType[] commandTypes)
    {
        foreach (CommandType commandType in commandTypes)
        {
            switch (commandType)
            {
                case CommandType.Client:
                    QueryProcessor.DotCommandHandler.RegisterCommand(command);
                    break;
                case CommandType.RemoteAdmin:
                    CommandProcessor.RemoteAdminCommandHandler.RegisterCommand(command);
                    break;
                case CommandType.Console:
                    GameCore.Console.ConsoleCommandHandler.RegisterCommand(command);
                    break;
            }
        }
    }
}