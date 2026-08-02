using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using Logger = LabApi.Features.Console.Logger;

namespace MyPlugin.EventHandlers;

public static class SSSCustomAnimations
{
    private const int BaseSettingId = 90_000;
    private const int MaxRangeSize = 1_000;

    public static void Register()
    {
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
    }

    public static void Unregister()
    {
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
    }

    public static List<ServerSpecificSettingBase> BuildSettings(Player player)
    {
        if (player?.ReferenceHub == null)
            return null;

        if (!MyPlugin.Instance.WearableSchematicNames.TryGetValue(player, out string equippedSchematicName))
            return null;

        var customAnimations = MyPlugin.Instance?.Config?.WearableCfg?.CustomAnimations;
        if (customAnimations == null || customAnimations.Count == 0)
            return null;

        var entryKey = FindMatchingKey(customAnimations.Keys, equippedSchematicName);

        if (string.IsNullOrEmpty(entryKey) || !customAnimations.TryGetValue(entryKey, out var names) || names == null || names.Count == 0)
            return null;

        var settingsList = new List<ServerSpecificSettingBase>
        {
            new SSGroupHeader("Additional Animations")
        };

        for (int i = 0; i < names.Count; i++)
        {
            settingsList.Add(new SSKeybindSetting(
                BaseSettingId + i,
                names[i],
                KeyCode.None
            ));
        }

        return settingsList;
    }

    private static string FindMatchingKey(IEnumerable<string> keys, string schematicName)
    {
        return keys.FirstOrDefault(k =>
            k.Equals(schematicName, StringComparison.OrdinalIgnoreCase) ||
            schematicName.EndsWith(k, StringComparison.OrdinalIgnoreCase) ||
            k.EndsWith(schematicName, StringComparison.OrdinalIgnoreCase));
    }

    private static float GetClipLength(Animator animator, string clipName)
    {
        if (animator?.runtimeAnimatorController == null)
            return 0f;

        var clip = animator.runtimeAnimatorController.animationClips
            .FirstOrDefault(c => c != null && c.name.Equals(clipName, StringComparison.OrdinalIgnoreCase));

        return clip != null ? clip.length : 0f;
    }

    private static void OnSettingValueReceived(ReferenceHub referenceHub, ServerSpecificSettingBase settingBase)
    {
        if (settingBase is not SSKeybindSetting keybind)
            return;

        if (keybind.SettingId < BaseSettingId || keybind.SettingId >= BaseSettingId + MaxRangeSize)
            return;

        if (!keybind.SyncIsPressed)
            return;

        Player player = Player.Get(referenceHub);
        if (player == null)
            return;

        if (!MyPlugin.Instance.WearableSchematicNames.TryGetValue(player, out string equippedSchematic))
            return;

        var customAnimations = MyPlugin.Instance?.Config?.WearableCfg?.CustomAnimations;
        if (customAnimations == null)
            return;

        var entryKey = FindMatchingKey(customAnimations.Keys, equippedSchematic);
        if (string.IsNullOrEmpty(entryKey) || !customAnimations.TryGetValue(entryKey, out var names) || names == null)
            return;

        int index = keybind.SettingId - BaseSettingId;
        if (index < 0 || index >= names.Count)
            return;

        string animationName = names[index];

        if (!MyPlugin.Instance.WearableSchematics.TryGetValue(player, out var schematic) || schematic == null || schematic.gameObject == null)
            return;

        Animator animator = schematic.gameObject.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Logger.Error("Animator not found on wearable schematic!");
            return;
        }

        float duration = GetClipLength(animator, animationName);

        Logger.Debug($"[SSS] Received keybind for animation: {animationName} (Duration: {duration}s) for player: {player.Nickname}");

        MyPlugin.Instance.PlayerAttackAnimation[player] = animationName;
        MyPlugin.Instance.AttackCooldowns[player] = Time.time + duration;
    }
}