#region Using
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
#endregion
namespace Configurations
{
    internal static class Database
    {
        #region Connect

        private static IDbConnection DB;
        private static bool MySQL;
        public static void Connect()
        {
            #region DB

            switch (TShock.Config.Settings.StorageType.ToLower())
            {
                case "mysql":
                    MySQL = true;
                    string[] host = TShock.Config.Settings.MySqlHost.Split(':');
                    DB = new MySqlConnection()
                    {
                        ConnectionString = $"Server={host[0]}; " +
                                           $"Port={(host.Length == 1 ? "3306" : host[1])}; " +
                                           $"Database={TShock.Config.Settings.MySqlDbName}; " +
                                           $"Uid={TShock.Config.Settings.MySqlUsername}; " +
                                           $"Pwd={TShock.Config.Settings.MySqlPassword};"
                    };
                    break;
                case "sqlite":
                    string path = Path.Combine(TShock.SavePath, "Configurations.sqlite");
                    DB = new SqliteConnection($"uri=file://{path},Version=3");
                    break;
            }

            #endregion
            #region Create tables

            DB.Query($@"
CREATE TABLE IF NOT EXISTS GroupsConfiguration
(
    WorldID INTEGER,
    GroupName VARCHAR(100),
    AddedPermissions TEXT,
    RemovedPermissions TEXT,
    UNIQUE{(MySQL ? " KEY" : "")} (WorldID, GroupName)
);

CREATE TABLE IF NOT EXISTS ItemBansOverride
(
    WorldID INTEGER,
    ItemBan INTEGER,
    Whitelist BOOLEAN,
    UNIQUE{(MySQL ? " KEY" : "")} (WorldID, ItemBan)
);

CREATE TABLE IF NOT EXISTS ProjectileBansOverride
(
    WorldID INTEGER,
    ProjectileBan INTEGER,
    Whitelist BOOLEAN,
    UNIQUE{(MySQL ? " KEY" : "")} (WorldID, ProjectileBan)
);

CREATE TABLE IF NOT EXISTS TileBansOverride
(
    WorldID INTEGER,
    TileBan INTEGER,
    Whitelist BOOLEAN,
    UNIQUE{(MySQL ? " KEY" : "")} (WorldID, TileBan)
);");

            #endregion
        }

        public static void Dispose() => DB?.Dispose();

        #endregion
        #region LoadConfiguration

        public static (ConcurrentDictionary<string, Group> GroupOverrides,
                       ConcurrentDictionary<int, bool> ItemBanOverrides,
                       ConcurrentDictionary<int, bool> ProjectileBanOverrides,
                       ConcurrentDictionary<int, bool> TileBanOverrides) LoadConfiguration()
        {
            #region GroupsConfiguration

            ConcurrentDictionary<string, Group> groupOverrides = new ConcurrentDictionary<string, Group>();
            using (QueryResult groups = DB.QueryReader("SELECT * FROM GroupsConfiguration " +
                    "WHERE WorldID=@0;", Main.worldID))
                while (groups.Read())
                {
                    Group group = TShock.Groups.GetGroupByName(groups.Get<string>("GroupName"));
                    if (group == null)
                        continue;
                    string[] added = groups.Get<string>("AddedPermissions")?
                                            .Split(new char[] { ',' },
                                            StringSplitOptions.RemoveEmptyEntries);
                    string[] removed = groups.Get<string>("RemovedPermissions")?
                                                .Split(new char[] { ',' },
                                                StringSplitOptions.RemoveEmptyEntries);
                    Group groupOverride = new Group(group.Name, permissions: group.Permissions);
                    bool isOverride = false;
                    if (isOverride |= (added?.Any() == true))
                        foreach (string permission in added)
                            groupOverride.AddPermission(permission);
                    if (isOverride |= (removed?.Any() == true))
                        foreach (string permission in removed)
                            groupOverride.RemovePermission(permission);
                    if (isOverride)
                        groupOverrides.TryAdd(group.Name, groupOverride);
                }

            #endregion
            #region ItemBansOverride

            ConcurrentDictionary<int, bool> itemBanOverrides = new ConcurrentDictionary<int, bool>();
            using (QueryResult itemBans = DB.QueryReader("SELECT * FROM ItemBansOverride " +
                    "WHERE WorldID=@0;", Main.worldID))
                while (itemBans.Read())
                    itemBanOverrides.TryAdd(itemBans.Get<int>("ItemBan"), itemBans.Get<bool>("Whitelist"));

            #endregion
            #region ProjectileBansOverride

            ConcurrentDictionary<int, bool> projectileBanOverrides = new ConcurrentDictionary<int, bool>();
            using (QueryResult projectileBans = DB.QueryReader("SELECT * FROM ProjectileBansOverride " +
                    "WHERE WorldID=@0;", Main.worldID))
                while (projectileBans.Read())
                    projectileBanOverrides.TryAdd(projectileBans.Get<int>("ProjectileBan"),
                        projectileBans.Get<bool>("Whitelist"));

            #endregion
            #region TileBansOverride

            ConcurrentDictionary<int, bool> tileBanOverrides = new ConcurrentDictionary<int, bool>();
            using (QueryResult tileBans = DB.QueryReader("SELECT * FROM TileBansOverride " +
                    "WHERE WorldID=@0;", Main.worldID))
                while (tileBans.Read())
                    tileBanOverrides.TryAdd(tileBans.Get<int>("TileBan"), tileBans.Get<bool>("Whitelist"));

            #endregion
            return (groupOverrides, itemBanOverrides, projectileBanOverrides, tileBanOverrides);
        }

        #endregion

        #region Add

        public static void AddItemBan(int ID, bool Whitelist) =>
            DB.Query("REPLACE INTO ItemBansOverride (WorldID, ItemBan, Whitelist) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, Whitelist);
        public static void AddProjectileBan(int ID, bool Whitelist) =>
            DB.Query("REPLACE INTO ProjectileBansOverride (WorldID, ProjectileBan, Whitelist) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, Whitelist);
        public static void AddTileBan(int ID, bool Whitelist) =>
            DB.Query("REPLACE INTO TileBansOverride (WorldID, TileBan, Whitelist) " +
                "VALUES (@0, @1, @2);", Main.worldID, ID, Whitelist);

        #endregion
        #region Remove

        public static void RemoveItemBan(int ID) =>
            DB.Query($"DELETE FROM ItemBansOverride WHERE WorldID=@0 AND ItemBan=@1;", Main.worldID, ID);
        public static void RemoveProjectileBan(int ID) =>
            DB.Query($"DELETE FROM ProjectileBanOverride WHERE WorldID=@0 AND ProjectileBan=@1;",
                Main.worldID, ID);
        public static void RemoveTileBan(int ID) =>
            DB.Query($"DELETE FROM TileBansOverride WHERE WorldID=@0 AND TileBan=@1;", Main.worldID, ID);

        #endregion
    }
}