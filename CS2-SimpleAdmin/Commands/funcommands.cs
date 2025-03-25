using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/cheats")]
    public void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        var targets = GetTarget(command);
        if (targets == null) return;
        var playersToTarget = targets.Players.Where(player =>
            player.IsValid &&
            player is { IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).ToList();

        playersToTarget.ForEach(player =>
        {
            if (caller!.CanTarget(player))
            {
                NoClip(caller, player, callerName);
            }
        });
    }

    internal static void NoClip(CCSPlayerController? caller, CCSPlayerController player, string? callerName = null, CommandInfo? command = null)
    {
        if (!player.IsValid) return;
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        callerName ??= caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Toggle no-clip mode for the player
        player.Pawn.Value?.ToggleNoclip();

        // Determine message keys and arguments for the no-clip notification
        var (activityMessageKey, adminActivityArgs) =
            ("sa_admin_noclip_message",
                new object[] { "CALLER", player.PlayerName });

        // Display admin activity message to other players
        if (caller == null || !SilentPlayers.Contains(caller.Slot))
        {
            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
        }

        // Log the command
        if (command == null)
        {
            Helper.LogCommand(caller, $"css_noclip {(string.IsNullOrEmpty(player.PlayerName) ? player.SteamID.ToString() : player.PlayerName)}");
        }
        else
        {
            Helper.LogCommand(caller, command);
        }
    }
}