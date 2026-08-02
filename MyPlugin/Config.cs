using PlayerRoles;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MyPlugin;

public class Config
{
    public bool IsEnabled { get; set; } = true;
    public bool Debug { get; set; } = false;

    public WearableConfig WearableCfg { get; set; } = new();
    public EmotesConfig EmotesCfg { get; set; } = new();
    public DoorBtnConfig DoorBtnCfg { get; set; } = new();

    public class DoorBtnConfig
    {
        [Description("If disabled, players do not need to look at the door button to open doors.")]
        public bool EnabledRaycast { get; set; } = false;
        public bool EnabledHints { get; set; } = false;
        public string SuccessMessage { get; set; } = "The door opened";
        public string DeclineMessage { get; set; } = "Look at the button";
    }

    public class WearableConfig
    {
        public string CommandDescription { get; set; } = "Equip a wearable schematic on yourself or another player.";
        public string ListHeader { get; set; } = "Available wearables:";
        public string NoWearables { get; set; } = "No wearable schematics are configured.";
        public string NotFound { get; set; } = "Failed to spawn the schematic.";
        public string Equipped { get; set; } = "Equipped wearable:";

        public string SpectatorBlocked { get; set; } = "Spectators can't use this command.";
        public string TargetIsSpectator { get; set; } = "That player is a spectator and can't wear anything.";
        public string PlayerNotFound { get; set; } = "Player with ID {0} not found.";
        public string RemovedSelf { get; set; } = "Removed your wearable schematic.";
        public string RemovedTarget { get; set; } = "Removed the wearable schematic from {0}.";
        public string NothingToRemoveSelf { get; set; } = "You don't have a wearable schematic equipped.";
        public string NothingToRemoveTarget { get; set; } = "{0} doesn't have a wearable schematic equipped.";

        public AnimationTimings Timings { get; set; } = new();

        public Dictionary<string, AnimationNames> SchematicAnimations { get; set; } = new()
        {
            ["!Steve[WEAR]"] = new AnimationNames(),
            ["!Test[WEAR]"] = new AnimationNames()
        };

        public Dictionary<string, List<string>> CustomAnimations { get; set; } = new()
        {
            ["!Steve[WEAR]"] = new List<string> { "Sit" },
            ["!Test[WEAR]"] = new List<string> { "Clap", "Dab" }
        };

        public class AnimationTimings
        {
            public float AttackDuration { get; set; } = 1.0f;
            public float JumpDuration { get; set; } = 0.35f;
            public float CheckInterval { get; set; } = 0.2f;
            public float MovementThreshold { get; set; } = 0.05f;
            public float RunSpeedThreshold { get; set; } = 1f;
            public float CrouchMovementThreshold { get; set; } = 0.3f;
            public float FallVelocityThreshold { get; set; } = -5f;
        }

        public class AnimationNames
        {
            public string Idle { get; set; } = "Idle";
            public string Walk { get; set; } = "Walk";
            public string Run { get; set; } = "Run";
            public string Crouch { get; set; } = "Crouch";
            public string Fall { get; set; } = "Fall";
            public string Jump { get; set; } = "Jump";
            public string Attack1509 { get; set; } = "Attack_1";
            public string AttackFirearm { get; set; } = "AttackFirearm";
            public string AttackJailbird { get; set; } = "Attack_2";
        }
    }

    public class EmotesConfig
    {
        public string CommandDescription { get; set; } = "Play an RP animation.";
        public string ListOfAnimations { get; set; } = "Available emotes:";
        public string EmptyAnwer { get; set; } = "Emotes";
        public string NoPermission { get; set; } = "No emote found with that name, or you don't have permission to use it.";
        public string PlayedAnimation { get; set; } = "Playing animation:";

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
}