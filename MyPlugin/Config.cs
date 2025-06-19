using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;
using PlayerRoles;
using Exiled.API.Enums; // Required for ItemType
using UnityEngine;
using System;
// using MyPlugin.CustomItems; 

namespace MyPlugin;

public class Config : IConfig
{
    [Description("If the plugin is enabled.")]
    public bool IsEnabled { get; set; } = true;

    [Description("If the debug mode is enabled.")]
    public bool Debug { get; set; } = false;

    public Emotes emotes { get; set; } = new();
    public DoorButtonOpen doorButtonOpen { get; set; } = new();
    public KeycardInfo keycardInfo { get; set; } = new();
    // public CustomItem1K CustomItem1 { get; set; } = new CustomItem1K(); // Item model
    // public CustomItem2K CustomItem2 { get; set; } = new CustomItem2K(); 


}
/*
public class CustomItem1K
{
    [Description("Item that will have ability of Custom Item")]
    public ItemType CustomItemType { get; set; } = ItemType.Painkillers;

    [Description("ID Of Custom item Custom Item")]
    public uint CustomItemTypeId { get; set; } = 1;

    [Description("Name Of Custom item Custom Item")]
    public string CustomItemName { get; set; } = "Amnestics Type A";

    [Description("Description Of Custom item Custom Item")]
    public string CustomItemDs { get; set; } = "Medicine that will make someone forget 6 - 10 hours";

    [Description("Scale Of Custom item Custom Item")]
    public Vector3 CustomItemScale { get; set; } = new Vector3(1.5f, 1.5f, 1.5f);

    [Description("Broadcast messeage after consuming Custom Item")]
    public String CustomItemBroadcastMesseage { get; set; } = "You ate Amnestics A and forgot 6 - 10 hours";

    [Description("Broadcast time after consuming Custom Item")]
    public ushort CustomItemBroadcastTime { get; set; } = 7;

    [Description("Effect type to give player after consuming Custom Item")]
    public EffectType[] CustomItemEffectTypes { get; set; } =
    {
                EffectType.Bleeding,
                EffectType.AmnesiaVision,
                EffectType.Deafened
            };

    [Description("Add effect time to effects you added before")]
    public float[] CustomItemEffectTime { get; set; } =
    {
                3,
                3,
                8,
            };

    [Description("Add effect intensity to effects you added before")]
    public byte[] CustomItemEffectIntensity { get; set; } =
    {
                7,
                7,
                2,
            };

    [Description("If player alrady has effect you will add more time to it If yes true if not false")]
    public bool[] CustomItemEffectAddDuration { get; set; } =
    {
                false,
                false,
                true,
            };

    [Description("Add late time to this meaning it will execute in like: 0.5s")]
    public float[] CustomItemEffectAddLateDuration { get; set; } =
    {
                0f,
                0.5f,
                1f,
            };

    [Description("Custom MER model you want to use for your custom item")]
    public string Model { get; set; } = "BottleBlue";
}

public class CustomItem2K
{
    [Description("Item that will have ability of Custom Item")]
    public ItemType CustomItemType { get; set; } = ItemType.Medkit;

    [Description("ID Of Custom item Custom Item")]
    public uint CustomItemTypeId { get; set; } = 2;

    [Description("Name Of Custom item Custom Item  ")]
    public string CustomItemName { get; set; } = "Amnestics Type B";

    [Description("Description Of Custom item Custom Item")]
    public string CustomItemDs { get; set; } = "Medicine that will make someone forget 6 - 10 hours";

    [Description("Scale Of Custom item Custom Item")]
    public Vector3 CustomItemScale { get; set; } = new Vector3(1.5f, 1.5f, 1.5f);

    [Description("Broadcast messeage after consuming Custom Item  ")]
    public String CustomItemBroadcastMesseage { get; set; } = "You ate Amnestics A and forgot 6 - 10 hours";

    [Description("Broadcast time after consuming Custom Item")]
    public ushort CustomItemBroadcastTime { get; set; } = 7;

    [Description("Effect type to give player after consuming Custom Item")]
    public EffectType[] CustomItemEffectTypes { get; set; } =
    {
                EffectType.SeveredEyes,
                EffectType.AmnesiaVision,
                EffectType.AntiScp207,
            };

    [Description("Add effect time to effects you added before")]
    public float[] CustomItemEffectTime { get; set; } =
    {
                3,
                3,
                8,
            };

    [Description("Add effect intensity to effects you added before")]
    public byte[] CustomItemEffectIntensity { get; set; } =
    {
                7,
                7,
                255,
            };

    [Description("If player alrady has effect you will add more time to it If yes true if not false")]
    public bool[] CustomItemEffectAddDuration { get; set; } =
    {
                false,
                false,
                true,
            };

    [Description("Add late time to this meaning it will execute in like: 0.5s")]
    public float[] CustomItemEffectAddLateDuration { get; set; } =
    {
                0f,
                0.5f,
                10f,
            };

    [Description("Custom MER model you want to use for your custom item")]
    public string Model { get; set; } = "MedkitBag";
}
*/
public class Emotes
{
    [Description("The default name for the emote command")]
    public string CommandName { get; set; } = "me";

    [Description("Aliases for the emote command.")]
    public string CommandAlias { get; set; } = ".m";

    [Description("The description for this command.")]
    public string CommandDescription { get; set; } = "Trigger emotes";

    [Description("The text shown in the console when listing animations.")]
    public string ListOfAnimations { get; set; } = "List of animations";

    [Description("The message displayed when command arguments are empty.")]
    public string EmptyAnwer { get; set; } = "\nTyping .me alone removes the current emote.\nUsage: .me AnimationName";

    [Description("The message displayed when a player lacks permission to use an emote.")]
    public string NoPermission { get; set; } =
        "You do not have permission for this emote.\nType .me to see available emotes.";

    [Description("The message displayed when a player successfully plays an animation.")]
    public string PlayedAnimation { get; set; } = "You played emote:";

    [Description("List of dictionaries mapping permission names to roles. Example: 'FG' for Facility Guards.")]
    public Dictionary<string, List<RoleTypeId>> Permission { get; set; } = new Dictionary<string, List<RoleTypeId>>
    {
        { "NONE", new List<RoleTypeId> { RoleTypeId.None } },
        { "FG", new List<RoleTypeId>
            {
                RoleTypeId.FacilityGuard,
            }
        },
        { "SC", new List<RoleTypeId> { RoleTypeId.Scientist } },
        { "CD", new List<RoleTypeId> { RoleTypeId.ClassD } },
        { "CH", new List<RoleTypeId>
            {
                RoleTypeId.ChaosConscript,
                RoleTypeId.ChaosMarauder,
                RoleTypeId.ChaosRepressor,
                RoleTypeId.ChaosRifleman
            }
        },
        { "NTF", new List<RoleTypeId>
            {
                RoleTypeId.NtfCaptain,
                RoleTypeId.NtfPrivate,
                RoleTypeId.NtfSergeant,
                RoleTypeId.NtfSpecialist
            }

        },
        { "SCP", new List<RoleTypeId>
             {
                 RoleTypeId.Scp049, RoleTypeId.Scp0492, RoleTypeId.Scp079, RoleTypeId.Scp3114,
                 RoleTypeId.Scp096, RoleTypeId.Scp106, RoleTypeId.Scp173, RoleTypeId.Scp939
             }
        }
    };
}

public class DoorButtonOpen
{
    [Description("If disabled, players do not need to look at the door button to open doors.")]
    public bool EnabledRaycast { get; set; } = false;

    [Description("Message shown when the player successfully opens a door.")]
    public string SuccessMessage { get; set; } = "The door opened";

    [Description("Message shown when the player was not looking at the door button  .")]
    public string DeclineMessage { get; set; } = "Look at the button";
}

public class KeycardInfo
{
    public bool IsEnabled { get; set; } = false;

    [Description("Messages displayed for each keycard type. '%owner' will be replaced by the keycard's assigned owner.")]
    public Dictionary<ItemType, string> KeycardMessages { get; set; } = new Dictionary<ItemType, string>
    {
        { ItemType.KeycardJanitor, "<color=#B200FF>Owner: %owner\nDepartment: Janitor</color>" },
        { ItemType.KeycardScientist, "<color=#F9FF71>Owner: %owner\nDepartment: Scientist</color>" },
        { ItemType.KeycardZoneManager, "<color=#00FF0C>Owner: %owner\nDepartment: ZoneManager</color>" },
        { ItemType.KeycardResearchCoordinator, "<color=#F3FF00>Owner: %owner\nDepartment: Research Coordinator</color>" },
        { ItemType.KeycardContainmentEngineer, "<color=#AD7B36>Owner: %owner\nDepartment: Containment Engineer</color>" },
        { ItemType.KeycardGuard, "<color=#5D5D5D>Owner: %owner\nDepartment: Security</color>" },
        { ItemType.KeycardMTFPrivate, "<color=#00FBFF>Owner: %owner\nDepartment: MTF Private</color>" },
        { ItemType.KeycardMTFOperative, "<color=#0087FF>Owner: %owner\nDepartment: MTF Operative</color>" },
        { ItemType.KeycardMTFCaptain, "<color=#002BFF>Owner: %owner\nDepartment: MTF Captain</color>" },
        { ItemType.KeycardFacilityManager, "<color=#FF0000>Owner: %owner\nDepartment: Facility Manager</color>" },
        { ItemType.KeycardChaosInsurgency, "<color=#003B01>Owner: ???\nDepartment: ???</color>" },
        { ItemType.KeycardO5, "<color=#000000>Owner: ???\nDepartment: ???</color>" },
    };

    [Description("Duration in seconds for which the hint message is displayed.")]
    public int HintDuration { get; set; } = 5;

    public Dictionary<ushort, string> KeycardOwners { get; set; } = new Dictionary<ushort, string>();
}