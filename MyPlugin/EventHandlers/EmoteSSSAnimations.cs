using LabApi.Features.Wrappers;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using Logger = LabApi.Features.Console.Logger;

namespace MyPlugin.EventHandlers;

public static class EmoteSSSAnimations
{
    private const int BaseSettingId = 80_000;
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

        if (!MyPlugin.Instance.SchematicsToDestroyCommand.TryGetValue(player, out SchematicObject schematic) || schematic?.gameObject == null)
            return null;

        Animator animator = schematic.gameObject.GetComponentInChildren<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
            return null;

        List<AnimationClip> clips = animator.runtimeAnimatorController.animationClips
            .Where(c => c != null)
            .GroupBy(c => c.name)
            .Select(g => g.First())
            .OrderBy(c => c.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (clips.Count == 0)
            return null;

        MyPlugin.Instance.EmoteAnimationClips[player] = clips;

        var settingsList = new List<ServerSpecificSettingBase>
        {
            new SSGroupHeader("Emote Animations")
        };

        for (int i = 0; i < clips.Count; i++)
        {
            settingsList.Add(new SSKeybindSetting(
                BaseSettingId + i,
                clips[i].name,
                KeyCode.None
            ));
        }

        return settingsList;
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

        if (!MyPlugin.Instance.EmoteAnimationClips.TryGetValue(player, out var clips) || clips == null)
            return;

        int index = keybind.SettingId - BaseSettingId;
        if (index < 0 || index >= clips.Count)
            return;

        if (!MyPlugin.Instance.SchematicsToDestroyCommand.TryGetValue(player, out var schematic) || schematic == null || schematic.gameObject == null)
            return;

        if (MyPlugin.Instance.EmoteAnimationCooldowns.TryGetValue(player, out float endTime) && Time.time < endTime)
            return;

        Animator animator = schematic.gameObject.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Logger.Error("Animator not found on emote schematic!");
            return;
        }

        var clip = clips[index];

        Logger.Debug($"[SSS] Playing emote animation: {clip.name} (Duration: {clip.length}s) for player: {player.Nickname}");

        animator.Play(clip.name, 0, 0f);
        MyPlugin.Instance.EmoteAnimationCooldowns[player] = Time.time + clip.length;
    }
}