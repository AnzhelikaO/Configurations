using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Localization;

namespace Configurations
{
    public class Database
    {
        private static IDbConnection DB;
        public static void Connect()
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    {
                        string[] Host = TShock.Config.MySqlHost.Split(':');
                        DB = new MySqlConnection()
                        {
                            ConnectionString = string.Format
                            (
                                "Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                Host[0],
                                Host.Length == 1 ? "3306" : Host[1],
                                TShock.Config.MySqlDbName,
                                TShock.Config.MySqlUsername,
                                TShock.Config.MySqlPassword
                            )
                        };
                        break;
                    }
                case "sqlite":
                    {
                        DB = new SqliteConnection(string.Format
                        (
                            "uri=file://{0},Version=3",
                            Path.Combine(TShock.SavePath, "Configurations.sqlite")
                        ));
                        break;
                    }
            }

            DB.Query(@"
CREATE TABLE IF NOT EXISTS GroupsConfiguration
(
    WorldID INTEGER,
    GroupName VARCHAR(100),
    AddedPermissions TEXT,
    RemovedPermissions TEXT,
    UNIQUE KEY (WorldID, GroupName)
);

CREATE TABLE IF NOT EXISTS ItemBansOverride
(
    WorldID INTEGER,
    ItemBan INTEGER,
    Type VARCHAR(50),
    UNIQUE KEY (WorldID, ItemBan)
);

CREATE TABLE IF NOT EXISTS ProjectileBansOverride
(
    WorldID INTEGER,
    ProjectileBan INTEGER,
    Type VARCHAR(50),
    UNIQUE KEY (WorldID, ProjectileBan)
);

CREATE TABLE IF NOT EXISTS TileBansOverride
(
    WorldID INTEGER,
    TileBan INTEGER,
    Type VARCHAR(50),
    UNIQUE KEY (WorldID, TileBan)
);");
        }
        public static void Dispose() => DB.Dispose();

        public static void LoadConfiguration()
        { try {
            using (QueryResult groups = DB.QueryReader("SELECT * FROM GroupsConfiguration WHERE WorldID=@0;", Main.worldID))
            {
                while (groups.Read())
                {
                    string Name = groups.Get<string>("GroupName");
                    string[] Added = groups.Get<string>("AddedPermissions")?.Split(',');
                    string[] Removed = groups.Get<string>("RemovedPermissions")?.Split(',');

                    Group Group = TShock.Groups.GetGroupByName(Name);
                    if (Group == null) { continue; }
                    if ((Added != null) && (Added.Length > 0))
                    {
                        foreach (string permission in Added)
                        {
                            if (!Group.permissions.Contains(permission))
                            { Group.permissions.Add(permission); }
                        }
                    }
                    if ((Removed != null) && (Removed.Length > 0))
                    { Group.permissions.RemoveAll(p => Removed.Contains(p)); }
                }
            }

            using (QueryResult itemBans = DB.QueryReader("SELECT * FROM ItemBansOverride WHERE WorldID=@0;", Main.worldID))
            {
                while (itemBans.Read())
                {
                    int Item = itemBans.Get<int>("ItemBan");
                    bool Removed = (itemBans.Get<string>("Type")?.ToLower() == "whitelist");
                    Item ItemBan = TShock.Utils.GetItemById(Item);
                    string ItemName = EnglishLanguage.GetItemNameById(Item);
                    if (ItemBan == null) { continue; }
                    if (Removed)
                    { TShock.Itembans.ItemBans.RemoveAll(i => (i.Name == ItemName)); }
                    else if (!TShock.Itembans.ItemBans.Any(i => (i.Name == ItemName)))
                    { TShock.Itembans.ItemBans.Add(new ItemBan(ItemName)); }
                }
            }

            using (QueryResult projectileBans = DB.QueryReader("SELECT * FROM ProjectileBansOverride WHERE WorldID=@0;", Main.worldID))
            {
                while (projectileBans.Read())
                {
                    short Projectile = (short)projectileBans.Get<int>("ProjectileBan");
                    bool Removed = (projectileBans.Get<string>("Type")?.ToLower() == "whitelist");
                    if (Removed)
                    { TShock.ProjectileBans.ProjectileBans.RemoveAll(p => (p.ID == Projectile)); }
                    else if (!TShock.ProjectileBans.ProjectileBans.Any(p => (p.ID == Projectile)))
                    { TShock.ProjectileBans.ProjectileBans.Add(new ProjectileBan(Projectile)); }
                }
            }

            using (QueryResult tileBans = DB.QueryReader("SELECT * FROM TileBansOverride WHERE WorldID=@0;", Main.worldID))
            {
                while (tileBans.Read())
                {
                    short Tile = (short)tileBans.Get<int>("TileBan");
                    bool Removed = (tileBans.Get<string>("Type")?.ToLower() == "whitelist");
                    if (Removed)
                    { TShock.TileBans.TileBans.RemoveAll(t => (t.ID == Tile)); }
                    else if (!TShock.TileBans.TileBans.Any(t => (t.ID == Tile)))
                    { TShock.TileBans.TileBans.Add(new TileBan(Tile)); }
                }
            }
        } catch { } }

        public static void AddItemBan(int ID, bool Whitelist) =>
            DB.Query("REPLACE INTO ItemBansOverride (WorldID, ItemBan, Type) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, (Whitelist ? "whitelist" : "blacklist"));

        public static void AddProjectileBan(int ID, bool Whitelist) =>
            DB.Query("REPLACE INTO ProjectileBansOverride (WorldID, ProjectileBan, Type) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, (Whitelist ? "whitelist" : "blacklist"));

        public static void AddTileBan(int ID, bool Whitelist) =>
            DB.Query("REPLACE INTO TileBansOverride (WorldID, TileBan, Type) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, (Whitelist ? "whitelist" : "blacklist"));

        public static void RemoveItemBan(int ID) =>
            DB.Query("DELETE FROM ItemBansOverride WHERE WorldID=@0 AND ItemBan=@1;", Main.worldID, ID);

        public static void RemoveProjectileBan(int ID) =>
            DB.Query("DELETE FROM ProjectileBansOverride WHERE WorldID=@0 AND ProjectileBan=@1;", Main.worldID, ID);

        public static void RemoveTileBan(int ID) =>
            DB.Query("DELETE FROM TileBansOverride WHERE WorldID=@0 AND TileBan=@1;", Main.worldID, ID);

        public static (int ID, bool Whitelist)[] GetItemBans() => GetBans("ItemBan");

        public static (int ID, bool Whitelist)[] GetProjectileBans() => GetBans("ProjectileBan");

        public static (int ID, bool Whitelist)[] GetTileBans() => GetBans("TileBan");

        private static (int ID, bool Whitelist)[] GetBans(string BanName)
        {
            List<(int, bool)> bans = new List<(int, bool)>();
            using (QueryResult reader = DB.QueryReader($"SELECT * FROM {BanName}sOverride WHERE WorldID=@0;", Main.worldID))
                while (reader.Read())
                    bans.Add((reader.Get<int>(BanName), (reader.Get<string>("Type")?.ToLower() == "whitelist")));
            return bans.ToArray();
        }
    }
}
