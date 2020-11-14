#region Using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Localization;
#endregion
namespace Configurations
{
    public static class Overrides
    {
        private static readonly object Locker = new object();
        internal static ConcurrentDictionary<string, Group> Groups =
            new ConcurrentDictionary<string, Group>();
        internal static ConcurrentDictionary<int, bool> ItemBans =
            new ConcurrentDictionary<int, bool>();
        internal static ConcurrentDictionary<int, bool> ProjectileBans =
            new ConcurrentDictionary<int, bool>();
        internal static ConcurrentDictionary<int, bool> TileBans =
            new ConcurrentDictionary<int, bool>();
        public static void Reload()
        {
            lock (Locker)
            {
                (Groups, ItemBans, ProjectileBans, TileBans) = Database.LoadConfiguration();
                TShock.ItemBans.DataModel.ItemBans.RemoveAll(b => (b is ItemBanOverride));
                foreach (var pair in ItemBans)
                    OverrideItemBan(pair.Key);
                TShock.ProjectileBans.ProjectileBans.RemoveAll(b => (b is ProjectileBanOverride));
                foreach (var pair in ProjectileBans)
                    OverrideProjectileBan(pair.Key);
                TShock.TileBans.TileBans.RemoveAll(b => (b is TileBan));
                foreach (var pair in TileBans)
                    OverrideTileBan(pair.Key);
            }
        }

        #region OverrideBan

        public static void OverrideItemBan(int ID, bool Whitelist)
        {
            lock (Locker)
            {
                Database.AddItemBan(ID, Whitelist);
                ItemBans[ID] = Whitelist;
                OverrideItemBan(ID);
            }
        }
        private static void OverrideItemBan(int ID)
        {
            string name = TShock.Utils.GetItemById(ID).Name;
            if (!TShock.ItemBans.DataModel.ItemBans.Any(b => (b.Name == name)))
            {
                ItemBanOverride itemBanOverride = new ItemBanOverride(name);
                TShock.ItemBans.DataModel.ItemBans.Add(itemBanOverride);
            }
        }

        public static void OverrideProjectileBan(int ID, bool Whitelist)
        {
            lock (Locker)
            {
                Database.AddProjectileBan(ID, Whitelist);
                ProjectileBans[ID] = Whitelist;
                OverrideProjectileBan(ID);
            }
        }
        public static void OverrideProjectileBan(int ID)
        {
            if (!TShock.ProjectileBans.ProjectileBans.Any(b => (b.ID == ID)))
                TShock.ProjectileBans.ProjectileBans.Add(new ProjectileBanOverride((short)ID));
        }

        public static void OverrideTileBan(int ID, bool Whitelist)
        {
            lock (Locker)
            {
                Database.AddTileBan(ID, Whitelist);
                TileBans[ID] = Whitelist;
                OverrideTileBan(ID);
            }
        }
        public static void OverrideTileBan(int ID)
        {
            if (!TShock.TileBans.TileBans.Any(b => (b.ID == ID)))
                TShock.TileBans.TileBans.Add(new TileBanOverride((short)ID));
        }

        #endregion
        #region RemoveBanOverride

        public static bool RemoveItemBanOverride(int ID, out bool Banned, out bool Whitelist)
        {
            lock (Locker)
            {
                string name = EnglishLanguage.GetItemNameById(ID);
                Banned = TShock.ItemBans.DataModel.ItemBans.Any(b =>
                    (!(b is ItemBanOverride) && (b.Name == name)));
                if (ItemBans.TryRemove(ID, out Whitelist))
                {
                    Database.RemoveItemBan(ID);
                    TShock.ItemBans.DataModel.ItemBans.RemoveAll(b =>
                        ((b is ItemBanOverride @override) && (@override.Name == name)));
                    return true;
                }
                return false;
            }
        }
        public static bool RemoveProjectileBanOverride(int ID, out bool Banned, out bool Whitelist)
        {
            lock (Locker)
            {
                Banned = TShock.ProjectileBans.ProjectileBans.Any(b =>
                    (!(b is ProjectileBanOverride) && (b.ID == ID)));
                if (ProjectileBans.TryRemove(ID, out Whitelist))
                {
                    Database.RemoveProjectileBan(ID);
                    TShock.ProjectileBans.ProjectileBans.RemoveAll(b =>
                        ((b is ProjectileBanOverride @override) && (@override.ID == ID)));
                    return true;
                }
                return false;
            }
        }
        public static bool RemoveTileBanOverride(int ID, out bool Banned, out bool Whitelist)
        {
            lock (Locker)
            {
                Banned = TShock.TileBans.TileBans.Any(b =>
                    (!(b is TileBanOverride) && (b.ID == ID)));
                if (TileBans.TryRemove(ID, out Whitelist))
                {
                    Database.RemoveTileBan(ID);
                    TShock.TileBans.TileBans.RemoveAll(b =>
                        ((b is TileBanOverride @override) && (@override.ID == ID))); ;
                    return true;
                }
                return false;
            }
        }

        #endregion
        #region IsBanned

        public static bool IsBannedItem(int ID, out bool Locally)
        {
            lock (Locker)
            {
                if (Locally = ItemBans.TryGetValue(ID, out bool banned))
                    return banned;
                string name = EnglishLanguage.GetItemNameById(ID);
                return TShock.ItemBans.DataModel.ItemBans.Any(b => (b.Name == name));
            }
        }
        public static bool IsBannedProjectile(int ID, out bool Locally)
        {
            lock (Locker)
            {
                return (Locally = ProjectileBans.TryGetValue(ID, out bool banned)
                            ? banned
                            : TShock.ProjectileBans.ProjectileBans.Any(b => (b.ID == ID)));
            }
        }
        public static bool IsBannedTile(int ID, out bool Locally)
        {
            lock (Locker)
            {
                return (Locally = TileBans.TryGetValue(ID, out bool banned)
                            ? banned
                            : TShock.TileBans.TileBans.Any(b => (b.ID == ID)));
            }
        }

        public enum BanType { Item, Projectile, Tile }
        public static bool IsBanned(BanType BanType, int ID, out bool Locally)
        {
            switch (BanType)
            {
                case BanType.Item:
                    return IsBannedItem(ID, out Locally);
                case BanType.Projectile:
                    return IsBannedProjectile(ID, out Locally);
                case BanType.Tile:
                    return IsBannedTile(ID, out Locally);
                default:
                    throw new NotImplementedException($"Unknown {nameof(Overrides.BanType)}.");
            }
        }

        #endregion
        #region HasOverride

        internal static bool HasGroupOverride(string Name, out Group Group)
        {
            lock (Locker)
                return Groups.TryGetValue(Name, out Group);
        }
        internal static bool HasItemBanOverride(int ID, out bool Whitelist)
        {
            lock (Locker)
                return ItemBans.TryGetValue(ID, out Whitelist);
        }
        internal static bool HasProjectileBanOverride(int ID, out bool Whitelist)
        {
            lock (Locker)
                return ProjectileBans.TryGetValue(ID, out Whitelist);
        }
        internal static bool HasTileBanOverride(int ID, out bool Whitelist)
        {
            lock (Locker)
                return TileBans.TryGetValue(ID, out Whitelist);
        }

        #endregion
        #region API

        public static Dictionary<string, Group> GetGroupOverrides()
        {
            lock (Locker)
                return Groups.ToDictionary(k => k.Key,
                                           v => new Group(v.Value.Name, permissions: v.Value.Permissions));
        }
        public static bool GetGroupOverride(string Name, out Group Group)
        {
            lock (Locker)
            {
                Group = null;
                if (Groups.TryGetValue(Name, out Group group))
                    Group = new Group(group.Name, permissions: group.Permissions);
                return (Group != null);
            }
        }

        public static Dictionary<int, bool> GetItemBanOverrides()
        {
            lock (Locker)
                return ItemBans.ToDictionary(k => k.Key, v => v.Value);
        }
        public static bool GetItemBanOverride(int ID, out bool Whitelist)
        {
            lock (Locker)
                return ItemBans.TryGetValue(ID, out Whitelist);
        }

        public static Dictionary<int, bool> GetProjectileBanOverrides()
        {
            lock (Locker)
                return ProjectileBans.ToDictionary(k => k.Key, v => v.Value);
        }
        public static bool GetProjectileBanOverride(int ID, out bool Whitelist)
        {
            lock (Locker)
                return ProjectileBans.TryGetValue(ID, out Whitelist);
        }

        public static Dictionary<int, bool> GetTileBanOverrides()
        {
            lock (Locker)
                return TileBans.ToDictionary(k => k.Key, v => v.Value);
        }
        public static bool GetTileBanOverride(int ID, out bool Whitelist)
        {
            lock (Locker)
                return TileBans.TryGetValue(ID, out Whitelist);
        }

        #endregion
    }

    public class ItemBanOverride : ItemBan
    {
        public ItemBanOverride(string Name)
            : base(Name) { }
    }
    public class ProjectileBanOverride : ProjectileBan
    {
        public ProjectileBanOverride(short ID)
            : base(ID) { }
    }
    public class TileBanOverride : TileBan
    {
        public TileBanOverride(short ID)
            : base(ID) { }
    }
}