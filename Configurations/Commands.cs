#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
#endregion
namespace Configurations
{
    class WorldBanCommands
    {
        #region Register

        private static List<Command> Commands = new List<Command>();
        public static void Register()
        {
            Commands.AddRange(new Command[]
            {
                new Command("custombans.itemban", ItemBan, "witemban"),
                new Command("custombans.projectileban", ProjectileBan, "wprojban"),
                new Command("custombans.tileban", TileBan, "wtileban")
            });
            TShockAPI.Commands.ChatCommands.AddRange(Commands);
        }
        public static void Deregister() =>
            TShockAPI.Commands.ChatCommands.RemoveAll(c => Commands.Contains(c));

        #endregion

        #region ItemBan

        private static void ItemBan(CommandArgs args)
        {
            string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (subCmd)
            {
                case "add":
                #region Add item
                {
                    if (args.Parameters.Count != 3 || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}witemban add <item name or id> <whitelist/w/blacklist/b>", TShock.Config.CommandSpecifier);
                        return;
                    }

                    List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (items.Count == 0)
                    {
                        args.Player.SendErrorMessage("Invalid item.");
                    }
                    else if (items.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => $"{i.Name}({i.netID})"));
                    }
                    else
                    {
                        if (Database.AddItemBan(items[0].type, whitelist))
                            TShock.Itembans.ItemBans.Add(new ItemBan(items[0].Name));
                        args.Player.SendSuccessMessage((whitelist ? "Unbanned " : "Banned ") + items[0].Name + " on current world.");
                    }
                }
                #endregion
                return;
                case "del":
                #region Delete item
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}witemban del <item name name or id>", TShock.Config.CommandSpecifier);
                        return;
                    }

                    List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (items.Count == 0)
                    {
                        args.Player.SendErrorMessage("Invalid item.");
                    }
                    else if (items.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => $"{i.Name}({i.netID})"));
                    }
                    else
                    {
                        Database.RemoveItemBan(items[0].type);
                        args.Player.SendSuccessMessage("Unbanned " + items[0].Name + " on current world.");
                    }
                }
                #endregion
                return;
                case "help":
                #region Help
                {
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;

                    var lines = new List<string>
                    {
                        "add <item name or id> <whitelist/w/blacklist/b> - Adds an item ban on current world.",
                        "del <item name or id> - Deletes an item ban on current world.",
                        "list [page] - Lists all item bans on current world."
                    };

                    PaginationTools.SendPage(args.Player, pageNumber, lines,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Item Ban Sub-Commands ({0}/{1}):",
                            FooterFormat = "Type {0}witemban help {{0}} for more sub-commands.".SFormat(TShock.Config.CommandSpecifier)
                        }
                    );
                }
                #endregion
                return;
                case "list":
                #region List items
                {
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    IEnumerable<string> itemBans = Database.GetItemBans().Select(ban =>
                    {
                        Item i = new Item();
                        i.netDefaults(ban.ID);
                        return $"{i.Name} ({i.netID}, {(ban.Whitelist ? "unbanned" : "banned")})";
                    });
                    PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(itemBans),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Item bans on current world ({0}/{1}):",
                            FooterFormat = "Type {0}witemban list {{0}} for more.".SFormat(TShock.Config.CommandSpecifier),
                            NothingToDisplayString = "There are currently no banned items on current world."
                        });
                }
                #endregion
                return;
            }
        }

        #endregion
        #region ProjectileBan

        private static void ProjectileBan(CommandArgs args)
        {
            string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (subCmd)
            {
                case "add":
                #region Add projectile
                {
                    if (args.Parameters.Count != 3 || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}wprojban add <proj id> <whitelist/w/blacklist/b>", TShock.Config.CommandSpecifier);
                        return;
                    }
                    short id;
                    if (Int16.TryParse(args.Parameters[1], out id) && id > 0 && id < Main.maxProjectileTypes)
                    {
                        if (Database.AddProjectileBan(id, whitelist))
                            TShock.ProjectileBans.ProjectileBans.Add(new ProjectileBan(id));
                        args.Player.SendSuccessMessage((whitelist ? "Unbanned " : "Banned ") + " projectile {0} on current world.", id);
                    }
                    else
                        args.Player.SendErrorMessage("Invalid projectile ID!");
                }
                #endregion
                return;
                case "del":
                #region Delete projectile
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}wprojban del <id>", TShock.Config.CommandSpecifier);
                        return;
                    }

                    short id;
                    if (Int16.TryParse(args.Parameters[1], out id) && id > 0 && id < Main.maxProjectileTypes)
                    {
                        Database.RemoveProjectileBan(id);
                        args.Player.SendSuccessMessage("Unbanned projectile {0} on current world.", id);
                        return;
                    }
                    else
                        args.Player.SendErrorMessage("Invalid projectile ID!");
                }
                #endregion
                return;
                case "help":
                #region Help
                {
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;

                    var lines = new List<string>
                    {
                        "add <projectile ID> <whitelist/w/blacklist/b> - Adds a projectile ban on current world.",
                        "del <projectile ID> - Deletes an projectile ban on current world.",
                        "list [page] - Lists all projectile bans on current world."
                    };

                    PaginationTools.SendPage(args.Player, pageNumber, lines,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Projectile Ban Sub-Commands on current world ({0}/{1}):",
                            FooterFormat = "Type {0}wprojban help {{0}} for more sub-commands.".SFormat(TShock.Config.CommandSpecifier)
                        }
                    );
                }
                #endregion
                return;
                case "list":
                #region List projectiles
                {
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    IEnumerable<string> projectileBans = Database.GetProjectileBans().Select(ban =>
                        $"{ban.ID} ({(ban.Whitelist ? "unbanned" : "banned")})");
                    PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(projectileBans),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Projectile bans on current world ({0}/{1}):",
                            FooterFormat = "Type {0}wprojban list {{0}} for more.".SFormat(TShock.Config.CommandSpecifier),
                            NothingToDisplayString = "There are currently no banned projectiles on current world."
                        });
                }
                #endregion
                return;
            }
        }

        #endregion
        #region TileBan

        private static void TileBan(CommandArgs args)
        {
            string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
            switch (subCmd)
            {
                case "add":
                #region Add tile
                {
                    if (args.Parameters.Count != 3 || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}wtileban add <tile id> <whitelist/w/blacklist/b>", TShock.Config.CommandSpecifier);
                        return;
                    }
                    short id;
                    if (Int16.TryParse(args.Parameters[1], out id) && id >= 0 && id < Main.maxTileSets)
                    {
                        if (Database.AddTileBan(id, whitelist))
                            TShock.TileBans.TileBans.Add(new TileBan(id));
                        args.Player.SendSuccessMessage((whitelist ? "Unbanned " : "Banned ") + " tile {0} on current world.", id);
                    }
                    else
                        args.Player.SendErrorMessage("Invalid tile ID!");
                }
                #endregion
                return;
                case "del":
                #region Delete tile ban
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}wtileban del <id>", TShock.Config.CommandSpecifier);
                        return;
                    }

                    short id;
                    if (Int16.TryParse(args.Parameters[1], out id) && id >= 0 && id < Main.maxTileSets)
                    {
                        Database.RemoveTileBan(id);
                        args.Player.SendSuccessMessage("Unbanned tile {0} on current world.", id);
                        return;
                    }
                    else
                        args.Player.SendErrorMessage("Invalid tile ID!");
                }
                #endregion
                return;
                case "help":
                #region Help
                {
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;

                    var lines = new List<string>
                        {
                            "add <tile ID> <whitelist/w/blacklist/b> - Adds a tile ban on current world.",
                            "del <tile ID> - Deletes a tile ban on current world.",
                            "list [page] - Lists all tile bans on current world."
                        };

                    PaginationTools.SendPage(args.Player, pageNumber, lines,
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Tile Ban Sub-Commands ({0}/{1}):",
                            FooterFormat = "Type {0}wtileban help {{0}} for more sub-commands.".SFormat(TShock.Config.CommandSpecifier)
                        }
                    );
                }
                #endregion
                return;
                case "list":
                #region List tile bans
                {
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                        return;
                    IEnumerable<string> tileBans = Database.GetTileBans().Select(ban =>
                        $"{ban.ID} ({(ban.Whitelist ? "unbanned" : "banned")})");
                    PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(tileBans),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Tile bans on current world ({0}/{1}):",
                            FooterFormat = "Type {0}wtileban list {{0}} for more.".SFormat(TShock.Config.CommandSpecifier),
                            NothingToDisplayString = "There are currently no banned tiles on current world."
                        });
                }
                #endregion
                return;
            }
        }

        #endregion

        #region GetType

        private static bool GetType(CommandArgs args, out bool Whitelist)
        {
            switch (args.Parameters[2].ToLower())
            {
                case "w":
                case "whitelist":
                    return (Whitelist = true);
                case "b":
                case "blacklist":
                    return !(Whitelist = false);
                default:
                    return (Whitelist = false);
            }
        }

        #endregion
    }
}