using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using UserSettings.ServerSpecific;

namespace MyPlugin.EventHandlers;
/// <summary>
/// Main purpose of this class is to synchronize the server-specific settings for a player, combining settings from both custom animations and emote animations,
/// and sending them to the player's client. It also handles clearing the settings when necessary.
/// </summary>
public static class PlayerSSSSync
{
    public static void Refresh(Player player)
    {
        if (player?.ReferenceHub == null)
            return;

        var combined = new List<ServerSpecificSettingBase>();

        var wearSettings = SSSCustomAnimations.BuildSettings(player);
        if (wearSettings != null)
            combined.AddRange(wearSettings);

        var emoteSettings = EmoteSSSAnimations.BuildSettings(player);
        if (emoteSettings != null)
            combined.AddRange(emoteSettings);

        if (combined.Count == 0)
        {
            ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub, Array.Empty<ServerSpecificSettingBase>());
            return;
        }

        ServerSpecificSettingBase[] settingsArray = combined.ToArray();
        RegisterDefinedSettings(settingsArray);

        MEC.Timing.CallDelayed(0.2f, () =>
        {
            if (player?.ReferenceHub != null)
            {
                ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub, settingsArray);
            }
        });
    }

    public static void Clear(Player player)
    {
        if (player?.ReferenceHub == null)
            return;

        ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub, Array.Empty<ServerSpecificSettingBase>());
    }

    private static void RegisterDefinedSettings(ServerSpecificSettingBase[] settings)
    {
        if (ServerSpecificSettingsSync.DefinedSettings == null)
        {
            ServerSpecificSettingsSync.DefinedSettings = settings;
            return;
        }

        List<ServerSpecificSettingBase> currentDefined = new List<ServerSpecificSettingBase>(ServerSpecificSettingsSync.DefinedSettings);

        foreach (var setting in settings)
        {
            bool exists = false;
            for (int i = 0; i < currentDefined.Count; i++)
            {
                if (currentDefined[i].SettingId == setting.SettingId)
                {
                    currentDefined[i] = setting;
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                currentDefined.Add(setting);
            }
        }

        ServerSpecificSettingsSync.DefinedSettings = currentDefined.ToArray();
    }
}