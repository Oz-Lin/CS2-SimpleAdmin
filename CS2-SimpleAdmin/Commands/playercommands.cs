using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_SimpleAdmin;

public partial class CS2_SimpleAdmin
{
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 2, usage: "<#userid or name> [<ct/tt/spec>] [-k]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTeamCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;
        var teamName = command.GetArg(2).ToLower();
        string _teamName;
        var teamNum = CsTeam.Spectator;

        var targets = GetTarget(command);
        if (targets == null) return;

        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        switch (teamName)
        {
            case "ct":
            case "counterterrorist":
                teamNum = CsTeam.CounterTerrorist;
                _teamName = "CT";
                break;

            case "t":
            case "tt":
            case "terrorist":
                teamNum = CsTeam.Terrorist;
                _teamName = "TT";
                break;

            case "swap":
                _teamName = "SWAP";
                break;

            default:
                teamNum = CsTeam.Spectator;
                _teamName = "SPEC";
                break;
        }

        var kill = command.GetArg(3).ToLower().Equals("-k");

        playersToTarget.ForEach(player =>
        {
            ChangeTeam(caller, player, _teamName, teamNum, kill, command);
        });
    }

    internal static void ChangeTeam(CCSPlayerController? caller, CCSPlayerController player, string teamName, CsTeam teamNum, bool kill, CommandInfo? command = null)
    {
        // Check if the player is valid and connected
        if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
            return;

        // Ensure the caller can target the player
        if (!caller.CanTarget(player)) return;

        // Set default caller name if not provided
        var callerName = caller != null ? caller.PlayerName : _localizer?["sa_console"] ?? "Console";

        // Change team based on the provided teamName and conditions
        if (!teamName.Equals("swap", StringComparison.OrdinalIgnoreCase))
        {
            if (player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && teamNum != CsTeam.Spectator && !kill && Instance.Config.OtherSettings.TeamSwitchType == 1)
                player.SwitchTeam(teamNum);
            else
                player.ChangeTeam(teamNum);
        }
        else
        {
            if (player.TeamNum != (byte)CsTeam.Spectator)
            {
                var _teamNum = (CsTeam)player.TeamNum == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
                teamName = _teamNum == CsTeam.Terrorist ? "TT" : "CT";
                if (player.PlayerPawn?.Value?.LifeState == (int)LifeState_t.LIFE_ALIVE && !kill && Instance.Config.OtherSettings.TeamSwitchType == 1)
                    player.SwitchTeam(_teamNum);
                else
                    player.ChangeTeam(_teamNum);
            }
        }

        // Log the command
        if (command == null)
            Helper.LogCommand(caller, $"css_team {player.PlayerName} {teamName}");
        else
            Helper.LogCommand(caller, command);

        // Determine message key and arguments for the team change notification
        var activityMessageKey = "sa_admin_team_message";
        var adminActivityArgs = new object[] { "CALLER", player.PlayerName, teamName };

        // Display admin activity message to other players
        if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

        Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
    }

    [CommandHelper(1, "<#userid or name> <new name>")]
    [RequiresPermissions("@css/kick")]
    public void OnRenameCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Set default caller name if not provided
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Get the new name from the command arguments
        var newName = command.GetArg(2);

        // Check if the new name is valid
        if (string.IsNullOrEmpty(newName))
            return;

        // Retrieve the targets based on the command
        var targets = GetTarget(command);
        if (targets == null) return;

        // Filter out valid players from the targets
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to rename
        playersToTarget.ForEach(player =>
        {
            // Check if the player is connected and can be targeted
            if (player.Connected != PlayerConnectedState.PlayerConnected || !caller!.CanTarget(player))
                return;
            
            // Determine message key and arguments for the rename notification
            var activityMessageKey = "sa_admin_rename_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName, newName };

            // Display admin activity message to other players
            if (caller != null && SilentPlayers.Contains(caller.Slot)) return;

            Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
            
            // Rename the player
            player.Rename(newName);
        });
    }

    [CommandHelper(1, "<#userid or name> <new name>")]
    [RequiresPermissions("@css/ban")]
    public void OnPrenameCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Set default caller name if not provided
        var callerName = caller == null ? _localizer?["sa_console"] ?? "Console" : caller.PlayerName;

        // Get the new name from the command arguments
        var newName = command.GetArg(2);

        // Retrieve the targets based on the command
        var targets = GetTarget(command);
        if (targets == null) return;

        // Filter out valid players from the targets
        var playersToTarget = targets.Players.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to rename
        playersToTarget.ForEach(player =>
        {
            // Check if the player is connected and can be targeted
            if (player.Connected != PlayerConnectedState.PlayerConnected || !caller!.CanTarget(player))
                return;
            
            // Determine message key and arguments for the rename notification
            var activityMessageKey = "sa_admin_rename_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName, newName };

            // Display admin activity message to other players
            if (caller != null && !SilentPlayers.Contains(caller.Slot))
            {
                Helper.ShowAdminActivity(activityMessageKey, callerName, false, adminActivityArgs);
            }
            
            // Determine if the new name is valid and update the renamed players list
            if (!string.IsNullOrEmpty(newName))
            {
                RenamedPlayers[player.SteamID] = newName;
                player.Rename(newName);
            }
            else
            {
                RenamedPlayers.Remove(player.SteamID);
            }
        });
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/kick")]
    public void OnGotoCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Check if the caller is valid and has a live pawn
        if (caller == null || caller.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE) return;

        // Get the target players
        var targets = GetTarget(command);
        if (targets == null || targets.Count() > 1) return;

        var playersToTarget = targets.Players
            .Where(player => player is { IsValid: true, IsHLTV: false })
            .ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to teleport
        foreach (var player in playersToTarget.Where(player => player is { Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).Where(caller.CanTarget))
        {
            if (caller.PlayerPawn.Value == null || player.PlayerPawn.Value == null)
                continue;

            // Teleport the caller to the player and toggle noclip
            caller.TeleportPlayer(player);
            // caller.PlayerPawn.Value.ToggleNoclip();

            caller.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            caller.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            
            Utilities.SetStateChanged(caller, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(caller, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            
            player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            
            Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

            // Set a timer to toggle collision back after 4 seconds
            AddTimer(4, () =>
            {
                if (!caller.IsValid || caller.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE)
                    return;
                
                caller.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                caller.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            
                Utilities.SetStateChanged(caller, "CCollisionProperty", "m_CollisionGroup");
                Utilities.SetStateChanged(caller, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
                
                player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            
                Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            });

            // Prepare message key and arguments for the teleport notification
            var activityMessageKey = "sa_admin_tp_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName };

            // Show admin activity
            if (!SilentPlayers.Contains(caller.Slot) && _localizer != null)
            {
                Helper.ShowAdminActivity(activityMessageKey, caller.PlayerName, false, adminActivityArgs);
            }
        }
    }

    [CommandHelper(1, "<#userid or name>")]
    [RequiresPermissions("@css/kick")]
    public void OnBringCommand(CCSPlayerController? caller, CommandInfo command)
    {
        // Check if the caller is valid and has a live pawn
        if (caller == null || caller.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE) 
            return;

        // Get the target players
        var targets = GetTarget(command);
        if (targets == null || targets.Count() > 1) return;

        var playersToTarget = targets.Players
            .Where(player => player is { IsValid: true, IsHLTV: false })
            .ToList();

        // Log the command
        Helper.LogCommand(caller, command);

        // Process each player to teleport
        foreach (var player in playersToTarget.Where(player => player is { Connected: PlayerConnectedState.PlayerConnected, PlayerPawn.Value.LifeState: (int)LifeState_t.LIFE_ALIVE }).Where(caller.CanTarget))
        {
            if (caller.PlayerPawn.Value == null || player.PlayerPawn.Value == null)
                continue;

            // Teleport the player to the caller and toggle noclip
            player.TeleportPlayer(caller);
            // caller.PlayerPawn.Value.ToggleNoclip();
            
            caller.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            caller.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            
            Utilities.SetStateChanged(caller, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(caller, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            
            player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            
            Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");

            // Set a timer to toggle collision back after 4 seconds
            AddTimer(4, () =>
            {
                if (!player.IsValid || player.PlayerPawn?.Value?.LifeState != (int)LifeState_t.LIFE_ALIVE)
                    return;
                
                caller.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                caller.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            
                Utilities.SetStateChanged(caller, "CCollisionProperty", "m_CollisionGroup");
                Utilities.SetStateChanged(caller, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
                
                player.PlayerPawn.Value.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
                player.PlayerPawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER;
            
                Utilities.SetStateChanged(player, "CCollisionProperty", "m_CollisionGroup");
                Utilities.SetStateChanged(player, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
            });

            // Prepare message key and arguments for the bring notification
            var activityMessageKey = "sa_admin_bring_message";
            var adminActivityArgs = new object[] { "CALLER", player.PlayerName };

            // Show admin activity
            if (!SilentPlayers.Contains(caller.Slot) && _localizer != null)
            {
                Helper.ShowAdminActivity(activityMessageKey, caller.PlayerName, false, adminActivityArgs);
            }
        }
    }
}