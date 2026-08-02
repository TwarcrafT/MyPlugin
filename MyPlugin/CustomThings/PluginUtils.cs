using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyPlugin;

public static class PluginUtils
{
    private static string _schematicsDir;

    public static string SchematicsDir => _schematicsDir ??= Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SCP Secret Laboratory", "LabAPI", "configs", "ProjectMER", "Schematics");

    /// <summary>
    /// Enumerates schematic definition files found in the schematics directory.
    /// </summary>
    /// <param name="tagFilter">Optional substring filter, e.g. "[WEAR]" to only return wearable schematics.</param>
    public static IEnumerable<(string FullName, string BaseName)> EnumerateSchematics(string tagFilter = null)
    {
        if (!Directory.Exists(SchematicsDir))
            yield break;

        foreach (var directoryPath in Directory.GetDirectories(SchematicsDir))
        {
            var files = Directory.GetFiles(directoryPath).Where(x => x.EndsWith(".json") && x.Contains('!'));

            if (!string.IsNullOrEmpty(tagFilter))
                files = files.Where(x => x.Contains(tagFilter));

            foreach (var jsonFilePath in files)
            {
                string fullName = Path.GetFileNameWithoutExtension(jsonFilePath);
                yield return (fullName, GetBaseName(fullName));
            }
        }
    }

    /// <summary>
    /// Strips the "[TAG]" suffix and leading "!" from a schematic file name to get its display name.
    /// </summary>
    public static string GetBaseName(string fullName)
    {
        string name = fullName;
        int bracketIndex = fullName.IndexOf('[');
        if (bracketIndex > 0) name = fullName.Substring(0, bracketIndex).Trim();
        if (name.StartsWith("!")) name = name.Substring(1);
        return name;
    }

    /// <summary>
    /// Finds the first schematic whose full file name starts with the given prefix (e.g. "!schematicname").
    /// </summary>
    public static string FindSchematic(string lookupPrefix, string tagFilter = null)
    {
        return EnumerateSchematics(tagFilter).FirstOrDefault(w => w.FullName.StartsWith(lookupPrefix)).FullName;
    }
    /// <summary>
    /// Black Magic from Exiled
    /// </summary>

    public static void SendFakeSyncVar(this Player target, NetworkIdentity behaviorIdentity, Type targetType, string propertyName, object value)
    {
        if (target?.ReferenceHub?.connectionToClient == null || behaviorIdentity == null)
            return;

        NetworkBehaviour behaviour = behaviorIdentity.GetComponent(targetType) as NetworkBehaviour;
        if (behaviour == null)
            return;

        NetworkWriter writer = NetworkWriterPool.Get();
        NetworkWriter targetWriter = NetworkWriterPool.Get();

        try
        {
            writer.WriteULong(1UL);

            if (value is string strVal)
                writer.WriteString(strVal);
            else if (value is int intVal)
                writer.WriteInt(intVal);
            else if (value is bool boolVal)
                writer.WriteBool(boolVal);

            targetWriter.WriteByte((byte)behaviour.ComponentIndex);
            targetWriter.WriteBytesAndSize(writer.ToArray(), 0, writer.Position);

            EntityStateMessage message = new EntityStateMessage
            {
                netId = behaviorIdentity.netId,
                payload = targetWriter.ToArraySegment()
            };

            target.ReferenceHub.connectionToClient.Send(message);
        }
        finally
        {
            NetworkWriterPool.Return((NetworkWriterPooled)writer);
            NetworkWriterPool.Return((NetworkWriterPooled)targetWriter);
        }
    }

    public static void MakePlayerInvisibleForPlayers(Player player, IEnumerable<Player> targets, float duration = 0f)
    {
        if (player == null || targets == null)
            return;

        foreach (Player target in targets.Where(p => p != null && p != player))
        {
            target.SendFakeSyncVar(player.ReferenceHub.networkIdentity, typeof(NicknameSync), nameof(NicknameSync.Network_myNickSync), string.Empty);
        }

        if (duration > 0)
        {
            Timing.CallDelayed(duration, () => MakePlayerVisibleForPlayers(player, targets));
        }
    }

    public static void MakePlayerVisibleForPlayers(Player player, IEnumerable<Player> targets)
    {
        if (player == null || targets == null)
            return;

        foreach (Player target in targets.Where(p => p != null && p != player))
        {
            target.SendFakeSyncVar(player.ReferenceHub.networkIdentity, typeof(NicknameSync), nameof(NicknameSync.Network_myNickSync), player.Nickname);
        }
    }

    public static Config.WearableConfig.AnimationNames GetAnimationNames(string schematicName)
    {
        var cfg = MyPlugin.Instance?.Config?.WearableCfg;

        if (!string.IsNullOrEmpty(schematicName) &&
            cfg?.SchematicAnimations != null &&
            cfg.SchematicAnimations.TryGetValue(schematicName, out var names) &&
            names != null)
        {
            return names;
        }

        return new Config.WearableConfig.AnimationNames();
    }
    public static Config.WearableConfig.AnimationNames GetAnimationNames(Player player)
    {
        if (player != null && MyPlugin.Instance.WearableSchematicNames.TryGetValue(player, out string schematicName))
            return GetAnimationNames(schematicName);

        return new Config.WearableConfig.AnimationNames();
    }
}