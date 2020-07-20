#region Using
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.Localization;
#endregion
namespace Configurations
{
    [ApiVersion(2, 1)]
    public class Configurations : TerrariaPlugin
    {
        #region Description

        public override string Author => "Anzhelika";
        public override string Name => "Configurations";
        public override string Description =>
            "Separate group permissions, item/tile/projectile bans for different worlds.";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public Configurations(Main Game) : base(Game) { }

        #endregion

        #region Initialize

        public override void Initialize()
        {
            Database.Connect();
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize);
            PlayerHooks.PlayerPermission += OnPlayerPermission;
            PlayerHooks.PlayerItembanPermission += OnPlayerItembanPermission;
            PlayerHooks.PlayerProjbanPermission += OnPlayerProjbanPermission;
            PlayerHooks.PlayerTilebanPermission += OnPlayerTilebanPermission;
            GeneralHooks.ReloadEvent += OnReload;
            WorldBanCommands.Register();
        }

        private void OnGamePostInitialize(EventArgs Args) =>
            Overrides.Reload();

        #endregion
        #region Dispose

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnGamePostInitialize);
                PlayerHooks.PlayerPermission -= OnPlayerPermission;
                PlayerHooks.PlayerItembanPermission -= OnPlayerItembanPermission;
                PlayerHooks.PlayerProjbanPermission -= OnPlayerProjbanPermission;
                PlayerHooks.PlayerTilebanPermission -= OnPlayerTilebanPermission;
                GeneralHooks.ReloadEvent -= OnReload;
                Database.Dispose();
                WorldBanCommands.Deregister();
            }
            base.Dispose(Disposing);
        }

        #endregion

        #region OnPlayerPermission

        private void OnPlayerPermission(PlayerPermissionEventArgs Args)
        {
            if ((Args.Result == PermissionHookResult.Unhandled)
                    && Overrides.HasGroupOverride(Args.Player.Group.Name, out Group group))
                Args.Result = (group.HasPermission(Args.Permission)
                                    ? PermissionHookResult.Granted
                                    : PermissionHookResult.Denied);
        }

        #endregion
        #region OnPlayerItembanPermission

        private void OnPlayerItembanPermission(PlayerItembanPermissionEventArgs Args)
        {
            if ((Args.Result == PermissionHookResult.Unhandled)
                    && Overrides.HasItemBanOverride(TShock.Utils.GetItemByName(
                        Args.BannedItem.Name)[0].netID, out bool whitelist))
                Args.Result = (whitelist ? PermissionHookResult.Granted : PermissionHookResult.Denied);
        }

        #endregion
        #region OnPlayerProjbanPermission

        private void OnPlayerProjbanPermission(PlayerProjbanPermissionEventArgs Args)
        {
            if ((Args.Result == PermissionHookResult.Unhandled)
                    && Overrides.HasProjectileBanOverride(Args.BannedProjectile.ID, out bool whitelist))
                Args.Result = (whitelist ? PermissionHookResult.Granted : PermissionHookResult.Denied);
        }

        #endregion
        #region OnPlayerTilebanPermission

        private void OnPlayerTilebanPermission(PlayerTilebanPermissionEventArgs Args)
        {
            if ((Args.Result == PermissionHookResult.Unhandled)
                    && Overrides.HasTileBanOverride(Args.BannedTile.ID, out bool whitelist))
                Args.Result = (whitelist ? PermissionHookResult.Granted : PermissionHookResult.Denied);
        }

        #endregion
        #region OnReload

        private void OnReload(ReloadEventArgs Args) =>
            Overrides.Reload();

        #endregion
    }
}