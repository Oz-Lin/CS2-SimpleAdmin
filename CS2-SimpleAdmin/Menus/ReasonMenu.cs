using CounterStrikeSharp.API.Core;
using CS2_SimpleAdmin.Models;
using CS2_SimpleAdminApi;
using CS2MenuManager.API.Class;

namespace CS2_SimpleAdmin.Menus;

public static class ReasonMenu
{
    public static void OpenMenu(CCSPlayerController admin, PenaltyType penaltyType, string menuName, CCSPlayerController player, Action<CCSPlayerController, CCSPlayerController, string> onSelectAction, BaseMenu? prevMenu)
    {
        var menu = AdminMenu.CreateMenu(menuName);

        var reasons = penaltyType switch
        {
            PenaltyType.Ban => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons,
            PenaltyType.Kick => CS2_SimpleAdmin.Instance.Config.MenuConfigs.KickReasons,
            PenaltyType.Mute => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            PenaltyType.Warn => CS2_SimpleAdmin.Instance.Config.MenuConfigs.WarnReasons,
            PenaltyType.Gag => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            PenaltyType.Silence => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            _ => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons
        };

        foreach (var reason in reasons)
        {
            menu?.AddItem(reason, (_, _) => onSelectAction(admin, player, reason));
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    public static void OpenMenu(CCSPlayerController admin, PenaltyType penaltyType, string menuName, DisconnectedPlayer player, Action<CCSPlayerController, DisconnectedPlayer, string> onSelectAction, BaseMenu prevMenu)
    {
        var menu = AdminMenu.CreateMenu(menuName);

        var reasons = penaltyType switch
        {
            PenaltyType.Ban => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons,
            PenaltyType.Kick => CS2_SimpleAdmin.Instance.Config.MenuConfigs.KickReasons,
            PenaltyType.Mute => CS2_SimpleAdmin.Instance.Config.MenuConfigs.MuteReasons,
            PenaltyType.Warn => CS2_SimpleAdmin.Instance.Config.MenuConfigs.WarnReasons,
            _ => CS2_SimpleAdmin.Instance.Config.MenuConfigs.BanReasons
        };

        foreach (var reason in reasons)
        {
            menu?.AddItem(reason, (_, _) => onSelectAction(admin, player, reason));
        }
        
        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }
}