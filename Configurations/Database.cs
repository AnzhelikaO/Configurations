using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
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
                            Path.Combine(TShock.SavePath, "Delimiter.sqlite")
                        ));
                        break;
                    }
            }

            SqlTableCreator sqlcreator = new SqlTableCreator
            (
                DB,
                ((DB.GetSqlType() == SqlType.Sqlite)
                    ? (IQueryBuilder)new SqliteQueryCreator()
                    : new MysqlQueryCreator())
            );

            sqlcreator.EnsureTableStructure(new SqlTable("RemovedPlugins",
                new SqlColumn("WorldID", MySqlDbType.Int32),
                new SqlColumn("PluginName", MySqlDbType.Text)));

            sqlcreator.EnsureTableStructure(new SqlTable("GroupsConfiguration",
                new SqlColumn("WorldID", MySqlDbType.Int32),
                new SqlColumn("GroupName", MySqlDbType.Text),
                new SqlColumn("AddedPermissions", MySqlDbType.Text),
                new SqlColumn("RemovedPermissions", MySqlDbType.Text)));

            sqlcreator.EnsureTableStructure(new SqlTable("ItemBans",
                new SqlColumn("WorldID", MySqlDbType.Int32),
                new SqlColumn("ItemBan", MySqlDbType.Int32),
                new SqlColumn("Type", MySqlDbType.Text)));

            sqlcreator.EnsureTableStructure(new SqlTable("ProjectileBans",
                new SqlColumn("WorldID", MySqlDbType.Int32),
                new SqlColumn("ProjectileBan", MySqlDbType.Int32),
                new SqlColumn("Type", MySqlDbType.Int32)));

            sqlcreator.EnsureTableStructure(new SqlTable("TileBans",
                new SqlColumn("WorldID", MySqlDbType.Int32),
                new SqlColumn("TileBan", MySqlDbType.Int32),
                new SqlColumn("Type", MySqlDbType.Int32)));
        }

        public static void LoadConfiguration()
        {
            using (QueryResult plugins = DB.QueryReader("SELECT * FROM RemovedPlugins WHERE WorldID=@0;", Main.worldID))
            {
                List<string> Plugins = new List<string>();
                while (plugins.Read())
                { Plugins.Add(plugins.Get<string>("PluginName")); }

                if (Plugins.Count > 0)
                {
                    foreach (PluginContainer Plugin in ServerApi.Plugins)
                    {
                        if (Plugins.Contains(Plugin.Plugin.Name))
                        { Plugin.Dispose(); }
                    }
                }
            }

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

            using (QueryResult itemBans = DB.QueryReader("SELECT * FROM ItemBans WHERE WorldID=@0;", Main.worldID))
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

            using (QueryResult projectileBans = DB.QueryReader("SELECT * FROM ProjectileBans WHERE WorldID=@0;", Main.worldID))
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

            using (QueryResult tileBans = DB.QueryReader("SELECT * FROM TileBans WHERE WorldID=@0;", Main.worldID))
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
        }
    }
}
