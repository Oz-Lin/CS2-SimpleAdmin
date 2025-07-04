using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;

namespace CS2_SimpleAdmin.Menus;

public static class ManageAdminsMenu
{
    public static void OpenMenu(CCSPlayerController admin, BaseMenu prevMenu)
    {
        if (admin.IsValid == false)
            return;

        var localizer = CS2_SimpleAdmin._localizer;
        if (AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root") == false)
        {
            admin.PrintToChat(localizer?["sa_prefix"] ??
                              "[SimpleAdmin] " +
                              (localizer?["sa_no_permission"] ?? "You do not have permissions to use this command")
            );
            return;
        }

        var menu = AdminMenu.CreateMenu(localizer?["sa_menu_admins_manage"] ?? "Admins Manage");
        List<ChatMenuOptionData> options =
        [
            new ChatMenuOptionData(localizer?["sa_admin_add"] ?? "Add Admin",
                () => PlayersMenu.OpenRealPlayersMenu(admin, localizer?["sa_admin_add"] ?? "Add Admin", (p, o) => AddAdminMenu(admin, p, menu),  null, menu)),
            new ChatMenuOptionData(localizer?["sa_admin_remove"] ?? "Remove Admin",
                () => PlayersMenu.OpenAdminPlayersMenu(admin, localizer?["sa_admin_remove"] ?? "Remove Admin", RemoveAdmin,
                    player => player != admin && admin.CanTarget(player), menu)),
            new ChatMenuOptionData(localizer?["sa_admin_reload"] ?? "Reload Admins", () => ReloadAdmins(admin))
        ];

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddItem(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled ? DisableOption.DisableHideNumber : DisableOption.None);
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void AddAdminMenu(CCSPlayerController admin, CCSPlayerController player, BaseMenu prevMenu)
    {
        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_admin_add"] ?? "Add Admin"}: {player.PlayerName}");

        foreach (var adminFlag in CS2_SimpleAdmin.Instance.Config.MenuConfigs.AdminFlags)
        {
            bool disabled = AdminManager.PlayerHasPermissions(player, adminFlag.Flag);
            var disableOption = disabled ? DisableOption.DisableHideNumber : DisableOption.None;
            menu?.AddItem(adminFlag.Name, (_, _) => { AddAdmin(admin, player, adminFlag.Flag); }, disableOption);
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void AddAdmin(CCSPlayerController admin, CCSPlayerController player, string flag)
    {
        // TODO: Change default immunity?
        CS2_SimpleAdmin.AddAdmin(admin, player.SteamID.ToString(), player.PlayerName, flag, 10);
    }

    private static void RemoveAdmin(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.Instance.RemoveAdmin(admin, player.SteamID.ToString());
    }

    private static void ReloadAdmins(CCSPlayerController admin)
    {
        CS2_SimpleAdmin.Instance.ReloadAdmins(admin);
    }
}