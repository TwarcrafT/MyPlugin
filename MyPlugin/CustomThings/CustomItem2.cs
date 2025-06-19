/*
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.CustomItems.API.Features;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Spawn;
using Exiled.API.Enums;
using ProjectMER;
using CustomPlayerEffects;
using MEC;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features;
using ProjectMER.Features.Serializable.Schematics;

namespace MyPlugin.CustomItems
{
    [CustomItem(ItemType.Medkit)]
    public class CustomItem2 : CustomItem
    {
        // Static fields for configuration
        private static ItemType customItemType = MyPlugin.Instance.Config.CustomItem2.CustomItemType;
        private static uint customItemTypeId = MyPlugin.Instance.Config.CustomItem2.CustomItemTypeId;
        private static string CustomItemName = MyPlugin.Instance.Config.CustomItem2.CustomItemName;
        private static string CustomItemDs = MyPlugin.Instance.Config.CustomItem2.CustomItemDs;
        private static Vector3 CustomItemScale = MyPlugin.Instance.Config.CustomItem2.CustomItemScale;
        private static string ConfigModelName = MyPlugin.Instance.Config.CustomItem2.Model;
        private static string CustomItemBroadcastMesseage = MyPlugin.Instance.Config.CustomItem2.CustomItemBroadcastMesseage;
        private static ushort CustomItemBroadcastTime = MyPlugin.Instance.Config.CustomItem2.CustomItemBroadcastTime;
        private static EffectType[] CustomItemEffectTypes = MyPlugin.Instance.Config.CustomItem2.CustomItemEffectTypes;
        private static float[] CustomItemEffectTime = MyPlugin.Instance.Config.CustomItem2.CustomItemEffectTime;
        private static byte[] CustomItemEffectIntensity = MyPlugin.Instance.Config.CustomItem2.CustomItemEffectIntensity;
        private static bool[] CustomItemEffectAddDuration = MyPlugin.Instance.Config.CustomItem2.CustomItemEffectAddDuration;
        private static float[] CustomItemEffectAddLateDuration = MyPlugin.Instance.Config.CustomItem2.CustomItemEffectAddLateDuration;

        // Usage tracking
        private static Dictionary<ushort, byte> itemUses = new Dictionary<ushort, byte>();
        private const byte MAX_USES = 3;


        public override ItemType Type { get; set; } = customItemType;
        public override uint Id { get; set; } = customItemTypeId;

        public override string Name { get; set; } = CustomItemName;

        public override string Description { get; set; } = CustomItemDs;

        public override float Weight { get; set; } = 1f;


        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties()
        {
            DynamicSpawnPoints = new List<DynamicSpawnPoint>()
            {
                new DynamicSpawnPoint()
                {
                    Location = Exiled.API.Enums.SpawnLocationType.InsideLczArmory,
                    Chance = 100
                }
            }
        };
        public override Vector3 Scale { get; set; } = CustomItemScale;

        public override bool ShouldMessageOnGban => base.ShouldMessageOnGban;

        protected override void SubscribeEvents()
        {
            Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
            Exiled.Events.Handlers.Map.PickupAdded += OnPickupAdded;
            Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            base.SubscribeEvents();
        }

        protected override void UnsubscribeEvents()
        {
            Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
            Exiled.Events.Handlers.Map.PickupAdded -= OnPickupAdded;
            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            base.UnsubscribeEvents();
        }

        private void OnUsingItem(Exiled.Events.EventArgs.Player.UsingItemEventArgs ev)
        {
            if (!Check(ev.Item))
                return;

            ev.IsAllowed = false; // Prevent default item consumption

            // Get current uses or initialize if not found
            if (!itemUses.TryGetValue(ev.Item.Serial, out byte usesLeft))
            {
                Log.Warn($"CustomItem2 (Serial: {ev.Item.Serial}) used but not found in uses dictionary. Initializing with max uses.");
                usesLeft = MAX_USES;
                itemUses[ev.Item.Serial] = usesLeft;
            }

            // Check if item has uses left
            if (usesLeft <= 0)
            {
                ev.Player.Broadcast(new Exiled.API.Features.Broadcast($"{CustomItemName} has no uses left!", 3));
                ev.Player.RemoveItem(ev.Item);
                itemUses.Remove(ev.Item.Serial);
                return;
            }

            // Consume one use
            usesLeft--;
            itemUses[ev.Item.Serial] = usesLeft;

            Log.Debug($"CustomItem2 (Serial: {ev.Item.Serial}) used. {usesLeft} uses remaining.");

            // Show usage information to player
            string usageMessage = $"{CustomItemName} used! ({usesLeft}/{MAX_USES} uses remaining)";
            if (usesLeft == 0)
            {
                usageMessage = $"{CustomItemName} used! (Item will be consumed)";
            }
            ev.Player.Broadcast(new Exiled.API.Features.Broadcast(usageMessage, 3));

            // Show custom broadcast message
            if (!string.IsNullOrEmpty(CustomItemBroadcastMesseage))
            {
                ev.Player.Broadcast(new Exiled.API.Features.Broadcast(CustomItemBroadcastMesseage, CustomItemBroadcastTime));
            }

            // Apply effects
            ApplyEffects(ev.Player);

            // Remove item if no uses left
            if (usesLeft <= 0)
            {
                Log.Debug($"CustomItem2 (Serial: {ev.Item.Serial}) exhausted all uses. Removing item.");
                Timing.CallDelayed(0.1f, () => {
                    ev.Player.RemoveItem(ev.Item);
                    itemUses.Remove(ev.Item.Serial);
                });
            }
        }

        private void ApplyEffects(Player player)
        {
            if (CustomItemEffectTypes == null || CustomItemEffectTypes.Length == 0)
                return;

            for (int i = 0; i < CustomItemEffectTypes.Length; i++)
            {
                if (i >= CustomItemEffectTime.Length ||
                    i >= CustomItemEffectIntensity.Length ||
                    i >= CustomItemEffectAddDuration.Length ||
                    i >= CustomItemEffectAddLateDuration.Length)
                {
                    Log.Warn($"CustomItem2: Effect configuration array mismatch at index {i}. Stopping effect application.");
                    break;
                }

                EffectType effectType = CustomItemEffectTypes[i];
                float effectTime = CustomItemEffectTime[i];
                byte effectIntensity = CustomItemEffectIntensity[i];
                bool addDuration = CustomItemEffectAddDuration[i];
                float lateDuration = CustomItemEffectAddLateDuration[i];

                if (lateDuration > 0)
                {
                    Timing.CallDelayed(lateDuration, () =>
                    {
                        if (player != null && player.IsAlive)
                        {
                            player.EnableEffect(effectType, effectIntensity, effectTime, addDuration);
                        }
                    });
                }
                else
                {
                    player.EnableEffect(effectType, effectIntensity, effectTime, addDuration);
                }
            }
        }

        private void OnPickingUpItem(Exiled.Events.EventArgs.Player.PickingUpItemEventArgs ev)
        {
            if (ev.Player == null || ev.Pickup == null)
                return;

            if (!Check(ev.Pickup))
                return;

            // Initialize or get existing uses
            if (!itemUses.TryGetValue(ev.Pickup.Serial, out byte usesLeft))
            {
                usesLeft = MAX_USES;
                itemUses[ev.Pickup.Serial] = usesLeft;
                Log.Debug($"CustomItem2 (Serial: {ev.Pickup.Serial}) picked up by {ev.Player.Nickname}. Initialized with {MAX_USES} uses.");
            }
            else
            {
                Log.Debug($"CustomItem2 (Serial: {ev.Pickup.Serial}) picked up by {ev.Player.Nickname}. Has {usesLeft} uses remaining.");
            }

            // Notify player about uses
            string pickupMessage = $"Picked up {CustomItemName} ({usesLeft}/{MAX_USES} uses)";
            ev.Player.Broadcast(new Exiled.API.Features.Broadcast(pickupMessage, 3));

            // Clean up schematic if exists
            if (MyPlugin.Instance.SchematicsToDestroy.TryGetValue(ev.Pickup.Serial, out SchematicObject schematic))
            {
                schematic?.Destroy();
                MyPlugin.Instance.SchematicsToDestroy.Remove(ev.Pickup.Serial);
            }
        }

        private void OnDroppingItem(Exiled.Events.EventArgs.Player.DroppingItemEventArgs ev)
        {
            if (!Check(ev.Item))
                return;

            // Keep track of uses when item is dropped
            if (itemUses.TryGetValue(ev.Item.Serial, out byte usesLeft))
            {
                Log.Debug($"CustomItem2 (Serial: {ev.Item.Serial}) dropped by {ev.Player.Nickname}. Preserving {usesLeft} uses.");
            }
        }

        private void OnPickupAdded(Exiled.Events.EventArgs.Map.PickupAddedEventArgs ev)
        {
            if (ev.Pickup == null)
            {
                Log.Debug("OnPickupAdded (CustomItem2): Pickup is null.");
                return;
            }

            if (!Check(ev.Pickup))
            {
                Log.Debug("OnPickupAdded (CustomItem2): This is not the custom item.");
                return;
            }

            // Initialize uses for newly spawned items (not from drops)
            if (!itemUses.ContainsKey(ev.Pickup.Serial))
            {
                itemUses[ev.Pickup.Serial] = MAX_USES;
                Log.Debug($"OnPickupAdded (CustomItem2): Initialized new pickup (Serial: {ev.Pickup.Serial}) with {MAX_USES} uses.");
            }

            // Spawn schematic model
            if (string.IsNullOrEmpty(ConfigModelName))
            {
                Log.Debug("OnPickupAdded (CustomItem2): No model name configured, skipping schematic spawn.");
                return;
            }

            SchematicObject mySchematicsVar;
            try
            {
                mySchematicsVar = ObjectSpawner.SpawnSchematic(ConfigModelName, ev.Pickup.Position, ev.Pickup.GameObject.transform.rotation, new Vector3(1, 1, 1));
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to spawn schematic for CustomItem2 ('{ConfigModelName}'): {ex}");
                return;
            }

            if (mySchematicsVar == null)
            {
                Log.Warn("OnPickupAdded (CustomItem2): Spawned schematic object is null, possibly failed to spawn.");
                return;
            }

            mySchematicsVar.gameObject.transform.parent = ev.Pickup.GameObject.transform;
            Log.Debug($"OnPickupAdded (CustomItem2): Attached schematic '{mySchematicsVar.name}' to pickup '{ev.Pickup.Type}' (Serial: {ev.Pickup.Serial})");
            MyPlugin.Instance.SchematicsToDestroy[ev.Pickup.Serial] = mySchematicsVar;
        }

        // Utility method to get remaining uses for debugging
        public static byte GetRemainingUses(ushort serial)
        {
            return itemUses.TryGetValue(serial, out byte uses) ? uses : (byte)0;
        }

        // Method to clear all usage data (useful for cleanup)
        public static void ClearUsageData()
        {
            itemUses.Clear();
            Log.Debug("CustomItem2: Cleared all usage data.");
        }
    }
}
*/