using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using System.Web;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;

namespace CS2_SimpleAdmin.Menus;

public static class PlayersMenu
{
    public static void OpenRealPlayersMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter, BaseMenu prevMenu)
    {
        OpenMenu(admin, menuName, onSelectAction, p => p.IsBot == false, prevMenu: prevMenu);
    }

    public static void OpenAdminPlayersMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController?, bool>? enableFilter, BaseMenu prevMenu)
    {
        OpenMenu(admin, menuName, onSelectAction, p => AdminManager.GetPlayerAdminData(p)?.Flags.Count > 0, prevMenu:prevMenu);
    }

    public static void OpenAliveMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter, BaseMenu prevMenu)
    {
        OpenMenu(admin, menuName, onSelectAction, p => p.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE, prevMenu:prevMenu);
    }

    public static void OpenDeadMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController?, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter, BaseMenu prevMenu)
    {
        OpenMenu(admin, menuName, onSelectAction, p => p.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE, prevMenu:prevMenu);
    }

    public static void OpenMenu(CCSPlayerController admin, string menuName, Action<CCSPlayerController, CCSPlayerController> onSelectAction, Func<CCSPlayerController, bool>? enableFilter, BaseMenu prevMenu)
    {
        var menu = AdminMenu.CreateMenu(menuName);

        var players = Helper.GetValidPlayersWithBots();

        foreach (var player in players)
        {
            var playerName = player != null && player.PlayerName.Length > 26 ? player.PlayerName[..26] : player?.PlayerName;

            var optionName = HttpUtility.HtmlEncode(playerName);
            if (player != null && enableFilter != null && enableFilter(player) == false)
                continue;

            var enabled = admin.CanTarget(player);
            var disableOption = enabled ? DisableOption.None : DisableOption.DisableHideNumber;

            if (optionName != null)
            {
                menu?.AddItem(optionName, (_, _) =>
                {
                    if (player != null)
                        onSelectAction.Invoke(admin, player);
                }, disableOption);
            }
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }
}