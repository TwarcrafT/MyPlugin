using System;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using System.Collections.Generic;
using Exiled.Events.EventArgs.Player;
using UnityEngine; 


namespace MyPlugin
{
    public class MyPlugin : Plugin<Config>
    {
        public static MyPlugin Instance { get; private set; }

        public override string Name => "MyPlugin";
        public override string Author => "pawelek7650"; 
        public override Version Version => new Version(2, 0, 1); 
        public override Version RequiredExiledVersion => new Version(9, 6, 0);

        public Dictionary<Player, ProjectMER.Features.Objects.SchematicObject> SchematicsToDestroyCommand { get; } =
            new Dictionary<Player, ProjectMER.Features.Objects.SchematicObject>();

        public override void OnEnabled()
        {
            Instance = this;
            RegisterEvents();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            UnRegisterEvents();

            foreach (var schematic in SchematicsToDestroyCommand.Values)
            {
                if (schematic != null && schematic.gameObject != null)
                {
                    schematic.Destroy();
                }
            }
            SchematicsToDestroyCommand.Clear();

            Instance = null;
            base.OnDisabled();
        }

        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.Died += OnDied; 
        }

        private void UnRegisterEvents()
        {
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.Died -= OnDied;
        }


        private void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (SchematicsToDestroyCommand.TryGetValue(ev.Player, out ProjectMER.Features.Objects.SchematicObject schematic))
            {
                if (schematic != null && schematic.gameObject != null)
                {
                    schematic.Destroy();
                }
                SchematicsToDestroyCommand.Remove(ev.Player);
                if (Config.Debug) Log.Debug($"[MyPlugin] [OnChangingRole] Destroyed schematic for {ev.Player.Nickname} due to role change.");
            }
        }

        private void OnLeft(LeftEventArgs ev)
        {
            if (SchematicsToDestroyCommand.TryGetValue(ev.Player, out ProjectMER.Features.Objects.SchematicObject schematic))
            {
                if (schematic != null && schematic.gameObject != null)
                {
                    schematic.Destroy();
                }
                SchematicsToDestroyCommand.Remove(ev.Player);
                if (Config.Debug) Log.Debug($"[MyPlugin] [OnLeft] Destroyed schematic for {ev.Player.Nickname} due to leaving.");
            }
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (Config.Debug) Log.Debug($"[MyPlugin] [OnDied] Player {ev.Player.Nickname} ({ev.Player.Id}) died. Checking for schematic.");

            if (SchematicsToDestroyCommand.TryGetValue(ev.Player, out ProjectMER.Features.Objects.SchematicObject schematic))
            {
                if (Config.Debug) Log.Debug($"[MyPlugin] [OnDied] Found schematic for {ev.Player.Nickname}. Destroying...");
                if (schematic != null && schematic.gameObject != null)
                {
                    schematic.Destroy(); 
                }
                SchematicsToDestroyCommand.Remove(ev.Player); 
                if (Config.Debug) Log.Debug($"[MyPlugin] [OnDied] Schematic for {ev.Player.Nickname} destroyed.");
            }
            else
            {
                if (Config.Debug) Log.Debug($"[MyPlugin] [OnDied] No schematic found for {ev.Player.Nickname}.");
            }
        }

    }
}