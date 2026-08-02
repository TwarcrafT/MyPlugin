using InventorySystem.Items.Jailbird;
using InventorySystem.Items.Scp1509;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace MyPlugin.EventHandlers;

public static class PlayerEv
{
    public static void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        {
            if (!ev.IsAllowed || !MyPlugin.Instance.Config.DoorBtnCfg.EnabledRaycast)
            {
                return;
            }

            if (!Physics.Raycast(ev.Player.Camera.transform.position, ev.Player.Camera.transform.forward,
                out var raycastHit, 30f, ~(1 << 1 | 1 << 13 | 1 << 16 | 1 << 28)))
            {
                ev.IsAllowed = false;
                if (!MyPlugin.Instance.Config.DoorBtnCfg.EnabledHints)
                {
                    ev.Player.SendHint(MyPlugin.Instance.Config.DoorBtnCfg.DeclineMessage, 5);
                }
                return;
            }
            // Log the object name what the raycast hit, the distance, and the hit point
            Logger.Debug($"[DEBUG] Raycast hit object: {raycastHit.collider.gameObject.name}");
            Logger.Debug($"[DEBUG] Hit distance: {raycastHit.distance}");
            Logger.Debug($"[DEBUG] Hit point: {raycastHit.point}");

            bool isButton = raycastHit.collider.gameObject.name.Contains("TouchScreenPanel") ||
                           raycastHit.collider.gameObject.name.Contains("collider") ||
                           raycastHit.collider.gameObject.name.Contains("CheckpointKeycardScreen") ||
                           raycastHit.collider.gameObject.name.Contains("HczButton") ||
                           raycastHit.collider.gameObject.name.Contains("TouchScreenPanel(1)") ||
                           raycastHit.collider.gameObject.name.Contains("HczButton(1)") ||
                           raycastHit.collider.gameObject.name.Contains("CheckpointKeycardScreen(1)") ||
                           raycastHit.collider.gameObject.name.Contains("KeycardScanner(1)") ||
                           raycastHit.collider.gameObject.name.Contains("KeycardScanner");
            ev.IsAllowed = isButton;

            if (MyPlugin.Instance.Config.DoorBtnCfg.EnabledHints)
            {
                if (ev.IsAllowed)
                {
                    ev.Player.SendHint(MyPlugin.Instance.Config.DoorBtnCfg.SuccessMessage, 5);
                }
                else
                {
                    ev.Player.SendHint(MyPlugin.Instance.Config.DoorBtnCfg.DeclineMessage, 5);
                }
            }
        }
    }

    public static void OnChangingRole(PlayerChangingRoleEventArgs ev)
    {
        MyPlugin.Instance.RemovePlayerSchematics(ev.Player);
    }

    public static void OnLeft(PlayerLeftEventArgs ev)
    {
        MyPlugin.Instance.RemovePlayerSchematics(ev.Player);
    }

    public static void OnDied(PlayerDeathEventArgs ev)
    {
        MyPlugin.Instance.RemovePlayerSchematics(ev.Player);
    }

    public static void OnPlayerHurting(PlayerHurtingEventArgs ev)
    {
        // TODO : Handle player hurting event if needed
    }

    public static void OnJumped(PlayerJumpedEventArgs ev)
    {
        if (!MyPlugin.Instance.WearableSchematics.ContainsKey(ev.Player))
            return;

        var timings = MyPlugin.Instance.Config.WearableCfg.Timings;
        MyPlugin.Instance.PlayerJumpAnimation[ev.Player] = true;
        MyPlugin.Instance.JumpCooldowns[ev.Player] = Time.time + timings.JumpDuration;
    }

    public static void OnShootingWeapon(PlayerShootingWeaponEventArgs ev)
    {
        if (!MyPlugin.Instance.WearableSchematics.ContainsKey(ev.Player))
            return;

        var item = ev.Player.CurrentItem;
        var animCfg = PluginUtils.GetAnimationNames(ev.Player);
        string animationToPlay = animCfg.AttackFirearm;
        TriggerAttack(ev.Player, animationToPlay);
    }

    public static void OnJailbirdMessage(PlayerProcessedJailbirdMessageEventArgs ev)
    {
        if (!MyPlugin.Instance.WearableSchematics.ContainsKey(ev.Player))
            return;
        if (ev.Message == JailbirdMessageType.AttackTriggered)
        {
            var item = ev.Player.CurrentItem;
            var animCfg = PluginUtils.GetAnimationNames(ev.Player);
            string animationToPlay = animCfg.AttackJailbird;
            if (item != null && item.IsEquipped)
            {
                TriggerAttack(ev.Player, animationToPlay);
            }
        }
    }

    public static void OnScp1509Message(PlayerProcessedScp1509MessageEventArgs ev)
    {
        if (!MyPlugin.Instance.WearableSchematics.ContainsKey(ev.Player))
            return;
        if (ev.Message == Scp1509MessageType.AttackTriggered)
        {
            var item = ev.Player.CurrentItem;
            var animCfg = PluginUtils.GetAnimationNames(ev.Player);
            string animationToPlay = animCfg.Attack1509;
            if (item != null && item.IsEquipped)
            {
                TriggerAttack(ev.Player, animationToPlay);
            }
        }
    }

    public static void OnUsedItem(PlayerUsedItemEventArgs ev)
    {
        // TODO: Handle item usage if needed
    }

    public static void TriggerAttack(Player player, string animationName)
    {
        var timings = MyPlugin.Instance.Config.WearableCfg.Timings;
        MyPlugin.Instance.PlayerAttackAnimation[player] = animationName;
        MyPlugin.Instance.AttackCooldowns[player] = Time.time + timings.AttackDuration;
    }
}