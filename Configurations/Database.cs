#region Using
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
#endregion
namespace Configurations
{
    public class Database
    {
        #region Connect

        private static IDbConnection DB;
        public static void Connect()
        {
            #region DB

            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] host = TShock.Config.MySqlHost.Split(':');
                    DB = new MySqlConnection()
                    {
                        ConnectionString = $"Server={host[0]}; " +
                                           $"Port={(host.Length == 1 ? "3306" : host[1])}; " +
                                           $"Database={TShock.Config.MySqlDbName}; " +
                                           $"Uid={TShock.Config.MySqlUsername}; " +
                                           $"Pwd={TShock.Config.MySqlPassword};"
                    };
                    break;
                case "sqlite":
                    string path = Path.Combine(TShock.SavePath, "Configurations.sqlite");
                    DB = new SqliteConnection($"uri=file://{path},Version=3");
                    break;
            }

            #endregion
            #region Create tables

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
    Whitelist BOOLEAN,
    UNIQUE KEY (WorldID, ItemBan)
);

CREATE TABLE IF NOT EXISTS ProjectileBansOverride
(
    WorldID INTEGER,
    ProjectileBan INTEGER,
    Whitelist BOOLEAN,
    UNIQUE KEY (WorldID, ProjectileBan)
);

CREATE TABLE IF NOT EXISTS TileBansOverride
(
    WorldID INTEGER,
    TileBan INTEGER,
    Whitelist BOOLEAN,
    UNIQUE KEY (WorldID, TileBan)
);");

            #endregion
        }

        public static void Dispose() => DB?.Dispose();

        #endregion
        #region LoadConfiguration

        public static void LoadConfiguration()
        {
            #region GroupsConfiguration

            try
            {
                using (QueryResult groups = DB.QueryReader("SELECT * FROM GroupsConfiguration WHERE WorldID=@0;", Main.worldID))
                    while (groups.Read())
                    {
                        Group group = TShock.Groups.GetGroupByName(groups.Get<string>("GroupName"));
                        if (group == null)
                            continue;
                        string[] added = groups.Get<string>("AddedPermissions")?.Split(',');
                        string[] removed = groups.Get<string>("RemovedPermissions")?.Split(',');
                        if ((added != null) && (added.Length > 0))
                            foreach (string permission in added)
                                if (!group.permissions.Contains(permission))
                                    group.permissions.Add(permission);
                        if ((removed != null) && (removed.Length > 0))
                            group.permissions.RemoveAll(p => removed.Contains(p));
                    }
            }
            catch { }

            #endregion
            #region ItemBansOverride

            try
            {
                using (QueryResult itemBans = DB.QueryReader("SELECT * FROM ItemBansOverride WHERE WorldID=@0;", Main.worldID))
                    while (itemBans.Read())
                    {
                        int item = itemBans.Get<int>("ItemBan");
                        if (TShock.Utils.GetItemById(item) == null)
                            continue;
                        string name = EnglishLanguage.GetItemNameById(item);
                        if (itemBans.Get<bool>("Whitelist"))
                            TShock.Itembans.ItemBans.RemoveAll(i => (i.Name == name));
                        else if (!TShock.Itembans.ItemBans.Any(i => (i.Name == name)))
                            TShock.Itembans.ItemBans.Add(new ItemBan(name));
                    }
            }
            catch { }

            #endregion
            #region ProjectileBansOverride

            try
            {
                using (QueryResult projectileBans = DB.QueryReader("SELECT * FROM ProjectileBansOverride WHERE WorldID=@0;", Main.worldID))
                    while (projectileBans.Read())
                    {
                        int projectile = projectileBans.Get<int>("ProjectileBan");
                        if (projectileBans.Get<bool>("Whitelist"))
                            TShock.ProjectileBans.ProjectileBans.RemoveAll(p => (p.ID == projectile));
                        else if (!TShock.ProjectileBans.ProjectileBans.Any(p => (p.ID == projectile)))
                            TShock.ProjectileBans.ProjectileBans.Add(new ProjectileBan((short)projectile));
                    }
            }
            catch { }

            #endregion
            #region TileBansOverride

            try
            {
                using (QueryResult tileBans = DB.QueryReader("SELECT * FROM TileBansOverride WHERE WorldID=@0;", Main.worldID))
                    while (tileBans.Read())
                    {
                        int tile = tileBans.Get<int>("TileBan");
                        if (tileBans.Get<bool>("Whitelist"))
                            TShock.TileBans.TileBans.RemoveAll(t => (t.ID == tile));
                        else if (!TShock.TileBans.TileBans.Any(t => (t.ID == tile)))
                            TShock.TileBans.TileBans.Add(new TileBan((short)tile));
                    }
            }
            catch { }

            #endregion
        }

        #endregion

        #region Add

        public static bool AddItemBan(int ID, bool Whitelist) =>
            (DB.Query("REPLACE INTO ItemBansOverride (WorldID, ItemBan, Whitelist) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, Whitelist) > 0);
        public static bool AddProjectileBan(int ID, bool Whitelist) =>
            (DB.Query("REPLACE INTO ProjectileBansOverride (WorldID, ProjectileBan, Whitelist) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, Whitelist) > 0);
        public static bool AddTileBan(int ID, bool Whitelist) =>
            (DB.Query("REPLACE INTO TileBansOverride (WorldID, TileBan, Whitelist) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, Whitelist) > 0);

        #endregion
        #region Remove

        public static bool RemoveItemBan(int ID, out bool Whitelist) =>
            RemoveBan("ItemBan", ID, out Whitelist);
        public static bool RemoveProjectileBan(int ID, out bool Whitelist) =>
            RemoveBan("ProjectileBan", ID, out Whitelist);
        public static bool RemoveTileBan(int ID, out bool Whitelist) =>
            RemoveBan("TileBan", ID, out Whitelist);

        private static bool RemoveBan(string BanName, int ID, out bool Whitelist)
        {
            using (QueryResult reader = DB.QueryReader(
$@"SELECT Whitelist FROM {BanName}sOverride WHERE WorldID=@0 AND {BanName}=@1;
DELETE FROM {BanName}sOverride WHERE WorldID=@0 AND {BanName}=@1;", Main.worldID, ID))
                if (reader.Read())
                {
                    Whitelist = reader.Get<bool>("Whitelist");
                    return true;
                }
                else
                    return (Whitelist = false);
        }

        #endregion
        #region Get

        public static (int ID, bool Whitelist)[] GetItemBans() => GetBans("ItemBan");
        public static (int ID, bool Whitelist)[] GetProjectileBans() => GetBans("ProjectileBan");
        public static (int ID, bool Whitelist)[] GetTileBans() => GetBans("TileBan");

        private static (int ID, bool Whitelist)[] GetBans(string BanName)
        {
            List<(int, bool)> bans = new List<(int, bool)>();
            using (QueryResult reader = DB.QueryReader($"SELECT * FROM {BanName}sOverride WHERE WorldID=@0;", Main.worldID))
                while (reader.Read())
                    bans.Add((reader.Get<int>(BanName), reader.Get<bool>("Whitelist")));
            return bans.ToArray();
        }

        #endregion
    }
}