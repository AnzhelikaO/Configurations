#region Using
using System;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI.Hooks;
#endregion
namespace Configurations
{
    [ApiVersion(2, 1)]
    public class Configurations : TerrariaPlugin
    {
        #region Description

        public override string Author => "Anzhelika";
        public override string Name => "Configurations";
        public override string Description => "Separate group permissions, item/tile/projectile bans";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public Configurations(Main game) : base(game) { }

        #endregion

        #region Initialize

        public override void Initialize()
        {
            Database.Connect();
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize);
            GeneralHooks.ReloadEvent += OnReload;
            WorldBanCommands.Register();
        }

        private void OnGamePostInitialize(EventArgs args) =>
            Database.LoadConfiguration();

        #endregion
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnGamePostInitialize);
                GeneralHooks.ReloadEvent -= OnReload;
                Database.Dispose();
                WorldBanCommands.Deregister();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region OnReload

        private void OnReload(ReloadEventArgs args) =>
            Database.LoadConfiguration();

        #endregion
    }
}