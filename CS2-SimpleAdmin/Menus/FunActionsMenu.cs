using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CS2MenuManager.API.Class;
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

    public static void OpenMenu(CCSPlayerController admin, BaseMenu prevMenu)
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

        if (AdminManager.CommandIsOverriden("css_god")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_god"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/cheats"))
            options.Add(new ChatMenuOptionData(localizer?["sa_godmode"] ?? "God Mode", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_godmode"] ?? "God Mode", GodMode, null, prevMenu: menu)));
        if (AdminManager.CommandIsOverriden("css_noclip")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_noclip"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/cheats"))
            options.Add(new ChatMenuOptionData(localizer?["sa_noclip"] ?? "No Clip", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_noclip"] ?? "No Clip", NoClip, null, prevMenu: menu)));
        if (AdminManager.CommandIsOverriden("css_respawn")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_respawn"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/cheats"))
            options.Add(new ChatMenuOptionData(localizer?["sa_respawn"] ?? "Respawn", () => PlayersMenu.OpenDeadMenu(admin, localizer?["sa_respawn"] ?? "Respawn", Respawn, null, prevMenu: menu)));
        if (AdminManager.CommandIsOverriden("css_give")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_give"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/cheats"))
            options.Add(new ChatMenuOptionData(localizer?["sa_give_weapon"] ?? "Give Weapon", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_give_weapon"] ?? "Give Weapon", (p, o) => GiveWeaponMenu(admin, p, menu), null, menu)));

        if (AdminManager.CommandIsOverriden("css_strip")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_strip"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay"))
            options.Add(new ChatMenuOptionData(localizer?["sa_strip_weapons"] ?? "Strip Weapons", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_strip_weapons"] ?? "Strip Weapons", StripWeapons, null, prevMenu: menu)));
        if (AdminManager.CommandIsOverriden("css_freeze")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_freeze"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay"))
            options.Add(new ChatMenuOptionData(localizer?["sa_freeze"] ?? "Freeze", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_freeze"] ?? "Freeze", Freeze, null, prevMenu: menu)));
        if (AdminManager.CommandIsOverriden("css_hp")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_hp"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay"))
            options.Add(new ChatMenuOptionData(localizer?["sa_set_hp"] ?? "Set Hp", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_hp"] ?? "Set Hp", (p, o) => SetHpMenu(admin, p, menu), null, menu)));
        if (AdminManager.CommandIsOverriden("css_speed")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_speed"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay"))
            options.Add(new ChatMenuOptionData(localizer?["sa_set_speed"] ?? "Set Speed", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_speed"] ?? "Set Speed", (p, o) => SetSpeedMenu(admin, p, menu), null, menu)));
        if (AdminManager.CommandIsOverriden("css_gravity")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_gravity"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay"))
            options.Add(new ChatMenuOptionData(localizer?["sa_set_gravity"] ?? "Set Gravity", () => PlayersMenu.OpenAliveMenu(admin, localizer?["sa_set_gravity"] ?? "Set Gravity", (p, o) => SetGravityMenu(admin, p, menu), null, menu)));
        if (AdminManager.CommandIsOverriden("css_money")
                ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_money"))
                : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/slay"))
            options.Add(new ChatMenuOptionData(localizer?["sa_set_money"] ?? "Set Money", () => PlayersMenu.OpenMenu(admin, localizer?["sa_set_money"] ?? "Set Money", (p, o) => SetMoneyMenu(admin, p, menu), null, menu)));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddItem(menuName, (_, _) => { menuOptionData.Action(); }, menuOptionData.Disabled ? DisableOption.DisableHideNumber : DisableOption.None);
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void GodMode(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.God(admin, player);
    }

    private static void NoClip(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.NoClip(admin, player);
    }

    private static void Respawn(CCSPlayerController? admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.Respawn(admin, player);
    }

    private static void GiveWeaponMenu(CCSPlayerController admin, CCSPlayerController player, BaseMenu prevMenu)
    {
        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_give_weapon"] ?? "Give Weapon"}: {player.PlayerName}");

        foreach (var weapon in GetWeaponsCache)
        {
            menu?.AddItem(weapon.Value.ToString(), (_, _) => { GiveWeapon(admin, player, weapon.Value); });
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void GiveWeapon(CCSPlayerController admin, CCSPlayerController player, CsItem weaponValue)
    {
        CS2_SimpleAdmin.GiveWeapon(admin, player, weaponValue);
    }

    private static void StripWeapons(CCSPlayerController admin, CCSPlayerController player)
    {
        CS2_SimpleAdmin.StripWeapons(admin, player);
    }

    private static void Freeze(CCSPlayerController admin, CCSPlayerController player)
    {
        if (!(player.PlayerPawn.Value?.IsValid ?? false))
            return;

        if (player.PlayerPawn.Value.MoveType != MoveType_t.MOVETYPE_INVALID)
            CS2_SimpleAdmin.Freeze(admin, player, -1);
        else
            CS2_SimpleAdmin.Unfreeze(admin, player);
    }

    private static void SetHpMenu(CCSPlayerController admin, CCSPlayerController player, BaseMenu prevMenu)
    {
        var hpArray = new[]
        {
            new Tuple<string, int>("1", 1),
            new Tuple<string, int>("10", 10),
            new Tuple<string, int>("25", 25),
            new Tuple<string, int>("50", 50),
            new Tuple<string, int>("100", 100),
            new Tuple<string, int>("200", 200),
            new Tuple<string, int>("500", 500),
            new Tuple<string, int>("999", 999)
        };

        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_set_hp"] ?? "Set Hp"}: {player.PlayerName}");

        foreach (var (optionName, value) in hpArray)
        {
            menu?.AddItem(optionName, (_, _) => { SetHp(admin, player, value); });
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void SetHp(CCSPlayerController admin, CCSPlayerController player, int hp)
    {
        CS2_SimpleAdmin.SetHp(admin, player, hp);
    }

    private static void SetSpeedMenu(CCSPlayerController admin, CCSPlayerController player, BaseMenu prevMenu)
    {
        var speedArray = new[]
        {
            new Tuple<string, float>("0.1", .1f),
            new Tuple<string, float>("0.25", .25f),
            new Tuple<string, float>("0.5", .5f),
            new Tuple<string, float>("0.75", .75f),
            new Tuple<string, float>("1", 1),
            new Tuple<string, float>("2", 2),
            new Tuple<string, float>("3", 3),
            new Tuple<string, float>("4", 4)
        };

        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_set_speed"] ?? "Set  Speed"}: {player.PlayerName}");

        foreach (var (optionName, value) in speedArray)
        {
            menu?.AddItem(optionName, (_, _) => { SetSpeed(admin, player, value); });
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void SetSpeed(CCSPlayerController admin, CCSPlayerController player, float speed)
    {
        CS2_SimpleAdmin.SetSpeed(admin, player, speed);
    }

    private static void SetGravityMenu(CCSPlayerController admin, CCSPlayerController player, BaseMenu prevMenu)
    {
        var gravityArray = new[]
        {
            new Tuple<string, float>("0.1", .1f),
            new Tuple<string, float>("0.25", .25f),
            new Tuple<string, float>("0.5", .5f),
            new Tuple<string, float>("0.75", .75f),
            new Tuple<string, float>("1", 1),
            new Tuple<string, float>("2", 2)
        };

        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_set_gravity"] ?? "Set Gravity"}: {player.PlayerName}");

        foreach (var (optionName, value) in gravityArray)
        {
            menu?.AddItem(optionName, (_, _) => { SetGravity(admin, player, value); });
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void SetGravity(CCSPlayerController admin, CCSPlayerController player, float gravity)
    {
        CS2_SimpleAdmin.SetGravity(admin, player, gravity);
    }

    private static void SetMoneyMenu(CCSPlayerController admin, CCSPlayerController player, BaseMenu prevMenu)
    {
        var moneyArray = new[]
        {
            new Tuple<string, int>("$0", 0),
            new Tuple<string, int>("$1000", 1000),
            new Tuple<string, int>("$2500", 2500),
            new Tuple<string, int>("$5000", 5000),
            new Tuple<string, int>("$10000", 10000),
            new Tuple<string, int>("$16000", 16000)
        };

        var menu = AdminMenu.CreateMenu($"{CS2_SimpleAdmin._localizer?["sa_set_money"] ?? "Set Money"}: {player.PlayerName}");

        foreach (var (optionName, value) in moneyArray)
        {
            menu?.AddItem(optionName, (_, _) => { SetMoney(admin, player, value); });
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void SetMoney(CCSPlayerController admin, CCSPlayerController player, int money)
    {
        CS2_SimpleAdmin.SetMoney(admin, player, money);
    }
}