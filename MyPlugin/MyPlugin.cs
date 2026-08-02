using CommandSystem.Commands.Shared;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using MEC;
using MyPlugin.Command;
using MyPlugin.EventHandlers;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace MyPlugin;

public class MyPlugin : Plugin<Config>
{
    public static MyPlugin Instance { get; private set; }

    public override string Name => "MyPlugin";
    public override string Author => "pawelek7650";
    public override Version Version => new Version(2, 0, 5);
    public override string Description => "MyPlugin is aimed at bringing some RP features for players";
    public override Version RequiredApiVersion => new Version(1, 1, 7);

    public Dictionary<Player, SchematicObject> SchematicsToDestroyCommand { get; } = new();
    public Dictionary<Player, SchematicObject> WearableSchematics { get; } = new();
    public Dictionary<Player, string> WearableSchematicNames { get; } = new();
    public Dictionary<Player, CoroutineHandle> WearableCoroutines { get; } = new();

    public Dictionary<Player, string> PlayerAttackAnimation { get; } = new();
    public Dictionary<Player, float> AttackCooldowns { get; } = new();

    public Dictionary<Player, bool> PlayerJumpAnimation { get; } = new();
    public Dictionary<Player, float> JumpCooldowns { get; } = new();

    public Dictionary<Player, List<AnimationClip>> EmoteAnimationClips { get; } = new();
    public Dictionary<Player, float> EmoteAnimationCooldowns { get; } = new();

    public override void Enable()
    {
        Instance = Instance ?? this;

        Me expressCommand = new Me();
        Me.Register(expressCommand, expressCommand, CommandType.Client);

        Wear wearCommand = new Wear();
        Wear.Register(wearCommand, wearCommand, CommandType.RemoteAdmin);

        SSSCustomAnimations.Register();
        EmoteSSSAnimations.Register();

        PlayerEvents.ChangingRole += PlayerEv.OnChangingRole;
        PlayerEvents.Left += PlayerEv.OnLeft;
        PlayerEvents.Death += PlayerEv.OnDied;
        PlayerEvents.InteractingDoor += PlayerEv.OnInteractingDoor;
        PlayerEvents.Hurting += PlayerEv.OnPlayerHurting;
        PlayerEvents.Jumped += PlayerEv.OnJumped;
        PlayerEvents.UsedItem += PlayerEv.OnUsedItem;
        PlayerEvents.ShootingWeapon += PlayerEv.OnShootingWeapon;
        PlayerEvents.ProcessedJailbirdMessage += PlayerEv.OnJailbirdMessage;
        PlayerEvents.ProcessedScp1509Message += PlayerEv.OnScp1509Message;
    }

    public override void Disable()
    {
        PlayerEvents.ChangingRole -= PlayerEv.OnChangingRole;
        PlayerEvents.Left -= PlayerEv.OnLeft;
        PlayerEvents.Death -= PlayerEv.OnDied;
        PlayerEvents.InteractingDoor -= PlayerEv.OnInteractingDoor;
        PlayerEvents.Hurting -= PlayerEv.OnPlayerHurting;
        PlayerEvents.Jumped -= PlayerEv.OnJumped;
        PlayerEvents.UsedItem -= PlayerEv.OnUsedItem;
        PlayerEvents.ShootingWeapon -= PlayerEv.OnShootingWeapon;
        PlayerEvents.ProcessedJailbirdMessage -= PlayerEv.OnJailbirdMessage;
        PlayerEvents.ProcessedScp1509Message -= PlayerEv.OnScp1509Message;

        SSSCustomAnimations.Unregister();
        EmoteSSSAnimations.Unregister();

        foreach (var player in WearableSchematics.Keys.Concat(SchematicsToDestroyCommand.Keys).Distinct().ToList())
            PlayerSSSSync.Clear(player);

        foreach (var schematic in SchematicsToDestroyCommand.Values)
        {
            if (schematic != null && schematic.gameObject != null)
                schematic.Destroy();
        }
        SchematicsToDestroyCommand.Clear();

        foreach (var kvp in WearableSchematics)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                kvp.Value.Destroy();
        }
        WearableSchematics.Clear();
        WearableSchematicNames.Clear();

        foreach (var coroutine in WearableCoroutines.Values)
        {
            Timing.KillCoroutines(coroutine);
        }
        WearableCoroutines.Clear();

        PlayerAttackAnimation.Clear();
        AttackCooldowns.Clear();
        PlayerJumpAnimation.Clear();
        JumpCooldowns.Clear();

        EmoteAnimationClips.Clear();
        EmoteAnimationCooldowns.Clear();

        Instance = null;
    }

    public void RemovePlayerSchematics(Player player)
    {
        if (SchematicsToDestroyCommand.TryGetValue(player, out SchematicObject s1))
        {
            if (s1 != null && s1.gameObject != null)
                s1.Destroy();
            SchematicsToDestroyCommand.Remove(player);
        }

        if (WearableSchematics.TryGetValue(player, out SchematicObject s2))
        {
            if (s2 != null && s2.gameObject != null)
                s2.Destroy();
            WearableSchematics.Remove(player);
        }

        if (WearableCoroutines.TryGetValue(player, out var coroutine))
        {
            Timing.KillCoroutines(coroutine);
            WearableCoroutines.Remove(player);
        }

        WearableSchematicNames.Remove(player);
        EmoteAnimationClips.Remove(player);
        EmoteAnimationCooldowns.Remove(player);

        PlayerSSSSync.Refresh(player);

        PlayerAttackAnimation.Remove(player);
        AttackCooldowns.Remove(player);
        PlayerJumpAnimation.Remove(player);
        JumpCooldowns.Remove(player);
    }
}