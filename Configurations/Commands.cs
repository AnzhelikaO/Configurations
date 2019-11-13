#region Using
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
            switch (args.Parameters[0]?.ToLower())
            {
                case "add":
                {
                    if ((args.Parameters.Count != 3) || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage(TShock.Config.CommandSpecifier +
                            "witemban add <item name or id> <whitelist/w/blacklist/b>");
                        return;
                    }

                    List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (items.Count == 0)
                    {
                        args.Player.SendErrorMessage("Invalid item.");
                        return;
                    }
                    else if (items.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => $"{i.Name}({i.netID})"));
                        return;
                    }

                    string name = items[0].Name;
                    if (whitelist && TShock.Itembans.ItemBans.Any(b => (b.Name == name)))
                    {
                        if (Database.AddItemBan(items[0].netID, true))
                        {
                            TShock.Itembans.ItemBans.RemoveAll(b => (b.Name == name));
                            args.Player.SendSuccessMessage($"Unbanned item {name} on current world.");
                        }
                        else
                            args.Player.SendErrorMessage($"Item {name} is not banned on current world.");
                    }
                    else if (!whitelist && !TShock.Itembans.ItemBans.Any(b => (b.Name == name)))
                    {
                        if (Database.AddItemBan(items[0].netID, false))
                        {
                            TShock.Itembans.ItemBans.Add(new ItemBan(name));
                            args.Player.SendSuccessMessage($"Banned item {name} on current world.");
                        }
                        else
                            args.Player.SendErrorMessage($"Item {name} is already banned on current world.");
                    }
                    else if (whitelist)
                        args.Player.SendErrorMessage($"Item {name} is not banned.");
                    else
                        args.Player.SendErrorMessage($"Item {name} is already banned.");
                    return;
                }
                case "del":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage(TShock.Config.CommandSpecifier +
                            "witemban del <item name name or id>");
                        return;
                    }

                    List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (items.Count == 0)
                    {
                        args.Player.SendErrorMessage("Invalid item.");
                        return;
                    }
                    else if (items.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => $"{i.Name}({i.netID})"));
                        return;
                    }

                    string name = items[0].Name;
                    if (Database.RemoveItemBan(items[0].netID, out bool whitelist))
                    {
                        if (whitelist)
                        {
                            TShock.Itembans.ItemBans.Add(new ItemBan(name));
                            args.Player.SendSuccessMessage($"Banned item {name}.");
                        }
                        else
                        {
                            TShock.Itembans.ItemBans.RemoveAll(b => (b.Name == name));
                            args.Player.SendSuccessMessage($"Unbanned item {name}.");
                        }
                    }
                    else if (whitelist)
                        args.Player.SendErrorMessage($"Item {items[0].Name} is not banned.");
                    else
                        args.Player.SendErrorMessage($"Item {items[0].Name} is already banned.");
                    return;
                }
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                        return;

                    PaginationTools.SendPage(args.Player, pageNumber,
                        PaginationTools.BuildLinesFromTerms(Database.GetItemBans().Select(ban =>
                        {
                            Item i = new Item();
                            i.netDefaults(ban.ID);
                            return $"{i.Name} ({i.netID}, {(ban.Whitelist ? "unbanned" : "banned")})";
                        })),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "World Item bans on current world ({0}/{1}):",
                            FooterFormat = $"Type {TShock.Config.CommandSpecifier}witemban list {{0}} for more.",
                            NothingToDisplayString = "There are currently no banned items on current world."
                        });
                    return;
                }
                default:
                {
                    args.Player.SendSuccessMessage("World Item Ban Sub-Commands:");
                    args.Player.SendInfoMessage("add <item name or id> <whitelist/w/blacklist/b> - Adds an item ban on current world.\n" +
                        "del <item name or id> - Deletes an item ban on current world.\n" +
                        "list [page] - Lists all item bans on current world.");
                    return;
                }
            }
        }

        #endregion
        #region ProjectileBan

        private static void ProjectileBan(CommandArgs args)
        {
            switch (args.Parameters[0]?.ToLower())
            {
                case "add":
                {
                    if ((args.Parameters.Count != 3) || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage(TShock.Config.CommandSpecifier +
                            "wprojban add <proj id> <whitelist/w/blacklist/b>");
                        return;
                    }
                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id <= 0) || (id >= Main.maxProjectileTypes))
                    {
                        args.Player.SendErrorMessage("Invalid projectile ID.");
                        return;
                    }

                    if (whitelist && TShock.ProjectileBans.ProjectileBans.Any(b => (b.ID == id)))
                    {
                        if (Database.AddProjectileBan(id, true))
                        {
                            TShock.ProjectileBans.ProjectileBans.RemoveAll(b => (b.ID == id));
                            args.Player.SendSuccessMessage($"Unbanned projectile {id} on current world.");
                        }
                        else
                            args.Player.SendErrorMessage($"Projectile {id} is not banned on current world.");
                    }
                    else if (!whitelist && !TShock.ProjectileBans.ProjectileBans.Any(b => (b.ID == id)))
                    {
                        if (Database.AddProjectileBan(id, false))
                        {
                            TShock.ProjectileBans.ProjectileBans.Add(new ProjectileBan(id));
                            args.Player.SendSuccessMessage($"Banned {id} on current world.");
                        }
                        else
                            args.Player.SendErrorMessage($"Projectile {id} is already banned on current world.");
                    }
                    else if (whitelist)
                        args.Player.SendErrorMessage($"Projectile {id} is not banned.");
                    else
                        args.Player.SendErrorMessage($"Projectile {id} is already banned.");
                    return;
                }
                case "del":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage(TShock.Config.CommandSpecifier +
                            "wprojban del <id>");
                        return;
                    }
                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id <= 0) || (id >= Main.maxProjectileTypes))
                    {
                        args.Player.SendErrorMessage("Invalid projectile ID.");
                        return;
                    }

                    if (Database.RemoveProjectileBan(id, out bool whitelist))
                    {
                        if (whitelist)
                        {
                            TShock.ProjectileBans.ProjectileBans.Add(new ProjectileBan(id));
                            args.Player.SendSuccessMessage($"Banned projectile {id}.");
                        }
                        else
                        {
                            TShock.ProjectileBans.ProjectileBans.RemoveAll(b => (b.ID == id));
                            args.Player.SendSuccessMessage($"Unbanned projectile {id}.");
                        }
                    }
                    else if (whitelist)
                        args.Player.SendErrorMessage($"Projectile {id} is not banned.");
                    else
                        args.Player.SendErrorMessage($"Projectile {id} is already banned.");
                    return;
                }
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                        return;

                    PaginationTools.SendPage(args.Player, pageNumber,
                        PaginationTools.BuildLinesFromTerms(Database.GetProjectileBans().Select(ban =>
                            $"{ban.ID} ({(ban.Whitelist ? "unbanned" : "banned")})")),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Projectile bans on current world ({0}/{1}):",
                            FooterFormat = $"Type {TShock.Config.CommandSpecifier}wprojban list {{0}} for more.",
                            NothingToDisplayString = "There are currently no banned projectiles on current world."
                        });
                    return;
                }
                default:
                {
                    args.Player.SendSuccessMessage("World Projectile Ban Sub-Commands:");
                    args.Player.SendInfoMessage("add <projectile ID> <whitelist/w/blacklist/b> - Adds a projectile ban on current world.\n" +
                        "del <projectile ID> - Deletes an projectile ban on current world.\n" +
                        "list [page] - Lists all projectile bans on current world.");
                    return;
                }
            }
        }

        #endregion
        #region TileBan

        private static void TileBan(CommandArgs args)
        {
            switch (args.Parameters[0]?.ToLower())
            {
                case "add":
                {
                    if ((args.Parameters.Count != 3) || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage(TShock.Config.CommandSpecifier +
                            "wtileban add <tile id> <whitelist/w/blacklist/b>");
                        return;
                    }

                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id < 0) || (id >= Main.maxTileSets))
                    {
                        args.Player.SendErrorMessage("Invalid tile ID.");
                        return;
                    }

                    if (whitelist && TShock.TileBans.TileBans.Any(b => (b.ID == id)))
                    {
                        if (Database.AddTileBan(id, true))
                        {
                            TShock.TileBans.TileBans.RemoveAll(b => (b.ID == id));
                            args.Player.SendSuccessMessage($"Unbanned tile {id} on current world.");
                        }
                        else
                            args.Player.SendErrorMessage($"Tile {id} is not banned on current world.");
                    }
                    else if (!whitelist && !TShock.TileBans.TileBans.Any(b => (b.ID == id)))
                    {
                        if (Database.AddTileBan(id, false))
                        {
                            TShock.TileBans.TileBans.Add(new TileBan(id));
                            args.Player.SendSuccessMessage($"Banned tile {id} on current world.");
                        }
                        else
                            args.Player.SendErrorMessage($"Tile {id} is already banned on current world.");
                    }
                    else if (whitelist)
                        args.Player.SendErrorMessage($"Tile {id} is not banned.");
                    else
                        args.Player.SendErrorMessage($"Tile {id} is already banned.");
                    return;
                }
                case "del":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage(TShock.Config.CommandSpecifier +
                            "wtileban del <id>");
                        return;
                    }

                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id < 0) || (id >= Main.maxTileSets))
                    {
                        args.Player.SendErrorMessage("Invalid tile ID.");
                        return;
                    }

                    if (Database.RemoveTileBan(id, out bool whitelist))
                    {
                        if (whitelist)
                        {
                            TShock.TileBans.TileBans.Add(new TileBan(id));
                            args.Player.SendSuccessMessage($"Banned tile {id}.");
                        }
                        else
                        {
                            TShock.TileBans.TileBans.RemoveAll(b => (b.ID == id));
                            args.Player.SendSuccessMessage($"Unbanned tile {id}.");
                        }
                    }
                    else if (whitelist)
                        args.Player.SendErrorMessage($"Tile {id} is not banned.");
                    else
                        args.Player.SendErrorMessage($"Tile {id} is already banned.");
                    return;
                }
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pageNumber))
                        return;

                    PaginationTools.SendPage(args.Player, pageNumber,
                        PaginationTools.BuildLinesFromTerms(Database.GetTileBans().Select(ban =>
                            $"{ban.ID} ({(ban.Whitelist ? "unbanned" : "banned")})")),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Tile bans on current world ({0}/{1}):",
                            FooterFormat = $"Type {TShock.Config.CommandSpecifier}wtileban list {{0}} for more.",
                            NothingToDisplayString = "There are currently no banned tiles on current world."
                        });
                    return;
                }
                default:
                {
                    args.Player.SendSuccessMessage("World Tile Ban Sub-Commands:");
                    args.Player.SendInfoMessage("add <tile ID> <whitelist/w/blacklist/b> - Adds a tile ban on current world.\n" +
                        "del <tile ID> - Deletes an tile ban on current world.\n" +
                        "list [page] - Lists all tile bans on current world.");
                    return;
                }
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