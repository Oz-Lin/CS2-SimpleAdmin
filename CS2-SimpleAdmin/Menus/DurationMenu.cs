using CounterStrikeSharp.API.Core;
using CS2_SimpleAdmin.Models;
using CS2MenuManager.API.Class;

namespace CS2_SimpleAdmin.Menus;

public static class DurationMenu
{
    public static void OpenMenu(CCSPlayerController admin, string menuName, CCSPlayerController player, Action<CCSPlayerController, CCSPlayerController, int> onSelectAction, BaseMenu? prevMenu)
    {
        var menu = AdminMenu.CreateMenu(menuName);

        foreach (var durationItem in CS2_SimpleAdmin.Instance.Config.MenuConfigs.Durations)
        {
            menu.AddItem(durationItem.Name, (_, _) => { onSelectAction(admin, player, durationItem.Duration); });
        }

        menu.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    public static void OpenMenu(CCSPlayerController admin, string menuName, DisconnectedPlayer player, Action<CCSPlayerController, DisconnectedPlayer, int> onSelectAction, BaseMenu? prevMenu)
    {
        var menu = AdminMenu.CreateMenu(menuName);

        foreach (var durationItem in CS2_SimpleAdmin.Instance.Config.MenuConfigs.Durations)
        {
            menu.AddItem(durationItem.Name, (_, _) => { onSelectAction(admin, player, durationItem.Duration); });
        }

        menu.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

}