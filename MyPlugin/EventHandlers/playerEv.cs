using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using UnityEngine;
using MyPlugin;
using Exiled.API.Enums;

namespace MyPlugin.EventHandlers;

public static class PlayerEv
{
    public static void Subscribe()
    {
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
    }

    public static void Unsubscribe()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
    }

    private static void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (!MyPlugin.Instance.Config.keycardInfo.IsEnabled)
        {
            ev.Player?.ShowHint("", 0.1f);
            return;
        }

        if (ev.Item == null || ev.Player == null)
        {
            ev.Player?.ShowHint("", 0.1f);
            return;
        }

        var config = MyPlugin.Instance.Config.keycardInfo;
        if (ev.Item.IsKeycard)
        {
            ushort keycardSerial = ev.Item.Serial;
            string ownerName = "Brak Właściciela";

            if (!config.KeycardOwners.ContainsKey(keycardSerial))
            {
                config.KeycardOwners[keycardSerial] = ev.Player.DisplayNickname;
                ownerName = ev.Player.DisplayNickname;
            }
            else
            {
                config.KeycardOwners.TryGetValue(keycardSerial, out ownerName);
            }

            if (config.KeycardMessages.TryGetValue(ev.Item.Type, out string messageTemplate))
            {
                string finalMessage = messageTemplate.Replace("%owner", ownerName);
                ev.Player.ShowHint(finalMessage, config.HintDuration);
            }
            else
            {
                ev.Player.ShowHint("", 0.1f);
            }
        }
        else
        {
            ev.Player.ShowHint("", 0.1f);
        }
    }

    private static void OnInteractingDoor(InteractingDoorEventArgs ev)
    {
        Log.Debug($"[DEBUG] OnInteractingDoor triggered for player {ev.Player.Nickname}");

        if (!ev.IsAllowed)
        {
            Log.Debug($"[DEBUG] Door interaction not allowed initially for {ev.Player.Nickname}");
            return;
        }

        if (!MyPlugin.Instance.Config.doorButtonOpen.EnabledRaycast)
        {
            Log.Debug($"[DEBUG] doorButtonOpen.EnabledRaycast is disabled");
            return;
        }

        Log.Debug($"[DEBUG] Starting raycast for player {ev.Player.Nickname}");
        Log.Debug($"[DEBUG] Camera position: {ev.Player.CameraTransform.position}");
        Log.Debug($"[DEBUG] Camera forward: {ev.Player.CameraTransform.forward}");

        if (!Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward,
            out var raycastHit, 30f, ~(1 << 1 | 1 << 13 | 1 << 16 | 1 << 28)))
        {
            Log.Debug($"[DEBUG] Raycast failed - no hit detected for {ev.Player.Nickname}");
            ev.IsAllowed = false;
            if (!MyPlugin.Instance.Config.doorButtonOpen.DisableHints)
                ev.Player.ShowHint(MyPlugin.Instance.Config.doorButtonOpen.DeclineMessage, 5);
            return;
        }

        Log.Debug($"[DEBUG] Raycast hit object: {raycastHit.collider.gameObject.name}");
        Log.Debug($"[DEBUG] Hit distance: {raycastHit.distance}");
        Log.Debug($"[DEBUG] Hit point: {raycastHit.point}");

        bool isButton = raycastHit.collider.gameObject.name.Contains("TouchScreenPanel") ||
                       raycastHit.collider.gameObject.name.Contains("collider") ||
                       raycastHit.collider.gameObject.name.Contains("CheckpointKeycardScreen") ||
                       raycastHit.collider.gameObject.name.Contains("HczButton") ||
                       raycastHit.collider.gameObject.name.Contains("TouchScreenPanel(1)") ||
                       raycastHit.collider.gameObject.name.Contains("HczButton(1)") ||
                       raycastHit.collider.gameObject.name.Contains("CheckpointKeycardScreen(1)") ||
                       raycastHit.collider.gameObject.name.Contains("KeycardScanner(1)") ||
                       raycastHit.collider.gameObject.name.Contains("KeycardScanner");

        Log.Debug($"[DEBUG] Is button detected: {isButton}");

        ev.IsAllowed = isButton;

        if (!MyPlugin.Instance.Config.doorButtonOpen.DisableHints)
        {
            if (ev.IsAllowed)
            {
                Log.Debug($"[DEBUG] Showing success message to {ev.Player.Nickname}");
                ev.Player.ShowHint(MyPlugin.Instance.Config.doorButtonOpen.SuccessMessage, 5);
            }
            else
            {
                Log.Debug($"[DEBUG] Showing decline message to {ev.Player.Nickname}");
                ev.Player.ShowHint(MyPlugin.Instance.Config.doorButtonOpen.DeclineMessage, 5);
            }
        }

        Log.Debug($"[DEBUG] Final ev.IsAllowed: {ev.IsAllowed}");
    }
}