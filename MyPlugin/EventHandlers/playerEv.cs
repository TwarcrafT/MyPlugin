using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using UnityEngine; // For Physics.Raycast
using MyPlugin; //config
using Exiled.API.Enums; // For ItemType

namespace MyPlugin.EventHandlers;

public static class PlayerEv
{

    public static void Subscribe()
    {
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
        if (MyPlugin.Instance.Config.Debug) Log.Debug("[PlayerEv] Subscribed to Player events."); // DEBUG LOG
    }


    public static void Unsubscribe()
    {
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
        if (MyPlugin.Instance.Config.Debug) Log.Debug("[PlayerEv] Unsubscribed from Player events."); // DEBUG LOG
    }

    private static void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (!MyPlugin.Instance.Config.keycardInfo.IsEnabled)
        {
            ev.Player?.ShowHint("", 0.1f); 
            return;
        }        if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnChangedItem] Player {ev.Player?.Nickname ?? "Unknown"} changed item to {ev.Item?.Type.ToString() ?? "nothing"}."); // DEBUG LOG

        if (ev.Item == null || ev.Player == null)
        {
            ev.Player?.ShowHint("", 0.1f); 
            if (MyPlugin.Instance.Config.Debug && ev.Player != null) Log.Debug($"[OnChangedItem] Item or Player is null, clearing hint for {ev.Player.Nickname}."); // DEBUG LOG
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
                Log.Debug($"[KeycardInfo] Keycard {ev.Item.Type} (Serial: {keycardSerial}) assigned to {ev.Player.Nickname} on first equip.");
            }
            else
            {
                config.KeycardOwners.TryGetValue(keycardSerial, out ownerName);
                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[KeycardInfo] Keycard {ev.Item.Type} (Serial: {keycardSerial}) already owned by {ownerName}."); // DEBUG LOG
            }

            if (config.KeycardMessages.TryGetValue(ev.Item.Type, out string messageTemplate))
            {
                string finalMessage = messageTemplate.Replace("%owner", ownerName);
                ev.Player.ShowHint(finalMessage, config.HintDuration);
                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[KeycardInfo] Displaying hint for {ev.Player.Nickname}: '{finalMessage}' for {config.HintDuration}s."); // DEBUG LOG
            }
            else
            {
                ev.Player.ShowHint("", 0.1f);
                if (MyPlugin.Instance.Config.Debug) Log.Debug($"[KeycardInfo] No message template for keycard type {ev.Item.Type}, clearing hint."); // DEBUG LOG
            }
        }
        else
        {
            ev.Player.ShowHint("", 0.1f);
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnChangedItem] Item {ev.Item.Type} is not a keycard, clearing hint for {ev.Player.Nickname}."); // DEBUG LOG
        }
    }


    private static void OnInteractingDoor(InteractingDoorEventArgs ev)
    {
        if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnInteractingDoor] Player {ev.Player?.Nickname ?? "Unknown"} interacting with door {ev.Door?.Name ?? "Unknown"}. IsAllowed: {ev.IsAllowed}."); // DEBUG LOG

        if (!ev.IsAllowed)
        {
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnInteractingDoor] Interaction already disallowed for {ev.Player?.Nickname}."); // DEBUG LOG
            return;
        }

        if (!MyPlugin.Instance.Config.doorButtonOpen.EnabledRaycast)
        {
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnInteractingDoor] Raycast is disabled in config, allowing interaction for {ev.Player?.Nickname}."); // DEBUG LOG
            return;
        }

        if (!Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward,
                             out var raycastHit,
                             30f, ~(1 << 1 | 1 << 13 | 1 << 16 | 1 << 28)))
        {
            ev.IsAllowed = false;
            ev.Player.ShowHint(MyPlugin.Instance.Config.doorButtonOpen.DeclineMessage, 5);
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnInteractingDoor] Raycast failed for {ev.Player?.Nickname}. Object not found, disallowing interaction. Hint: '{MyPlugin.Instance.Config.doorButtonOpen.DeclineMessage}'."); // DEBUG LOG
            return;
        }

        ev.IsAllowed = raycastHit.collider.gameObject.name.Contains("TouchScreenPanel") ||
                       raycastHit.collider.gameObject.name.Contains("collider") ||
                       raycastHit.collider.gameObject.name.Contains("CheckpointKeycardScreen") ||
                       raycastHit.collider.gameObject.name.Contains("HczButton") ||
                       raycastHit.collider.gameObject.name.Contains("TouchScreenPanel(1)") ||
                       raycastHit.collider.gameObject.name.Contains("HczButton(1)") ||
                       raycastHit.collider.gameObject.name.Contains("CheckpointKeycardScreen(1)") ||
                       raycastHit.collider.gameObject.name.Contains("KeycardScanner(1)") ||
                       raycastHit.collider.gameObject.name.Contains("KeycardScanner");

        if (ev.IsAllowed)
        {
            ev.Player.ShowHint(MyPlugin.Instance.Config.doorButtonOpen.SuccessMessage, 5);
            
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnInteractingDoor] Raycast hit object '{raycastHit.collider.gameObject.name}'. Allowing interaction for {ev.Player?.Nickname}. Hint: '{MyPlugin.Instance.Config.doorButtonOpen.SuccessMessage}'."); // DEBUG LOG
        }
        else
        {
            ev.Player.ShowHint(MyPlugin.Instance.Config.doorButtonOpen.DeclineMessage, 5);
            if (MyPlugin.Instance.Config.Debug) Log.Debug($"[OnInteractingDoor] Raycast hit object '{raycastHit.collider.gameObject.name}' but it's not a button. Disallowing interaction for {ev.Player?.Nickname}. Hint: '{MyPlugin.Instance.Config.doorButtonOpen.DeclineMessage}'."); // DEBUG LOG
        }
    }
}