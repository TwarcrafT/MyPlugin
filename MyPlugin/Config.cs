using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;
using PlayerRoles;
using Exiled.API.Enums;
using UnityEngine;
using System;

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

    public class Emotes
    {
        public string CommandName { get; set; } = "me";
        public string CommandAlias { get; set; } = ".m";
        public string CommandDescription { get; set; } = "Trigger emotes";
        public string ListOfAnimations { get; set; } = "List of animations";
        public string EmptyAnwer { get; set; } = "\nTyping .me alone removes the current emote.\nUsage: .me AnimationName";
        public string NoPermission { get; set; } = "You do not have permission for this emote.\nType .me to see available emotes.";
        public string PlayedAnimation { get; set; } = "You played emote:";

        public Dictionary<string, List<RoleTypeId>> Permission { get; set; } = new Dictionary<string, List<RoleTypeId>>
        {
            { "NONE", new List<RoleTypeId> { RoleTypeId.None } },
            { "FG", new List<RoleTypeId> { RoleTypeId.FacilityGuard } },
            { "SC", new List<RoleTypeId> { RoleTypeId.Scientist } },
            { "CD", new List<RoleTypeId> { RoleTypeId.ClassD } },
            { "CH", new List<RoleTypeId> { RoleTypeId.ChaosConscript, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor, RoleTypeId.ChaosRifleman } },
            { "NTF", new List<RoleTypeId> { RoleTypeId.NtfCaptain, RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist } },
            { "SCP", new List<RoleTypeId> { RoleTypeId.Scp049, RoleTypeId.Scp0492, RoleTypeId.Scp079, RoleTypeId.Scp3114, RoleTypeId.Scp096, RoleTypeId.Scp106, RoleTypeId.Scp173, RoleTypeId.Scp939 } }
        };
    }

    public class DoorButtonOpen
    {
        [Description("If disabled, players do not need to look at the door button to open doors.")]
        public bool EnabledRaycast { get; set; } = false; 

        [Description("If enabled, hints for success/failure will not be shown to the player.")]
        public bool DisableHints { get; set; } = true; 

        public string SuccessMessage { get; set; } = "The door opened";
        public string DeclineMessage { get; set; } = "Look at the button";
    }

    public class KeycardInfo
    {
        public bool IsEnabled { get; set; } = false;
        public Dictionary<ItemType, string> KeycardMessages { get; set; } = new Dictionary<ItemType, string>();
        public int HintDuration { get; set; } = 5;
        public Dictionary<ushort, string> KeycardOwners { get; set; } = new Dictionary<ushort, string>();
    }
}