using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
//using CounterStrikeSharp.API.Modules.Menu;
using CS2MenuManager;
using CS2MenuManager.API.Menu;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Interface;

namespace CS2_SimpleAdmin.Menus;

public static class AdminMenu
{
    public static IMenu CreateMenu(string title)
    {
        return new ScreenMenu(title, CS2_SimpleAdmin.Instance);
    }

    public static void OpenMenu(CCSPlayerController admin, IMenu menu)
    {
        if (menu is ScreenMenu screenMenu)
        {
            // 0 = infinite (must be explicitly closed)
            screenMenu.Display(admin, 0);
        }
    }

    // Example: an overload that also opens a submenu (if needed)
    public static void OpenMenu(CCSPlayerController admin)
    {
        if (admin == null || !admin.IsValid)
            return;

        var localizer = CS2_SimpleAdmin._localizer;
        if (!AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/generic"))
        {
            admin.PrintToChat(localizer?["sa_prefix"] ?? "[SimpleAdmin] " +
                (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command"));
            return;
        }

        var menu = CreateMenu(localizer?["sa_title"] ?? "SimpleAdmin");
        // Add menu options as needed
        List<ChatMenuOptionData> options = new List<ChatMenuOptionData>
        {
            new ChatMenuOptionData(localizer?["sa_menu_players_manage"] ?? "Manage Players", () => ManagePlayersMenu.OpenMenu(admin)),
            new ChatMenuOptionData(localizer?["sa_menu_server_manage"] ?? "Manage Server", () => ManageServerMenu.OpenMenu(admin)),
            new ChatMenuOptionData(localizer?["sa_menu_fun_commands"] ?? "Fun Commands", () => FunActionsMenu.OpenMenu(admin))
        };
        var customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands;
        if (customCommands.Count > 0)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_custom_commands"] ?? "Custom Commands", () => CustomCommandsMenu.OpenMenu(admin)));
        }
        if (AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root"))
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_admins_manage"] ?? "Admins Manage", () => ManageAdminsMenu.OpenMenu(admin)));
        // (Add more options as needed.)

        foreach (var menuOptionData in options)
        {
            menu?.AddItem(
                menuOptionData.Name,
                (player, option) => menuOptionData.Action.Invoke(),
                menuOptionData.Disabled ? DisableOption.DisableHideNumber : DisableOption.None
            );

        }

        if (menu != null)
            OpenMenu(admin, menu);
    }
}