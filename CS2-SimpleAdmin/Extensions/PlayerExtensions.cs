using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using System.Text;
using CounterStrikeSharp.API.Modules.UserMessages;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace CS2_SimpleAdmin;

public static class PlayerExtensions
{
    public static void Print(this CCSPlayerController controller, string message = "")
    {
        StringBuilder _message = new(CS2_SimpleAdmin._localizer!["sa_prefix"]);
        _message.Append(message);
        controller.PrintToChat(_message.ToString());
    }

    public static bool CanTarget(this CCSPlayerController? controller, CCSPlayerController? target)
    {
        if (controller is null || target is null) return true;
        if (target.IsBot) return true;

        return AdminManager.CanPlayerTarget(controller, target) ||
                                  AdminManager.CanPlayerTarget(new SteamID(controller.SteamID),
                                      new SteamID(target.SteamID)) || 
                                      AdminManager.GetPlayerImmunity(controller) >= AdminManager.GetPlayerImmunity(target);
    }

    public static bool CanTarget(this CCSPlayerController? controller, SteamID steamId)
    {
        if (controller is null) return true;

        return AdminManager.CanPlayerTarget(new SteamID(controller.SteamID), steamId) || 
               AdminManager.GetPlayerImmunity(controller) >= AdminManager.GetPlayerImmunity(steamId);
    }

    public static void ToggleNoclip(this CBasePlayerPawn pawn)
    {
        if (pawn.MoveType == MoveType_t.MOVETYPE_NOCLIP)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2); // walk
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
        else
        {
            pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 8); // noclip
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
    }

    public static void Rename(this CCSPlayerController? controller, string newName = "Unknown")
    {
        newName ??= CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";

        if (controller != null)
        {
            var playerName = new SchemaString<CBasePlayerController>(controller, "m_iszPlayerName");
            playerName.Set(newName + " ");

            CS2_SimpleAdmin.Instance.AddTimer(0.25f, () =>
            {
                Utilities.SetStateChanged(controller, "CCSPlayerController", "m_szClan");
                Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
            });

            CS2_SimpleAdmin.Instance.AddTimer(0.3f, () =>
            {
                playerName.Set(newName);
            });
        }

        CS2_SimpleAdmin.Instance.AddTimer(0.4f, () =>
        {
            if (controller != null) Utilities.SetStateChanged(controller, "CBasePlayerController", "m_iszPlayerName");
        });
    }

    public static void TeleportPlayer(this CCSPlayerController? controller, CCSPlayerController? target)
    {
        if (controller?.PlayerPawn.Value == null && target?.PlayerPawn.Value == null)
            return;

        if (
            controller?.PlayerPawn.Value is { AbsOrigin: not null, AbsRotation: not null } &&
            target?.PlayerPawn.Value is { AbsOrigin: not null, AbsRotation: not null }
        )
        {
            controller.PlayerPawn.Value.Teleport(
                target.PlayerPawn.Value.AbsOrigin,
                target.PlayerPawn.Value.AbsRotation,
                target.PlayerPawn.Value.AbsVelocity
            );
        }
    }

    public static void SendLocalizedMessage(this CCSPlayerController? controller, IStringLocalizer? localizer,
        string messageKey, params object[] messageArgs)
    {
        if (controller == null || localizer == null) return;

        using (new WithTemporaryCulture(controller.GetLanguage()))
        {
            StringBuilder sb = new();
            sb.Append(localizer[messageKey, messageArgs]);

            foreach (var part in Helper.SeparateLines(sb.ToString()))
            {
                var lineWithPrefix = localizer["sa_prefix"] + part.Trim();
                controller.PrintToChat(lineWithPrefix);
            }
        }
    }

    public static void SendLocalizedMessageCenter(this CCSPlayerController? controller, IStringLocalizer? localizer,
        string messageKey, params object[] messageArgs)
    {
        if (controller == null || localizer == null) return;

        using (new WithTemporaryCulture(controller.GetLanguage()))
        {
            StringBuilder sb = new();
            sb.Append(localizer[messageKey, messageArgs]);

            foreach (var part in Helper.SeparateLines(sb.ToString()))
            {
                string _part;
                _part = Helper.CenterMessage(part);
                var lineWithPrefix = localizer["sa_prefix"] + _part;
                controller.PrintToChat(lineWithPrefix);
            }
        }
    }
}