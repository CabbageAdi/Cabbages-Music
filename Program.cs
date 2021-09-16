using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using System.Linq;
using System;
using SpotifyAPI.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.IO;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity.Extensions;
using System.Reflection;

namespace Cabbage_Music
{
    public class Program
    {
        public static Dictionary<ulong, List<LavalinkTrack>> Queues { get; set; }
        public static Dictionary<ulong, bool> Loop { get; set; }

        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public static async Task MainAsync(string[] args)
        {
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

            var discord = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = config.token,
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.AllUnprivileged
            });

            var rand = new Random();
            var spotifyConfig = SpotifyClientConfig
.CreateDefault()
.WithAuthenticator(new ClientCredentialsAuthenticator(config.spotify.client_id, config.spotify.client_secret));

            var spotify = new SpotifyClient(spotifyConfig);

            var services = new ServiceCollection().AddSingleton(rand).AddSingleton(spotify).BuildServiceProvider();

            var commands = await discord.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                EnableDms = false,
                StringPrefixes = new[] { "cabbages!", "cabbage!", "cab!", "c!", "cm" },
                EnableDefaultHelp = false,
                Services = services
            });

            await discord.UseInteractivityAsync(new()
            {
                AckPaginationButtons = true
            });

            var slash = await discord.UseSlashCommandsAsync(new SlashCommandsConfiguration { Services = services });
            slash.RegisterCommands<SlashCommands>();

            var lavalink = await discord.UseLavalinkAsync();

            var endpoint = new ConnectionEndpoint
            {
                Hostname = config.lavalink.hostname,
                Port = config.lavalink.port
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = config.lavalink.password,
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            Queues = new();
            Loop = new();

            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            foreach (var instance in commands.Values)
            {
                instance.CommandErrored += async (s, e) =>
                {
                    if (e.Exception.Message == "Specified command was not found.") return;
                    if (e.Exception is ChecksFailedException checkfail)
                    {
                        foreach (var thing in checkfail.FailedChecks)
                        {
                            if (thing is CooldownAttribute cooldown)
                            {
                                var embed = new DiscordEmbedBuilder
                                {
                                    Title = "You're on cooldown!",
                                    Description = $"The max cooldown is `{cooldown.Reset.TotalSeconds.ToString("0.0")}s`\nYour remaining cooldown is `{cooldown.GetRemainingCooldown(e.Context).TotalSeconds.ToString("0.0")}s`"
                                };
                                await e.Context.RespondAsync(embed: embed);
                            }
                        }
                    }
                    else
                    {
                        s.Client.Logger.LogError(e.Exception.ToString() + $" in `{e.Context.Guild.Name}` in channel `{e.Context.Channel.Name}` by `{e.Context.Member.Username + "#" + e.Context.Member.Discriminator}`");
                    }
                };
            }

            foreach (var instance in slash.Values)
            {
                instance.SlashCommandErrored += (s, e) =>
                {
                    s.Client.Logger.LogError(e.Exception.ToString() + $" in `{e.Context.Guild.Name}` in channel `{e.Context.Channel.Name}` by `{e.Context.Member.Username + "#" + e.Context.Member.Discriminator}`");
                    return Task.CompletedTask;
                };
            }

            discord.VoiceStateUpdated += (s, e) =>
            {
                _ = Task.Run(() =>
                {
                    if (e.User == discord.CurrentUser && e.After.Channel == null)
                    {
                        if (Queues.ContainsKey(e.Guild.Id))
                            Queues.Remove(e.Guild.Id);
                        if (Loop.ContainsKey(e.Guild.Id))
                            Loop.Remove(e.Guild.Id);
                    }
                });
                return Task.CompletedTask;
            };

            discord.GuildDownloadCompleted += (s, e) =>
            {
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        int count = 0;
                        foreach (var shard in discord.ShardClients.Values)
                        {
                            count += shard.Guilds.Count;
                        }
                        int commandcount = s.GetCommandsNext().RegisteredCommands.Where(idk => !idk.Value.IsHidden).Count();
                        await discord.UpdateStatusAsync(new DiscordActivity($"c!help | {count} servers", ActivityType.ListeningTo));
                        await Task.Delay(100000);
                    }
                });

                return Task.CompletedTask;
            };

            await discord.StartAsync();
            foreach(var instance in lavalink.Values)
            {
                await instance.ConnectAsync(lavalinkConfig);
            }
            await Task.Delay(-1);
        }
    }

    public class Config
    {
        public string token { get; set; }
        public SpotifyConfig spotify { get; set; }
        public LavalinkConfig lavalink { get; set; }
    }

    public class SpotifyConfig
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
    }

    public class LavalinkConfig
    {
        public string hostname { get; set; }
        public int port { get; set; }
        public string password { get; set; }
    }
}
