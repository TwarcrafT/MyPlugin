using CommandSystem;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using LabApi.Features.Enums;
using LabApi.Features;
using MEC;
using MyPlugin.EventHandlers;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ICommand = CommandSystem.ICommand;
using Logger = LabApi.Features.Console.Logger;
using Player = LabApi.Features.Wrappers.Player;
using Mirror;

namespace MyPlugin.Command;

public class Wear : ICommand
{
    public string Command => "wearables";
    public string[] Aliases => new[] { ".wear" };
    public string Description => MyPlugin.Instance?.Config?.WearableCfg?.CommandDescription ?? "Equip a wearable schematic";

    private const string DelKeyword = "del";
    private const string WearableTag = "[WEAR]";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player senderPlayer = Player.Get(sender);
        var cfg = MyPlugin.Instance?.Config?.WearableCfg;

        if (senderPlayer == null)
        {
            response = "This command must be executed at the game level.";
            return false;
        }

        if (senderPlayer.Role == RoleTypeId.Spectator)
        {
            response = cfg.SpectatorBlocked;
            return false;
        }

        string[] args = new string[arguments.Count];
        Array.Copy(arguments.Array, arguments.Offset, args, 0, arguments.Count);

        if (args.Length == 0)
        {
            response = GetUsageAndList();
            return false;
        }

        Player targetPlayer = senderPlayer;
        int argStart = 0;

        if (int.TryParse(args[0], out int playerId))
        {
            Player found = Player.Get(playerId);
            if (found == null)
            {
                response = string.Format(cfg.PlayerNotFound, playerId);
                return false;
            }

            if (found.Role == RoleTypeId.Spectator)
            {
                response = cfg.TargetIsSpectator;
                return false;
            }

            targetPlayer = found;
            argStart = 1;
        }

        if (args.Length <= argStart)
        {
            response = GetUsageAndList();
            return false;
        }

        string firstRemaining = args[argStart];

        if (firstRemaining.Equals(DelKeyword, StringComparison.OrdinalIgnoreCase))
        {
            bool hadSchematic = MyPlugin.Instance.WearableSchematics.ContainsKey(targetPlayer);
            MyPlugin.Instance.RemovePlayerSchematics(targetPlayer);

            response = targetPlayer == senderPlayer
                ? (hadSchematic ? cfg.RemovedSelf : cfg.NothingToRemoveSelf)
                : (hadSchematic ? string.Format(cfg.RemovedTarget, targetPlayer.Nickname) : string.Format(cfg.NothingToRemoveTarget, targetPlayer.Nickname));
            return true;
        }

        MyPlugin.Instance.RemovePlayerSchematics(targetPlayer);

        var argumentsProvided = string.Join(" ", args.Skip(argStart));
        string schematicToUse = PluginUtils.FindSchematic($"!{argumentsProvided}", WearableTag);

        Vector3 spawnPos = targetPlayer.Position + Vector3.down;
        var schematic = ObjectSpawner.SpawnSchematic(schematicToUse, spawnPos, targetPlayer.Rotation);

        if (schematic == null)
        {
            response = cfg.NotFound;
            return false;
        }
        PluginUtils.MakePlayerInvisibleForPlayers(targetPlayer, Player.List, 0f);
        schematic.transform.parent = targetPlayer.GameObject.transform;
        targetPlayer.EnableEffect<Fade>(255, float.MaxValue);
        MyPlugin.Instance.WearableSchematics[targetPlayer] = schematic;
        MyPlugin.Instance.WearableSchematicNames[targetPlayer] = schematicToUse;
        MyPlugin.Instance.WearableCoroutines[targetPlayer] = Timing.RunCoroutine(AnimationSystem(targetPlayer, schematic, schematicToUse));

        PlayerSSSSync.Refresh(targetPlayer);

        {
            Timing.CallDelayed(0.1f, () =>
            {
                if (targetPlayer.IsAlive && schematic.isActiveAndEnabled)
                {
                    foreach (var netId in schematic.NetworkIdentities)
                    {
                        if (netId != null)
                        {
                            targetPlayer.Connection.Send(new ObjectDestroyMessage { netId = netId.netId });
                        }
                    }
                }
            });
        }

        response = targetPlayer == senderPlayer
            ? $"{cfg.Equipped} {argumentsProvided}"
            : $"{cfg.Equipped} {argumentsProvided} ({targetPlayer.Nickname})";
        return true;
    }

    private string GetUsageAndList()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Usage: .wear [PlayerID] <schematic/del>");
        builder.AppendLine();
        builder.Append(ListWearables());
        return builder.ToString();
    }

    private string ListWearables()
    {
        var cfg = MyPlugin.Instance.Config.WearableCfg;
        var builder = new StringBuilder();
        builder.AppendLine(cfg.ListHeader);

        var displayed = new HashSet<string>();
        bool foundAny = false;

        foreach (var (_, baseName) in PluginUtils.EnumerateSchematics(WearableTag))
        {
            if (displayed.Add(baseName))
            {
                builder.AppendLine($"- {baseName}");
                foundAny = true;
            }
        }

        return foundAny ? builder.ToString() : cfg.NoWearables;
    }
    private static Config.WearableConfig.AnimationNames GetAnimationNames(string schematicName)
    {
        var cfg = MyPlugin.Instance.Config.WearableCfg;

        if (!string.IsNullOrEmpty(schematicName) &&
            cfg.SchematicAnimations != null &&
            cfg.SchematicAnimations.TryGetValue(schematicName, out var names) &&
            names != null)
        {
            return names;
        }

        return new Config.WearableConfig.AnimationNames();
    }

    private IEnumerator<float> AnimationSystem(Player player, SchematicObject schematic, string schematicName)
    {
        var timings = MyPlugin.Instance.Config.WearableCfg.Timings;
        var animCfg = GetAnimationNames(schematicName);

        string currentAnimation = animCfg.Idle;
        Vector3 lastPosition = player.Position;

        SetAnimationState(schematic, currentAnimation, player, animCfg, timings, previousAnimation: null);

        while (MyPlugin.Instance.WearableSchematics.ContainsKey(player) && schematic != null)
        {
            string nextAnimation = DetermineAnimation(player, lastPosition, animCfg, timings);

            if (nextAnimation != currentAnimation)
            {
                SetAnimationState(schematic, nextAnimation, player, animCfg, timings, previousAnimation: currentAnimation);
                currentAnimation = nextAnimation;
            }

            lastPosition = player.Position;
            yield return Timing.WaitForSeconds(timings.CheckInterval);
        }

        MyPlugin.Instance.RemovePlayerSchematics(player);
    }

    private string DetermineAnimation(Player player, Vector3 lastPosition, Config.WearableConfig.AnimationNames animCfg, Config.WearableConfig.AnimationTimings timings)
    {
        if (MyPlugin.Instance.PlayerAttackAnimation.TryGetValue(player, out string attackAnim) && !string.IsNullOrEmpty(attackAnim))
        {
            if (MyPlugin.Instance.AttackCooldowns.TryGetValue(player, out float attackEndTime))
            {
                if (Time.time >= attackEndTime)
                {
                    MyPlugin.Instance.PlayerAttackAnimation.Remove(player);
                    MyPlugin.Instance.AttackCooldowns.Remove(player);
                }
                else
                {
                    return attackAnim;
                }
            }
            else
            {
                return attackAnim;
            }
        }

        if (MyPlugin.Instance.PlayerJumpAnimation.TryGetValue(player, out bool isJumping) && isJumping)
        {
            if (MyPlugin.Instance.JumpCooldowns.TryGetValue(player, out float jumpEndTime) && Time.time < jumpEndTime)
            {
                return animCfg.Jump;
            }

            MyPlugin.Instance.PlayerJumpAnimation[player] = false;
            MyPlugin.Instance.JumpCooldowns.Remove(player);
        }

        float distance = Vector3.Distance(player.Position, lastPosition);

        float verticalVelocity = 0f;
        try
        {
            verticalVelocity = player.Velocity.y;
        }
        catch { }

        if (verticalVelocity < timings.FallVelocityThreshold)
            return animCfg.Fall;

        if (distance >= timings.RunSpeedThreshold)
            return animCfg.Run;
        if (distance >= timings.CrouchMovementThreshold)
            return animCfg.Walk;
        if (distance > timings.MovementThreshold)
            return animCfg.Crouch;

        return animCfg.Idle;
    }

    private string ResolveFallback(string animationName, Config.WearableConfig.AnimationNames cfg)
    {
        if (animationName == cfg.Crouch) return cfg.Walk;
        if (animationName == cfg.Run) return cfg.Walk;
        if (animationName == cfg.Walk) return cfg.Idle;
        if (animationName == cfg.Fall) return cfg.Idle;
        if (animationName == cfg.Jump) return cfg.Idle;
        if (animationName == cfg.Attack1509) return cfg.Idle;
        if (animationName == cfg.AttackFirearm) return cfg.Idle;
        if (animationName == cfg.AttackJailbird) return cfg.Idle;
        return null;
    }

    private void SetAnimationState(SchematicObject schematic, string animationName, Player player, Config.WearableConfig.AnimationNames animCfg, Config.WearableConfig.AnimationTimings timings, string previousAnimation)
    {
        try
        {
            Animator animator = schematic.gameObject.GetComponentInChildren<Animator>();

            if (animator == null)
            {
                Logger.Error("Animator not found on wearable schematic!");
                return;
            }

            string nameToPlay = animationName;
            int safety = 0;

            while (!animator.HasState(0, Animator.StringToHash(nameToPlay)) && safety < 10)
            {
                string fallback = ResolveFallback(nameToPlay, animCfg);
                if (string.IsNullOrEmpty(fallback) || fallback == nameToPlay)
                {
                    Logger.Debug($"No animation state found for '{animationName}' (or its fallbacks) on animator.");
                    return;
                }

                Logger.Debug($"Animation state '{nameToPlay}' not found, falling back to '{fallback}'.");
                nameToPlay = fallback;
                safety++;
            }

            if (!string.IsNullOrEmpty(previousAnimation))
            {
                Logger.Debug($"[Animation] Changing animation for {player.Nickname}: '{previousAnimation}' -> '{nameToPlay}'");
            }
            else
            {
                Logger.Debug($"[Animation] Initial animation set for {player.Nickname}: '{nameToPlay}'");
            }

            animator.Play(nameToPlay, 0, 0f);

            if (nameToPlay == animCfg.Attack1509 || nameToPlay == animCfg.AttackFirearm || nameToPlay == animCfg.AttackJailbird)
            {
                float animationLength = timings.AttackDuration;

                try
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.IsName(nameToPlay))
                        animationLength = stateInfo.length;
                }
                catch
                {
                    Logger.Debug($"Could not get animation length, using default: {animationLength}s");
                }

                MyPlugin.Instance.AttackCooldowns[player] = Time.time + animationLength;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error in SetAnimationState: {ex}");
        }
    }

    public static void Register(Wear wearCommand, ICommand command, params CommandType[] commandTypes)
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