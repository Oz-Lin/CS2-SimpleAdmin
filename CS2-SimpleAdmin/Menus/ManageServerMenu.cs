using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Modules.Timers;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CS2MenuManager.API.Interface;

namespace CS2_SimpleAdmin.Menus;

public static class ManageServerMenu
{
    // Add: cache for workshop maps to avoid reading file every time
    private static Dictionary<string, string>? _workshopMapCache = null;

    // Add: ensure cache is loaded
    private static void EnsureWorkshopMapCache(string filePath)
    {
        if (_workshopMapCache != null) return;
        _workshopMapCache = new Dictionary<string, string>();
        if (!File.Exists(filePath)) return;
        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//")) continue;
            var parts = line.Split(':');
            if (parts.Length < 2) continue;
            var name = parts[0].Trim();
            var id = parts[1].Trim();
            // Only take the map name (content after removing spaces)
            var workshopName = name.Split(' ')[0];
            _workshopMapCache[workshopName] = id;
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

        var menu = AdminMenu.CreateMenu(localizer?["sa_menu_server_manage"] ?? "Server Manage");
        List<ChatMenuOptionData> options = [];


        // permissions
        var hasMap = AdminManager.CommandIsOverriden("css_map") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_map")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/changemap");
        var hasPlugins = AdminManager.CommandIsOverriden("css_pluginsmanager") ? AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), AdminManager.GetPermissionOverrides("css_pluginsmanager")) : AdminManager.PlayerHasPermissions(new SteamID(admin.SteamID), "@css/root");

        //bool hasMap = AdminManager.PlayerHasPermissions(admin, "@css/changemap");

        // options added in order

        if (hasPlugins)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_menu_pluginsmanager_title"] ?? "Manage Plugins", () => CS2_SimpleAdmin.PluginManagerMenu(admin, menu)));
            //options.Add(new ChatMenuOptionData(localizer?["sa_menu_pluginsmanager_title"] ?? "Manage Plugins", () => admin.ExecuteClientCommandFromServer("css_pluginsmanager")));
        }

        if (hasMap)
        {
            options.Add(new ChatMenuOptionData(localizer?["sa_changemap"] ?? "Change Map", () => ChangeMapMenu(admin, menu)));
        }

        options.Add(new ChatMenuOptionData(localizer?["sa_restart_game"] ?? "Restart Game", () => CS2_SimpleAdmin.RestartGame(admin)));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddItem(menuName, (_, _) => { menuOptionData.Action.Invoke(); }, menuOptionData.Disabled ? DisableOption.DisableHideNumber : DisableOption.None);
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void ChangeMapMenu(CCSPlayerController admin, BaseMenu prevMenu)
    {
        if (admin == null || !admin.IsValid)
            return;

        string filePath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "plugins", "RockTheVote", "maplist.txt");

        var menu = AdminMenu.CreateMenu(CS2_SimpleAdmin._localizer?["sa_changemap"] ?? "Change Map");

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//"))
                    continue;

                // Expected format: "map_name (optional info):workshopID"
                string[] parts = line.Split(':');
                if (parts.Length < 2)
                    continue;

                string mapDisplayName = parts[0].Trim();
                // Grab the actual map name from mapDisplayName by taking the first string before any space.
                string workshopName = mapDisplayName.Split(' ')[0];

                menu.AddItem(mapDisplayName, (player, option) =>
                {
                    EnsureWorkshopMapCache(filePath);
                    if (_workshopMapCache == null || !_workshopMapCache.TryGetValue(workshopName, out var workshopId))
                    {
                        admin.PrintToChat($"Can't find Workshop ID for Map {workshopName}");
                        return;
                    }

                    // try changelevel first
                    Server.ExecuteCommand($"ds_workshop_changelevel {workshopName}");
                    // If changelevel failed, use host_workshop_map
                    admin.PrintToChat($"Trying ds_workshop_changelevel {workshopName}, otherwise host_workshop_map {workshopId} instead.");
                    // If fails to change after 5s, fallback to host_workshop_map
                    Timer timer = new Timer(5.0f, () =>
                    {
                        Server.ExecuteCommand($"host_workshop_map {workshopId}");
                    }, TimerFlags.STOP_ON_MAPCHANGE);

                });
            }
        }
        else
        {
            admin.PrintToChat("Map list file not found at: " + filePath);
        }

        List<ChatMenuOptionData> options = new();

        var wsMaps = CS2_SimpleAdmin.Instance.Config.WorkshopMaps;
        options.AddRange(wsMaps.Select(map => new ChatMenuOptionData(
            $"{map.Key} (WS)",
            () => ExecuteChangeMap(admin, map.Value?.ToString() ?? map.Key, true)
        )));

        foreach (var menuOptionData in options)
        {
            var menuName = menuOptionData.Name;
            menu?.AddItem(menuName, (_, _) => { menuOptionData.Action.Invoke(); },
                menuOptionData.Disabled ? DisableOption.DisableHideNumber : DisableOption.None);
        }

        menu!.PrevMenu = prevMenu;
        menu.Display(admin, 0);
    }

    private static void ExecuteChangeMap(CCSPlayerController admin, string mapName, bool workshop)
    {
        if (workshop)
            CS2_SimpleAdmin.Instance.ChangeWorkshopMap(admin, mapName);
        else
            CS2_SimpleAdmin.Instance.ChangeMap(admin, mapName);
    }
}