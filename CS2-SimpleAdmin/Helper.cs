﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Plugin.Host;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
//using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using CS2_SimpleAdmin.Managers;
using CS2_SimpleAdminApi;
using CS2MenuManager;
using CS2MenuManager.API;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Interface;
using CS2MenuManager.API.Menu;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZLinq;
using static CS2_SimpleAdmin.CS2_SimpleAdmin;
using BaseMenu = CS2MenuManager.API.Class.BaseMenu;


namespace CS2_SimpleAdmin;

internal static class Helper
{
    private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
    private static readonly string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}/{AssemblyName}.json";

    private delegate nint CNetworkSystemUpdatePublicIp(nint a1);

    private static CNetworkSystemUpdatePublicIp? _networkSystemUpdatePublicIp;

    public static bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
			return false;
#endif
        }
    }

    public static List<CCSPlayerController> GetPlayerFromName(string name)
    {
        return Utilities.GetPlayers().FindAll(x => x.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static CCSPlayerController? GetPlayerFromSteamid64(string steamid)
    {
        return GetValidPlayers().FirstOrDefault(x => x.SteamID.ToString().Equals(steamid, StringComparison.OrdinalIgnoreCase));
    }

    public static CCSPlayerController? GetPlayerFromIp(string ipAddress)
    {
        return GetValidPlayers().FirstOrDefault(x => x.IpAddress != null && x.IpAddress.Split(":")[0].Equals(ipAddress));
    }

    public static List<CCSPlayerController> GetValidPlayers()
    {
        return Utilities.GetPlayers().AsValueEnumerable()
            .Where(p => p is { IsValid: true, IsBot: false, Connected: PlayerConnectedState.PlayerConnected })
            .ToList();
    }
    
    public static List<CCSPlayerController> GetValidPlayersWithBots()
    {
        return Utilities.GetPlayers().AsValueEnumerable()
            .Where(p => p is { IsValid: true, IsHLTV: false, Connected: PlayerConnectedState.PlayerConnected }).ToList();
    }


    // public static bool IsValidSteamId64(string input)
    // {
    //     const string pattern = @"^\d{17}$";
    //     return Regex.IsMatch(input, pattern);
    // }

    public static bool ValidateSteamId(string input, out SteamID? steamId)
    {
        steamId = null;

        if (string.IsNullOrEmpty(input))
        {
            return false;
        }
        
        if (!SteamID.TryParse(input, out var parsedSteamId)) return false;

        steamId = parsedSteamId;
        return true;
    }

    public static bool IsValidIp(string input)
    {
        const string pattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
        return Regex.IsMatch(input, pattern);
    }

    public static void GivePlayerFlags(SteamID? steamid, List<string>? flags = null, uint immunity = 0)
    {
        try
        {
            if (steamid == null || (flags == null && immunity == 0))
            {
                return;
            }

            if (flags == null) return;
            foreach (var flag in flags.Where(flag => !string.IsNullOrEmpty(flag)))
            {
                if (flag.StartsWith($"@"))
                {
                    //Console.WriteLine($"Adding permission {flag} to SteamID {steamid}");
                    AdminManager.AddPlayerPermissions(steamid, flag);
                }
                else if (flag.StartsWith($"#"))
                {
                    //Console.WriteLine($"Adding SteamID {steamid} to group {flag}");
                    AdminManager.AddPlayerToGroup(steamid, flag);
                }
            }

            AdminManager.SetPlayerImmunity(steamid, immunity);
        }
        catch
        {
        }
    }

    public static void KickPlayer(int userId, NetworkDisconnectionReason reason = NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED, int delay = 0)
    {
        var player = Utilities.GetPlayerFromUserid(userId);

        if (player == null || !player.IsValid || player.IsHLTV)
            return;
        
        if (player.UserId.HasValue && CS2_SimpleAdmin.PlayersInfo.TryGetValue(player.UserId.Value, out var value))
            value.WaitingForKick = true;

        player.CommitSuicide(true, true);
        
        if (delay > 0)
        {
            CS2_SimpleAdmin.Instance.AddTimer(delay, () =>
            {
                if (!player.IsValid || player.IsHLTV)
                    return;
                
                // Server.ExecuteCommand($"kickid {player.UserId}");

                player.Disconnect(reason);
            });
        }
        else
        {
            // Server.ExecuteCommand($"kickid {player.UserId}");

            player.Disconnect(reason); 
        }
        
        if (CS2_SimpleAdmin.UnlockedCommands && reason == NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED)
            Server.ExecuteCommand($"banid 1 {new SteamID(player.SteamID).SteamId3}");

        // if (!string.IsNullOrEmpty(reason))
        // {
        // 	var escapeChars = reason.IndexOfAny([';', '|']);
        //
        // 	if (escapeChars != -1)
        // 	{
        // 		reason = reason[..escapeChars];
        // 	}
        // }
        //
        // Server.ExecuteCommand($"kickid {userId} {reason}");
    }
    
    public static void KickPlayer(CCSPlayerController player, NetworkDisconnectionReason reason = NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED, int delay = 0)
    {
        if (!player.IsValid || player.IsHLTV)
            return;

        if (player.UserId.HasValue && CS2_SimpleAdmin.PlayersInfo.TryGetValue(player.UserId.Value, out var value))
            value.WaitingForKick = true;
        
        player.CommitSuicide(true, true);
        
        if (delay > 0)
        {
            CS2_SimpleAdmin.Instance.AddTimer(delay, () =>
            {
                if (!player.IsValid || player.IsHLTV)
                    return;
                
                // if (!string.IsNullOrEmpty(reason))
                // {
                // 	var escapeChars = reason.IndexOfAny([';', '|']);
                //
                // 	if (escapeChars != -1)
                // 	{
                // 		reason = reason[..escapeChars];
                // 	}
                // }
                //
                // Server.ExecuteCommand($"kickid {player.UserId}");
                player.Disconnect(reason);
            });
        }
        else
        {
            // Server.ExecuteCommand($"kickid {player.UserId}");

            player.Disconnect(reason);
        }
        
        if (CS2_SimpleAdmin.UnlockedCommands && reason == NetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED)
            Server.ExecuteCommand($"banid 1 {new SteamID(player.SteamID).SteamId3}");

        // if (!string.IsNullOrEmpty(reason))
        // {
        // 	var escapeChars = reason.IndexOfAny([';', '|']);
        //
        // 	if (escapeChars != -1)
        // 	{
        // 		reason = reason[..escapeChars];
        // 	}
        // }
        //
        // Server.ExecuteCommand($"kickid {userId} {reason}");
    }

    public static int ParsePenaltyTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time) || !time.Any(char.IsDigit))
        {
            // CS2_SimpleAdmin._logger?.LogError("Time string cannot be null or empty.");
            return -1;
        }

        if (time.Equals($"0"))
            return 0;

        var timeUnits = new Dictionary<string, int>
        {
            { "m", 1 },            // Minute
            { "h", 60 },           // Hour
            { "d", 1440 },         // Day (24 * 60)
            { "w", 10080 },        // Week (7 * 24 * 60)
            { "mo", 43200 },        // Month (30 * 24 * 60)
            { "y", 525600 }         // Year (365 * 24 * 60)
        };

        
        // Check if the input is purely numeric (e.g., "10" for 10 minutes)
        if (int.TryParse(time, out var numericMinutes))
        {
            return numericMinutes;
        }
        
        int totalMinutes = 0;
        
        var regex = new Regex(@"(\d+)([a-z]+)");
        var matches = regex.Matches(time);

        foreach (Match match in matches)
        {
            var value = int.Parse(match.Groups[1].Value); // Numeric part
            var unit = match.Groups[2].Value;            // Unit part

            if (timeUnits.TryGetValue(unit, out var minutesPerUnit))
            {
                totalMinutes += value * minutesPerUnit;
            }
            else
            {
                throw new ArgumentException($"Invalid time unit '{unit}' in time string.", nameof(time));
            }
        }
        
        return totalMinutes > 0 ? totalMinutes : -1;
    }

    public static void PrintToCenterAll(string message)
    {
        Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).ToList().ForEach(controller =>
        {
            controller.PrintToCenter(message);
        });
    }
    
    internal static void HandleVotes(CCSPlayerController player, ItemOption option)
    {
        if (!CS2_SimpleAdmin.VoteInProgress)
            return;

        option.DisableOption = DisableOption.DisableHideNumber;
        CS2_SimpleAdmin.VoteAnswers[option.Text]++;
    }
    
    internal static void LogCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (CS2_SimpleAdmin._localizer == null)
            return;

        var playerName = caller?.PlayerName ?? CS2_SimpleAdmin._localizer["sa_console"];

        var hostname = ConVar.Find("hostname")?.StringValue ?? CS2_SimpleAdmin._localizer["sa_unknown"];

        CS2_SimpleAdmin.Instance.Logger.LogInformation($"{CS2_SimpleAdmin._localizer[
            "sa_discord_log_command",
            playerName, command.GetCommandString]}".Replace("HOSTNAME", hostname).Replace("**", ""));
        
        SendDiscordLogMessage(caller, command, CS2_SimpleAdmin._localizer);
    }

    internal static void LogCommand(CCSPlayerController? caller, string command)
    {
        if (CS2_SimpleAdmin._localizer == null)
            return;

        var playerName = caller?.PlayerName ?? CS2_SimpleAdmin._localizer["sa_console"];
        var hostnameCvar = ConVar.Find("hostname");

        var hostname = hostnameCvar?.StringValue ?? CS2_SimpleAdmin._localizer["sa_unknown"];

        CS2_SimpleAdmin.Instance.Logger.LogInformation($"{CS2_SimpleAdmin._localizer["sa_discord_log_command",
            playerName, command]}".Replace("HOSTNAME", hostname).Replace("**", ""));

        SendDiscordLogMessage(caller, command, CS2_SimpleAdmin._localizer);
    }

    /*public static IEnumerable<Embed> GenerateEmbedsDiscord(string title, string description, string thumbnailUrl, Color color, string[] fieldNames, string[] fieldValues, bool[] inlineFlags)
	{
		var hostname = ConVar.Find("hostname")?.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
		var address = $"{ConVar.Find("ip")?.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

		description = description.Replace("{hostname}", hostname);
		description = description.Replace("{address}", address);

		var embed = new EmbedBuilder
		{
			Title = title,
			Description = description,
			ThumbnailUrl = thumbnailUrl,
			Color = color,
		};

		for (var i = 0; i < fieldNames.Length; i++)
		{
			fieldValues[i] = fieldValues[i].Replace("{hostname}", hostname ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");
			fieldValues[i] = fieldValues[i].Replace("{address}", address ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown");

			embed.AddField(fieldNames[i], fieldValues[i], inlineFlags[i]);

			if ((i + 1) % 2 == 0 && i < fieldNames.Length - 1)
			{
				embed.AddField("\u200b", "\u200b");
			}
		}

		return new List<Embed> { embed.Build() };
	}*/

    private static void SendDiscordLogMessage(CCSPlayerController? caller, CommandInfo command, IStringLocalizer? localizer)
    {
        if (CS2_SimpleAdmin.DiscordWebhookClientLog == null || localizer == null) return;

        var communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl() + ">" : "<https://steamcommunity.com/profiles/0>";
        var callerName = caller != null ? caller.PlayerName : CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console";
        _ = CS2_SimpleAdmin.DiscordWebhookClientLog.SendMessageAsync(Helper.GenerateMessageDiscord(localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command.GetCommandString]));
    }

    private static void SendDiscordLogMessage(CCSPlayerController? caller, string command, IStringLocalizer? localizer)
    {
        if (CS2_SimpleAdmin.DiscordWebhookClientLog == null || localizer == null) return;

        var communityUrl = caller != null ? "<" + new SteamID(caller.SteamID).ToCommunityUrl() + ">" : "<https://steamcommunity.com/profiles/0>";
        var callerName = caller != null ? caller.PlayerName : CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console";
        _ = CS2_SimpleAdmin.DiscordWebhookClientLog.SendMessageAsync(GenerateMessageDiscord(localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})", command]));
    }

    public static void ShowAdminActivity(string messageKey, string? callerName = null, bool dontPublish = false, params object[] messageArgs)
    {
        string[] publishActions = ["ban", "gag", "silence", "mute"];
        
        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.ShowActivityType == 0) return;
        if (CS2_SimpleAdmin._localizer == null) return;
        
        if (string.IsNullOrWhiteSpace(callerName))
            callerName = CS2_SimpleAdmin._localizer["sa_console"];

        var formattedMessageArgs = messageArgs.Select(arg => arg.ToString() ?? string.Empty).ToArray();
        
        if (dontPublish == false && publishActions.Any(messageKey.Contains))
        {
            CS2_SimpleAdmin.SimpleAdminApi?.OnAdminShowActivityEvent(messageKey, callerName, dontPublish, messageArgs);
        }
        
        // // Replace placeholder based on showActivityType
        // for (var i = 0; i < formattedMessageArgs.Length; i++)
        // {
        // 	var arg = formattedMessageArgs[i]; // Convert argument to string if not null
        // 	// Replace "CALLER" placeholder in the argument string
        // 	formattedMessageArgs[i] = CS2_SimpleAdmin.Instance.Config.OtherSettings.ShowActivityType switch
        // 	{
        // 		1 => arg.Replace("CALLER", CS2_SimpleAdmin._localizer["sa_admin"]),
        // 		2 => arg.Replace("CALLER", callerName ?? "Console"),
        // 		_ => arg
        // 	};
        // }
        var validPlayers = GetValidPlayers().Where(c => c is { IsValid: true, IsBot: false });

        if (!validPlayers.Any())
            return;

        if (CS2_SimpleAdmin.Instance.Config.OtherSettings.ShowActivityType == 3)
        {
            validPlayers = validPlayers.Where(c =>
                AdminManager.PlayerHasPermissions(new SteamID(c.SteamID), "@css/kick") ||
                AdminManager.PlayerHasPermissions(new SteamID(c.SteamID), "@css/ban"));
        }
        
        foreach (var controller in validPlayers.ToList())
        {
            var currentMessageArgs = (string[])formattedMessageArgs.Clone();

            // Replace "CALLER" placeholder based on showActivityType and whether the recipient is an admin
            for (var i = 0; i < currentMessageArgs.Length; i++)
            {
                var arg = currentMessageArgs[i];
                currentMessageArgs[i] = CS2_SimpleAdmin.Instance.Config.OtherSettings.ShowActivityType switch
                {
                    1 => arg.Replace("CALLER", AdminManager.PlayerHasPermissions(new SteamID(controller.SteamID), "@css/kick") || AdminManager.PlayerHasPermissions(new SteamID(controller.SteamID), "@css/ban") ? callerName : CS2_SimpleAdmin._localizer["sa_admin"]),
                    _ => arg.Replace("CALLER", callerName ?? CS2_SimpleAdmin._localizer["sa_console"]),
                };
            }

            // Send the localized message to each player
            controller.SendLocalizedMessage(CS2_SimpleAdmin._localizer, messageKey, currentMessageArgs.Cast<object>().ToArray());
        }
    }

    public static void DisplayCenterMessage(
        CCSPlayerController player,
        string messageKey,
        string? callerName = null,
        params object[] messageArgs)
    {
        if (CS2_SimpleAdmin._localizer == null) return;

        // Determine the localized message key
        var localizedMessageKey = $"{messageKey}";

        var formattedMessageArgs = messageArgs.Select(arg => arg?.ToString() ?? string.Empty).ToArray();

        // Replace placeholder based on showActivityType
        for (var i = 0; i < formattedMessageArgs.Length; i++)
        {
            var arg = formattedMessageArgs[i]; // Convert argument to string if not null
                                               // Replace "CALLER" placeholder in the argument string
            formattedMessageArgs[i] = CS2_SimpleAdmin.Instance.Config.OtherSettings.ShowActivityType switch
            {
                1 => arg.Replace("CALLER", CS2_SimpleAdmin._localizer["sa_admin"]),
                _ => arg
            };
        }

        // Print the localized message to the center of the screen for the player
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            player.PrintToCenter(CS2_SimpleAdmin._localizer[localizedMessageKey, formattedMessageArgs.Cast<object>().ToArray()]);
        }
    }

    private static string ConvertMinutesToTime(int minutes)
    {
        var time = TimeSpan.FromMinutes(minutes);

        return time.Days > 0 ? $"{time.Days}d {time.Hours}h {time.Minutes}m" : time.Hours > 0 ? $"{time.Hours}h {time.Minutes}m" : $"{time.Minutes}m";
    }

    public static void SendDiscordPenaltyMessage(CCSPlayerController? caller, CCSPlayerController? target, string reason, int duration, PenaltyType penalty, IStringLocalizer? localizer)
    {
        if (localizer == null) return;

        var penaltySetting = penalty switch
        {
            PenaltyType.Ban => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyBanSettings,
            PenaltyType.Mute => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyMuteSettings,
            PenaltyType.Gag => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyGagSettings,
            PenaltyType.Silence => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltySilenceSettings,
            PenaltyType.Warn => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyWarnSettings,
            _ => throw new ArgumentOutOfRangeException(nameof(penalty), penalty, null)
        };

        var webhookUrl = penaltySetting.FirstOrDefault(s => s.Name.Equals("Webhook"))?.Value;

        if (string.IsNullOrEmpty(webhookUrl)) return;

        const string defaultCommunityUrl = "<https://steamcommunity.com/profiles/0>";
        var callerCommunityUrl = caller != null ? $"<{new SteamID(caller.SteamID).ToCommunityUrl()}>" : defaultCommunityUrl;
        var targetCommunityUrl = target != null ? $"<{new SteamID(target.SteamID).ToCommunityUrl()}>" : defaultCommunityUrl;

        var callerName = caller?.PlayerName ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console";
        var targetName = target?.PlayerName ?? localizer["sa_unknown"];
        var targetSteamId = target != null ? new SteamID(target.SteamID).SteamId64.ToString() : localizer["sa_unknown"];

        var futureTime = Time.ActualDateTime().AddMinutes(duration);
        var futureUnixTimestamp = new DateTimeOffset(futureTime).ToUnixTimeSeconds();

        string time;

        if (penaltySetting.FirstOrDefault(s => s.Name.Equals("Time"))?.Value == "{relative}")
            time = duration != 0 ? $"<t:{futureUnixTimestamp}:R>" : localizer["sa_permanent"];
        else
            time = duration != 0 ? ConvertMinutesToTime(duration) : localizer["sa_permanent"];
            
        string[] fieldNames = [
            localizer["sa_player"],
            localizer["sa_steamid"],
            localizer["sa_duration"],
            localizer["sa_reason"],
            localizer["sa_admin"]];
        string[] fieldValues =
        [
            $"[{targetName}]({targetCommunityUrl})", $"||{targetSteamId}||", time, reason,
            $"[{callerName}]({callerCommunityUrl})"
        ];
        
        bool[] inlineFlags = [true, true, true, false, false];
        var hostname = ConVar.Find("hostname")?.StringValue ?? localizer["sa_unknown"];
        var colorHex = penaltySetting.FirstOrDefault(s => s.Name.Equals("Color"))?.Value ?? "#FFFFFF";

        if (string.IsNullOrEmpty(colorHex))
            colorHex = "#FFFFFF";

        var embed = new Embed
        {
            Color = DiscordManager.ColorFromHex(colorHex),
            Title = penalty switch
            {
                PenaltyType.Ban => localizer["sa_discord_penalty_ban"],
                PenaltyType.Mute => localizer["sa_discord_penalty_mute"],
                PenaltyType.Gag => localizer["sa_discord_penalty_gag"],
                PenaltyType.Silence => localizer["sa_discord_penalty_silence"],
                PenaltyType.Warn => localizer["sa_discord_penalty_warn"],
                _ => throw new ArgumentOutOfRangeException(nameof(penalty), penalty, null)
            },
            Description = $"{hostname}",
            ThumbnailUrl = penaltySetting.FirstOrDefault(s => s.Name.Equals("ThumbnailUrl"))?.Value,
            ImageUrl = penaltySetting.FirstOrDefault(s => s.Name.Equals("ImageUrl"))?.Value,
            Footer = new Footer
            {
                Text = penaltySetting.FirstOrDefault(s => s.Name.Equals("Footer"))?.Value
            },
            
            Timestamp = Time.ActualDateTime().ToUniversalTime().ToString("o"),
        };

        for (var i = 0; i < fieldNames.Length; i++)
        {
            embed.AddField(fieldNames[i], fieldValues[i], inlineFlags[i]);
        }

        Task.Run(async () =>
        {
            try
            {
                await new DiscordManager(webhookUrl).SendEmbedAsync(embed);
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                CS2_SimpleAdmin._logger?.LogError("Unable to send discord webhook: {exception}", ex.Message);
            }
        });
    }
    
    public static void SendDiscordPenaltyMessage(CCSPlayerController? caller, string steamId, string reason, int duration, PenaltyType penalty, IStringLocalizer? localizer)
    {
        if (localizer == null) return;

        var penaltySetting = penalty switch
        {
            PenaltyType.Ban => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyBanSettings,
            PenaltyType.Mute => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyMuteSettings,
            PenaltyType.Gag => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyGagSettings,
            PenaltyType.Silence => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltySilenceSettings,
            PenaltyType.Warn => CS2_SimpleAdmin.Instance.Config.Discord.DiscordPenaltyWarnSettings,
            _ => throw new ArgumentOutOfRangeException(nameof(penalty), penalty, null)
        };

        var webhookUrl = penaltySetting.FirstOrDefault(s => s.Name.Equals("Webhook"))?.Value;

        if (string.IsNullOrEmpty(webhookUrl)) return;
        const string defaultCommunityUrl = "<https://steamcommunity.com/profiles/0>";
        var callerCommunityUrl = caller != null ? $"<{new SteamID(caller.SteamID).ToCommunityUrl()}>" : defaultCommunityUrl;
        var targetCommunityUrl = $"<{new SteamID(ulong.Parse(steamId)).ToCommunityUrl()}>";

        var callerName = caller?.PlayerName ?? CS2_SimpleAdmin._localizer?["sa_console"] ?? "Console";
        var targetName = steamId;
        var targetSteamId = steamId;

        var futureTime = Time.ActualDateTime().AddMinutes(duration);
        var futureUnixTimestamp = new DateTimeOffset(futureTime).ToUnixTimeSeconds();

        string time;

        if (penaltySetting.FirstOrDefault(s => s.Name.Equals("Time"))?.Value == "{relative}")
            time = duration != 0 ? $"<t:{futureUnixTimestamp}:R>" : localizer["sa_permanent"];
        else
            time = duration != 0 ? ConvertMinutesToTime(duration) : localizer["sa_permanent"];
            
        string[] fieldNames = [
            localizer["sa_player"],
            localizer["sa_steamid"],
            localizer["sa_duration"],
            localizer["sa_reason"],
            localizer["sa_admin"]];
        string[] fieldValues =
        [
            $"[{targetName}]({targetCommunityUrl})", $"||{targetSteamId}||", time, reason,
            $"[{callerName}]({callerCommunityUrl})"
        ];
        
        bool[] inlineFlags = [true, true, true, false, false];
        var hostname = ConVar.Find("hostname")?.StringValue ?? localizer["sa_unknown"];
        var colorHex = penaltySetting.FirstOrDefault(s => s.Name.Equals("Color"))?.Value ?? "#FFFFFF";

        var embed = new Embed
        {
            Color = DiscordManager.ColorFromHex(colorHex),
            Title = penalty switch
            {
                PenaltyType.Ban => localizer["sa_discord_penalty_ban"],
                PenaltyType.Mute => localizer["sa_discord_penalty_mute"],
                PenaltyType.Gag => localizer["sa_discord_penalty_gag"],
                PenaltyType.Silence => localizer["sa_discord_penalty_silence"],
                PenaltyType.Warn => localizer["sa_discord_penalty_warn"],
                _ => throw new ArgumentOutOfRangeException(nameof(penalty), penalty, null)
            },
            Description = $"{hostname}",
            ThumbnailUrl = penaltySetting.FirstOrDefault(s => s.Name.Equals("ThumbnailUrl"))?.Value,
            ImageUrl = penaltySetting.FirstOrDefault(s => s.Name.Equals("ImageUrl"))?.Value,
            Footer = new Footer
            {
                Text = penaltySetting.FirstOrDefault(s => s.Name.Equals("Footer"))?.Value
            },
            
            Timestamp = Time.ActualDateTime().ToUniversalTime().ToString("o"),
        };

        for (var i = 0; i < fieldNames.Length; i++)
        {
            embed.AddField(fieldNames[i], fieldValues[i], inlineFlags[i]);
        }

        Task.Run(async () =>
        {
            try
            {
                await new DiscordManager(webhookUrl).SendEmbedAsync(embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }
    
    private static string GenerateMessageDiscord(string message)
    {
        var hostname = ConVar.Find("hostname")?.StringValue ?? CS2_SimpleAdmin._localizer?["sa_unknown"] ?? "Unknown";
        var address = $"{ConVar.Find("ip")?.StringValue}:{ConVar.Find("hostport")!.GetPrimitiveValue<int>()}";

        message = message.Replace("HOSTNAME", hostname);
        message = message.Replace("ADDRESS", address);

        return message;
    }

    public static string[] SeparateLines(string message)
    {
        return message.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    public static string CenterMessage(string message) =>
        $"⠀⠀⠀⠀⠀⠀⠀⠀{message}⠀⠀⠀⠀⠀⠀⠀⠀";

    public static string GetServerIp()
    {
        var networkSystem = NativeAPI.GetValveInterface(0, "NetworkSystemVersion001");

        unsafe
        {
            if (_networkSystemUpdatePublicIp == null)
            {
                var funcPtr = *(nint*)(*(nint*)(networkSystem) + 256);
                _networkSystemUpdatePublicIp = Marshal.GetDelegateForFunctionPointer<CNetworkSystemUpdatePublicIp>(funcPtr);
            }
            /*
			struct netadr_t
			{
			   uint32_t type
			   uint8_t ip[4]
			   uint16_t port
			}
			*/
            // + 4 to skip type, because the size of uint32_t is 4 bytes
            var ipBytes = (byte*)(_networkSystemUpdatePublicIp(networkSystem) + 4);
            // port is always 0, use the one from convar "hostport"
            return $"{ipBytes[0]}.{ipBytes[1]}.{ipBytes[2]}.{ipBytes[3]}";
        }
    }

    public static void UpdateConfig<T>(T config) where T : BasePluginConfig, new()
    {
        // get newest config version
        var newCfgVersion = new T().Version;

        // loaded config is up to date
        if (config.Version == newCfgVersion)
            return;

        // update the version
        config.Version = newCfgVersion;

        // serialize the updated config back to json
        var updatedJsonContent = JsonSerializer.Serialize(config,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        File.WriteAllText(CfgPath, updatedJsonContent);
    }

    public static void TryLogCommandOnDiscord(CCSPlayerController? caller, string commandString)
    {
        if (CS2_SimpleAdmin.DiscordWebhookClientLog == null || CS2_SimpleAdmin._localizer == null)
            return;

        if (caller != null && caller.IsValid == false)
            caller = null;

        var callerName = caller == null ? CS2_SimpleAdmin._localizer["sa_console"] : caller.PlayerName;
        var communityUrl = caller != null
            ? "<" + new SteamID(caller.SteamID).ToCommunityUrl() + ">"
            : "<https://steamcommunity.com/profiles/0>";
        _ = CS2_SimpleAdmin.DiscordWebhookClientLog.SendMessageAsync(GenerateMessageDiscord(
            CS2_SimpleAdmin._localizer["sa_discord_log_command", $"[{callerName}]({communityUrl})",
                commandString]));
    }

    public static BaseMenu CreateMenu(string title)
    {
        return MenuManager.MenuByType(CS2_SimpleAdmin.Instance.Config.MenuConfigs.MenuType, title, Instance);
    }
   // public static IMenu? CreateMenu(string title, Action<CCSPlayerController>? backAction = null)
   // {
        /*
        var menuType = CS2_SimpleAdmin.Instance.Config.MenuConfigs.MenuType.ToLower();

        var menu = menuType switch
        {
            _ when menuType.Equals("selectable", StringComparison.CurrentCultureIgnoreCase) =>
                CS2_SimpleAdmin.MenuApi?.GetMenu(title),

            _ when menuType.Equals("dynamic", StringComparison.CurrentCultureIgnoreCase) =>
                CS2_SimpleAdmin.MenuApi?.GetMenuForcetype(title, MenuType.ButtonMenu),

            _ when menuType.Equals("center", StringComparison.CurrentCultureIgnoreCase) =>
                CS2_SimpleAdmin.MenuApi?.GetMenuForcetype(title, MenuType.CenterMenu),

            _ when menuType.Equals("chat", StringComparison.CurrentCultureIgnoreCase) =>
                CS2_SimpleAdmin.MenuApi?.GetMenuForcetype(title, MenuType.ChatMenu),

            _ when menuType.Equals("console", StringComparison.CurrentCultureIgnoreCase) =>
                CS2_SimpleAdmin.MenuApi?.GetMenuForcetype(title, MenuType.ConsoleMenu),

            _ => CS2_SimpleAdmin.MenuApi?.GetMenu(title)
        };

        return menu;
        */
    //    return new ScreenMenu(title, CS2_SimpleAdmin.Instance);
    //}
    
    internal static IPluginManager? GetPluginManager()
    {
        // Access the singleton instance of Application
        var applicationInstance = Application.Instance;

        // Use Reflection to access the private _pluginManager field
        var pluginManagerField = typeof(Application).GetField("_pluginManager", BindingFlags.NonPublic | BindingFlags.Instance);
        var pluginManager = pluginManagerField?.GetValue(applicationInstance) as IPluginManager;
        
        return pluginManager;
    }
    
}

public class SchemaString<TSchemaClass>(TSchemaClass instance, string member)
    : NativeObject(Schema.GetSchemaValue<nint>(instance.Handle, typeof(TSchemaClass).Name, member))
    where TSchemaClass : NativeObject
{
    public unsafe void Set(string str)
    {
        var bytes = GetStringBytes(str);

        for (var i = 0; i < bytes.Length; i++)
        {
            Unsafe.Write((void*)(Handle.ToInt64() + i), bytes[i]);
        }

        Unsafe.Write((void*)(Handle.ToInt64() + bytes.Length), 0);
    }

    private static byte[] GetStringBytes(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }
}

public static class Time
{
    public static DateTime ActualDateTime()
    {
        string timezoneId = CS2_SimpleAdmin.Instance.Config.Timezone;
        DateTime utcNow = DateTime.UtcNow;

        try
        {
            TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            DateTime userTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timezone);
            return userTime;
        }
        catch (TimeZoneNotFoundException)
        {
            CS2_SimpleAdmin._logger?.LogWarning($"Time zone '{timezoneId}' not found. Returning UTC time.");
            return utcNow;
        }
        catch (InvalidTimeZoneException)
        {
            CS2_SimpleAdmin._logger?.LogWarning($"Time zone '{timezoneId}' is invalid. Returning UTC time.");
            return utcNow;
        }
    }
}

public static class WeaponHelper
{
    private static readonly Lazy<Dictionary<string, CsItem>> WeaponsEnumCache = new(BuildEnumMemberMap);

    private static Dictionary<string, CsItem> BuildEnumMemberMap()
    {
        var dictionary = new Dictionary<string, CsItem>();

        foreach (var field in typeof(CsItem).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
            if (attribute?.Value == null) continue;
            if (field.GetValue(null) is not CsItem csItem)
                continue;
            var enumValue = field.GetValue(null);
            
            dictionary.TryAdd(attribute.Value, csItem);
        }

        return dictionary;
    }
    
    public static CsItem? GetEnumFromWeaponName(string weaponName)
    {
        if (WeaponsEnumCache.Value.TryGetValue(weaponName, out var csItem))
        {
            return csItem;
        }

        return null;
    }
    
    public static List<(string EnumMemberValue, CsItem EnumValue)> GetWeaponsByPartialName(string input)
    {
        // Normalize input for case-insensitive comparison
        var normalizedInput = input.ToLowerInvariant();

        // Find all matching weapons based on the input
        var matchingWeapons = WeaponsEnumCache.Value
            .Where(kvp => kvp.Key.Contains(normalizedInput, StringComparison.InvariantCultureIgnoreCase))
            .Select(kvp => (EnumMemberValue: kvp.Key, EnumValue: kvp.Value))
            .ToList();

        // Check for an exact match first
        var exactMatch = matchingWeapons
            .FirstOrDefault(m => m.EnumMemberValue.Equals(input, StringComparison.OrdinalIgnoreCase));

        if (exactMatch.EnumMemberValue != null)
        {
            // Return a list containing only the exact match
            return [exactMatch];
        }

        // If no exact match, get all matches that start with the input
        var filteredWeapons = matchingWeapons
            .Where(m => m.EnumMemberValue.StartsWith(normalizedInput, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        return filteredWeapons; // Return all relevant matches for the partial input
    }
}

public static class IpHelper
{
    public static uint IpToUint(string ipAddress)
    {
        return (uint)BitConverter.ToInt32(System.Net.IPAddress.Parse(ipAddress).GetAddressBytes().Reverse().ToArray(),
            0);
    }
    
    public static bool TryConvertIpToUint(string ipString, out uint ipUint)
    {
        ipUint = 0;
        if (string.IsNullOrWhiteSpace(ipString))
            return false;

        if (!System.Net.IPAddress.TryParse(ipString, out var ipAddress))
            return false;

        var bytes = ipAddress.GetAddressBytes();
        if (bytes.Length != 4)
            return false;

        ipUint = IpToUint(ipString);
        return true;
    }

    public static string UintToIp(uint ipAddress)
    {
        var bytes = BitConverter.GetBytes(ipAddress).Reverse().ToArray();
        return new System.Net.IPAddress(bytes).ToString();
    }
}