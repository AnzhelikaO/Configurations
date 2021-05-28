#region Using
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
#endregion
namespace Configurations
{
    internal static class WorldBanCommands
    {
        #region Register

        private static List<Command> Commands = new List<Command>()
        {
            new Command("custombans.itemban", ItemBan, "witemban"),
            new Command("custombans.projectileban", ProjectileBan, "wprojban"),
            new Command("custombans.tileban", TileBan, "wtileban")
        };
        public static void Register() =>
            TShockAPI.Commands.ChatCommands.AddRange(Commands);
        public static void Deregister() =>
            TShockAPI.Commands.ChatCommands.RemoveAll(c => Commands.Contains(c));

        #endregion

        #region ItemBan

        private static void ItemBan(CommandArgs args)
        {
            switch (args.Parameters.FirstOrDefault()?.ToLower())
            {
                #region Add

                case "+":
                case "a":
                case "add":
                {
                    if ((args.Parameters.Count < 3) || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier +
                            "witemban <add/+> <Item Name or ID> <whitelist/w/blacklist/b>");
                        return;
                    }

                    string name = string.Join(" ", args.Parameters.Skip(1).Take(args.Parameters.Count - 2));
                    List<Item> items = TShock.Utils.GetItemByIdOrName(name);
                    if (items.Count == 0)
                    {
                        args.Player.SendErrorMessage($"Invalid item '{name}'.");
                        return;
                    }
                    else if (items.Count > 1)
                    {
                        args.Player.SendMultipleMatchError(items.Select(i => $"{i.Name}({i.netID})"));
                        return;
                    }
                    Item item = items[0];

                    if (whitelist == Overrides.IsBannedItem(item.netID, out bool locally))
                    {
                        Overrides.OverrideItemBan(item.netID, whitelist);
                        args.Player.SendSuccessMessage($"{(whitelist ? "Unb" : "B")}anned " +
                            $"item {item.Name} in the current world.");
                    }
                    else
                        args.Player.SendErrorMessage($"Item {item.Name} is " +
                            $"{(whitelist ? "not" : "already")} banned" +
                            $"{(locally ? " in the current world" : "")}.");
                    return;
                }

                #endregion
                #region Delete

                case "-":
                case "d":
                case "del":
                case "delete":
                {
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier +
                            "witemban <delete/-> <Item Name or ID>");
                        return;
                    }

                    string name = string.Join(" ", args.Parameters.Skip(1));
                    List<Item> items = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (items.Count == 0)
                    {
                        args.Player.SendErrorMessage($"Invalid item '{args.Parameters[1]}'.");
                        return;
                    }
                    else if (items.Count > 1)
                    {
                        args.Player.SendMultipleMatchError(items.Select(i => $"{i.Name}({i.netID})"));
                        return;
                    }
                    Item item = items[0];

                    if (Overrides.RemoveItemBanOverride(item.netID, out bool banned, out bool whitelist))
                        args.Player.SendSuccessMessage($"{(banned ? "B" : "Unb")}anned " +
                            $"item {item.Name}.");
                    else
                        args.Player.SendErrorMessage($"Item {item.Name} is " +
                            $"{(whitelist ? "not" : "already")} banned in the current world.");
                    return;
                }

                #endregion
                #region List

                case "l":
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters,
                            1, args.Player, out int pageNumber))
                        return;

                    PaginationTools.SendPage(args.Player, pageNumber,
                        PaginationTools.BuildLinesFromTerms(Overrides.ItemBans.Select(ban =>
                        {
                            Item i = new Item();
                            i.netDefaults(ban.Key);
                            return $"{i.Name} ({i.netID}, {(ban.Value ? "un" : "")}banned)";
                        })),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "World Item bans in the current world ({0}/{1}):",
                            FooterFormat =
                                $"Type {TShock.Config.Settings.CommandSpecifier}witemban <list/l> {{0}} for more.",
                            NothingToDisplayString =
                                "There are currently no banned items in the current world."
                        });
                    return;
                }

                #endregion
                #region Help

                default:
                {
                    args.Player.SendSuccessMessage("World Item Ban Sub-Commands:");
                    args.Player.SendInfoMessage("<add/+> <Item Name or ID> <whitelist/w/blacklist/b> - " +
                        "Adds an item ban in the current world.\n" +
                        "<delete/-> <Item Name or ID> - Deletes an item ban in the current world.\n" +
                        "<list/l> [Page] - Lists all item bans in the current world.");
                    return;
                }

                #endregion
            }
        }

        #endregion
        #region ProjectileBan

        private static void ProjectileBan(CommandArgs args)
        {
            switch (args.Parameters.FirstOrDefault()?.ToLower())
            {
                #region Add

                case "+":
                case "a":
                case "add":
                {
                    if ((args.Parameters.Count != 3) || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier +
                            "wprojban <add/+> <Projectile ID> <whitelist/w/blacklist/b>");
                        return;
                    }
                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id <= 0) || (id >= Main.maxProjectileTypes))
                    {
                        args.Player.SendErrorMessage($"Invalid projectile ID '{args.Parameters[1]}'.");
                        return;
                    }

                    if (whitelist == Overrides.IsBannedProjectile(id, out bool locally))
                    {
                        Overrides.OverrideProjectileBan(id, whitelist);
                        args.Player.SendSuccessMessage($"{(whitelist ? "Unb" : "B")}anned " +
                            $"projectile {id} in the current world.");
                    }
                    else
                        args.Player.SendErrorMessage($"Projectile {id} is " +
                            $"{(whitelist ? "not" : "already")} banned" +
                            $"{(locally ? " in the current world" : "")}.");
                    return;
                }

                #endregion
                #region Delete

                case "-":
                case "f":
                case "del":
                case "delete":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier +
                            "wprojban <delete/-> <Projectile ID>");
                        return;
                    }
                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id <= 0) || (id >= Main.maxProjectileTypes))
                    {
                        args.Player.SendErrorMessage($"Invalid projectile ID '{args.Parameters[1]}'.");
                        return;
                    }

                    if (Overrides.RemoveProjectileBanOverride(id, out bool banned, out bool whitelist))
                        args.Player.SendSuccessMessage($"{(banned ? "B" : "Unb")}anned " +
                            $"projectile {id} in the current world.");
                    else
                        args.Player.SendErrorMessage($"Projectile {id} " +
                            $"is {(whitelist ? "not" : "already")} banned in the current world.");
                    return;
                }

                #endregion
                #region List

                case "l":
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters,
                            1, args.Player, out int pageNumber))
                        return;

                    PaginationTools.SendPage(args.Player, pageNumber,
                        PaginationTools.BuildLinesFromTerms(Overrides.ProjectileBans.Select(ban =>
                            $"{ban.Key} ({(ban.Value ? "un" : "")}banned)")),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Projectile bans in the current world ({0}/{1}):",
                            FooterFormat =
                                $"Type {TShock.Config.Settings.CommandSpecifier}wprojban <list/l> {{0}} for more.",
                            NothingToDisplayString =
                                "There are currently no banned projectiles in the current world."
                        });
                    return;
                }

                #endregion
                #region Help

                default:
                {
                    args.Player.SendSuccessMessage("World Projectile Ban Sub-Commands:");
                    args.Player.SendInfoMessage("<add/+> <Projectile ID> <whitelist/w/blacklist/b> - " +
                        "Adds a projectile ban in the current world.\n" +
                        "<delete/-> <Projectile ID> - Deletes an projectile ban in the current world.\n" +
                        "<list/l> [Page] - Lists all projectile bans in the current world.");
                    return;
                }

                #endregion
            }
        }

        #endregion
        #region TileBan

        private static void TileBan(CommandArgs args)
        {
            switch (args.Parameters.FirstOrDefault()?.ToLower())
            {
                #region Add

                case "+":
                case "a":
                case "add":
                {
                    if ((args.Parameters.Count != 3) || !GetType(args, out bool whitelist))
                    {
                        args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier +
                            "wtileban <add/+> <Tile ID> <whitelist/w/blacklist/b>");
                        return;
                    }

                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id < 0) || (id >= Main.maxTileSets))
                    {
                        args.Player.SendErrorMessage($"Invalid tile ID '{args.Parameters[1]}'.");
                        return;
                    }

                    if (whitelist == Overrides.IsBannedTile(id, out bool locally))
                    {
                        Overrides.OverrideTileBan(id, whitelist);
                        args.Player.SendSuccessMessage($"{(whitelist ? "Unb" : "B")}anned " +
                            $"tile {id} in the current world.");
                    }
                    else
                        args.Player.SendErrorMessage($"Tile {id} is " +
                            $"{(whitelist ? "not" : "already")} banned" +
                            $"{(locally ? " in the current world" : "")}.");
                    return;
                }

                #endregion
                #region Delete

                case "del":
                {
                    if (args.Parameters.Count != 2)
                    {
                        args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier +
                            "wtileban <delete/-> <Tile ID>");
                        return;
                    }

                    if (!short.TryParse(args.Parameters[1], out short id)
                        || (id < 0) || (id >= Main.maxTileSets))
                    {
                        args.Player.SendErrorMessage($"Invalid tile ID '{args.Parameters[1]}'.");
                        return;
                    }

                    if (Overrides.RemoveTileBanOverride(id, out bool banned, out bool whitelist))
                        args.Player.SendSuccessMessage($"{(banned ? "B" : "Unb")}anned tile {id}.");
                    else
                        args.Player.SendErrorMessage($"Tile {id} is " +
                            $"{(whitelist ? "not" : "already")} banned in the current world.");
                    return;
                }

                #endregion
                #region List

                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters,
                            1, args.Player, out int pageNumber))
                        return;

                    PaginationTools.SendPage(args.Player, pageNumber,
                        PaginationTools.BuildLinesFromTerms(Overrides.TileBans.Select(ban =>
                            $"{ban.Key} ({(ban.Value ? "un" : "")}banned)")),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Tile bans in the current world ({0}/{1}):",
                            FooterFormat =
                                $"Type {TShock.Config.Settings.CommandSpecifier}wtileban <list/l> {{0}} for more.",
                            NothingToDisplayString =
                                "There are currently no banned tiles in the current world."
                        });
                    return;
                }

                #endregion
                #region Help

                default:
                {
                    args.Player.SendSuccessMessage("World Tile Ban Sub-Commands:");
                    args.Player.SendInfoMessage("<add/+> <Tile ID> <whitelist/w/blacklist/b> - " +
                        "Adds a tile ban in the current world.\n" +
                        "<delete/-> <tile ID> - Deletes an tile ban in the current world.\n" +
                        "<list/l> [Page] - Lists all tile bans in the current world.");
                    return;
                }

                #endregion
            }
        }

        #endregion

        #region GetType

        private static bool GetType(CommandArgs args, out bool Whitelist)
        {
            switch (args.Parameters.Last().ToLower())
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