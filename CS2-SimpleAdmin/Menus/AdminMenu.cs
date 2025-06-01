using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;

namespace CS2_SimpleAdmin.Menus;

public static class AdminMenu
{
    public static BaseMenu CreateMenu(string title)
    {
        return Helper.CreateMenu(title);
    }

    public static void OpenMenu(CCSPlayerController admin)
    {
        if (admin.IsValid == false)
            return;

        var localizer = CS2_SimpleAdmin._localizer;
        if (AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/generic") == false)
        {
            admin.PrintToChat(localizer?["sa_prefix"] ??
                              "[SimpleAdmin] " +
                              (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
            );
            return;
        }

        var menu = CreateMenu(localizer?["sa_title"] ?? "SimpleAdmin");
        List<ChatMenuOptionData> options =
        [
            new ChatMenuOptionData(localizer?["sa_menu_players_manage"] ?? "Players Manage", () => ManagePlayersMenu.OpenMenu(admin, menu)),
            new ChatMenuOptionData(localizer?["sa_menu_server_manage"] ?? "Server Manage", () => ManageServerMenu.OpenMenu(admin, menu)),
            new ChatMenuOptionData(localizer?["sa_menu_fun_commands"] ?? "Fun Commands", () => FunActionsMenu.OpenMenu(admin, menu)),
        ];

        var customCommands = CS2_SimpleAdmin.Instance.Config.CustomServerCommands;
        if (customCommands.Count > 0)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_custom_commands"] ?? "Custom Commands", () => CustomCommandsMenu.OpenMenu(admin, menu)));
        }

        if (AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root"))
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_admins_manage"] ?? "Admins Manage", () => ManageAdminsMenu.OpenMenu(admin, menu)));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu.AddItem(menuName, (_, _) => { menuOptionData.Action.Invoke(); },
                menuOptionData.Disabled ? DisableOption.DisableHideNumber : DisableOption.None);
        }

        menu.Display(admin, 0);
    }
}