using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CS2MenuManager.API.Enum;

namespace CS2_SimpleAdmin.Menus;

public static class FunActionsMenu
{
    private static Dictionary<int, CsItem>? _weaponsCache;

    private static Dictionary<int, CsItem> GetWeaponsCache
    {
        get
        {
            if (_weaponsCache != null) return _weaponsCache;

            var weaponsArray = Enum.GetValues(typeof(CsItem));

            // avoid duplicates in the menu
            _weaponsCache = new Dictionary<int, CsItem>();
            foreach (CsItem item in weaponsArray)
            {
                if (item == CsItem.Tablet)
                    continue;

                _weaponsCache[(int)item] = item;
            }

            return _weaponsCache;
        }
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

        var menu = AdminMenu.CreateMenu(localizer?["sa_menu_fun_commands"] ?? "Fun Commands");
        List<ChatMenuOptionData> options = [];

        //var hasCheats = AdminManager.PlayerHasPermissions(admin, "@css/cheats");
        //var hasSlay = AdminManager.PlayerHasPermissions(admin, "@css/slay");

        // options added in order

        if (AdminManager.CommandIsOverriden("css_noclip")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_noclip"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/cheats"))
            options.Add(new ChatMenuOptionData(localizer?["sa_noclip"] ?? "No Clip", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_noclip"] ?? "No Clip", NoClip)));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddItem(menuName, (_, _) => { menuOptionData.Action(); }, menuOptionData.Disabled ? DisableOption.DisableHideNumber : DisableOption.None);
        }

        if (menu != null) AdminMenu.OpenMenu(admin, menu);
    }

    private static void NoClip(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.NoClip(admin, player);
    }
}