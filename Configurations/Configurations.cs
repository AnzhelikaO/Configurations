using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI.Hooks;

namespace Configurations
{
    [ApiVersion(2, 1)]
    public class Configurations : TerrariaPlugin
    {
        public override string Author => "Anzhelika";
        public override string Name => "Delimiter";
        public override string Description => "Separate group permissions, item/tile/projectile bans";
        public override Version Version => new Version(1, 0, 0, 0);
        public Configurations(Main game) : base(game) { }

        public override void Initialize()
        {
            Database.Connect();
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
            GeneralHooks.ReloadEvent += OnReload;
        }

        private void OnReload(ReloadEventArgs args)
        { Database.LoadConfiguration(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }

        private void OnPostInit(EventArgs args) { Database.LoadConfiguration(); }
    }
}
