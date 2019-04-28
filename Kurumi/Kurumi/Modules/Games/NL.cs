using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Random;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Games
{
    public class NL : ModuleBase
    {
        [Command("choose")]
        public async Task Choose([Optional, Remainder]string TheString)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (TheString == null || !TheString.Contains(";"))
                {
                    await Context.Channel.SendEmbedAsync(lang["choose_empty"]);
                    return;
                }
                string[] Options = TheString.Split(";");
                int Index = new KurumiRandom().Next(0, Options.Length);
                await Context.Channel.SendEmbedAsync(lang["choose_chosen", "OPTION", Options[Index]]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Choose", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Choose", null, ex), Context);
            }
        }

        [Command("coinflip")]
        [Alias("flip")]
        public async Task Flip()
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (new KurumiRandom().Next(0, 2) == 1)
                {
                    await Context.Channel.SendEmbedAsync(lang["coin_head"]);
                }
                else
                {
                    await Context.Channel.SendEmbedAsync(lang["coin_tails"]);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Coinflip", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Coinflip", null, ex), Context);
            }
        }

        [Command("roll")]
        public async Task Roll([Optional, Remainder]string number)
        {
            try
            {
                if (!int.TryParse(number, out int Num) || Num == 0)
                    Num = 10;
                var lang = Language.GetLanguage(Context.Guild);
                await Context.Channel.SendEmbedAsync(lang["roll", "NUMBER", new KurumiRandom().Next(1, Num + 1).ToString(), "USER", Context.User.Username]);
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Roll", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Roll", null, ex), Context);
            }
        }

        [Command("russianroulette")]
        [Alias("rr")]
        public async Task RRoulette([Optional]string bet)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                if (bet == null)
                {
                    await Context.Channel.SendEmbedAsync(lang["rr_min_bet"]);
                    return;
                }
                if (!uint.TryParse(bet, out uint temp))
                {
                    await Context.Channel.SendEmbedAsync(lang["rr_invalid_bet"]);
                    return;
                }
                if (temp > 150)
                {
                    await Context.Channel.SendEmbedAsync(lang["rr_max_bet"]);
                    return;
                }
                if (temp < 1)
                {
                    await Context.Channel.SendEmbedAsync(lang["rr_min_bet"]);
                    return;
                }
                if (((temp * 12) / 2) > UserDatabase.GetOrCreate(Context.User.Id).Credit)
                {
                    await Context.Channel.SendEmbedAsync(lang["rr_no_money"]);
                    return;
                }
                KurumiRandom rand = new KurumiRandom();
                var embed = new EmbedBuilder().WithColor(Config.EmbedColor);
                embed.WithDescription(lang["rr_start", "BET", bet]);

                IUserMessage msg = await ReplyAsync("", embed: embed.Build());
                await Task.Delay(1000);
                if (rand.Next(1, 7) != 1)
                {
                    embed.WithDescription(lang["rr_win", "BET", bet, "MONEY", Math.Round(float.Parse(bet) * 1.5f).ToString()]);
                    await msg.ModifyAsync(x => x.Embed = embed.Build());
                    UserDatabase.Get(Context.User.Id).Credit += (temp * 2);
                }
                else
                {
                    embed.WithDescription(lang["rr_lose", "BET", bet, "MONEY", (temp * 12).ToString()]);
                    await msg.ModifyAsync(x => x.Embed = embed.Build());
                    UserDatabase.Get(Context.User.Id).Credit -= (temp * 12);
                }

                await Utilities.Log(new LogMessage(LogSeverity.Info, "RussianRoulette", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "RussianRoulette", null, ex), Context);
            }
        }

        [Command("Slot")]
        public async Task Slot([Remainder, Optional]string bet)
        {
            try
            {
                LanguageDictionary lang = Language.GetLanguage(Context.Guild);

                //Get bet in int
                if (!uint.TryParse(bet, out uint Bet))
                {
                    await Context.Channel.SendEmbedAsync(lang["slot_invalid_bet"]);
                    return;
                }
                //Check Money
                if (UserDatabase.GetOrCreate(Context.User.Id).Credit < Bet)
                {
                    await Context.Channel.SendEmbedAsync(lang["slot_not_enough"]);
                    return;
                }
                //Max bet
                if (Bet > 500)
                {
                    await Context.Channel.SendEmbedAsync(lang["slot_max_bet"]);
                    return;
                }
                if (Bet == 0)
                {
                    await Context.Channel.SendEmbedAsync(lang["slot_min_bet"]);
                    return;
                }
                //Send message
                var Message = await Context.Channel.SendEmbedAsync(lang["slot_rolling"]);

                //Get rng
                KurumiRandom rng = new KurumiRandom();
                int a = rng.Next(1, 101);
                int b = rng.Next(1, 101);
                int c = rng.Next(1, 101);

                int First = 0;
                int Second = 0;
                int Third = 0;

                for (int i = 0; i < ChanceTable.Count; i++)
                {
                    var (Start, End, Res) = ChanceTable[i];
                    if (Start <= a && End >= a)
                        First = Res;
                    if (Start <= b && End >= b)
                        Second = Res;
                    if (Start <= c && End >= c)
                        Third = Res;
                }

                //Get multiplier
                float multiplier = 0F;
                int count = 0;
                int Wining = -1;
                if (First == Third && Second == First)
                {
                    Wining = First;
                    count = 3;
                }
                else if (Second == First || Second == Third)
                {
                    Wining = Second;
                    count = 2;
                }
                else if (First == Third)
                {
                    count = 2;
                    Wining = Third;
                }

                if (count != 0)
                {
                    for (int i = 0; i < WinTable.Count; i++)
                    {
                        var (rngRes, Count, Multiplier) = WinTable[i];
                        if (Count == count && rngRes == Wining)
                        {
                            multiplier = Multiplier;
                            break;
                        }
                    }
                }

                //Send message
                uint Reward = count == 0 ? Bet : (uint)(Bet * multiplier);
                await Context.Channel.SendEmbedAsync("-===SLOT===-\n" +
                    $"| {Table[rng.Next(0, Table.Count)]} {Table[rng.Next(0, Table.Count)]} {Table[rng.Next(0, Table.Count)]} |\n" +
                    $">{Table[First]} {Table[Second]} {Table[Third]}<\n" +
                    $"| {Table[rng.Next(0, Table.Count)]} {Table[rng.Next(0, Table.Count)]} {Table[rng.Next(0, Table.Count)]} |\n" +
                    $"{lang[$"{(count == 0 ? "slot_lost" : "slot_won")}", "BET", Reward]}");

                //Modify credit
                if (count == 0)
                    UserDatabase.Get(Context.User.Id).Credit -= Reward;
                else
                    UserDatabase.Get(Context.User.Id).Credit += Reward;

                await Utilities.Log(new LogMessage(LogSeverity.Info, "Slot", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Slot", null, ex), Context);
            }
        }

        private readonly static List<(byte rngRes, byte Count, float Multiplier)> WinTable = new List<(byte, byte, float)>()
        {
            (0, 2, 1.5F),
            (2, 2, 1.75F),
            (1, 2, 1.8F),
            (4, 2, 1.85F),
            (2, 3, 2F),
            (0, 3, 2F),
            (3, 2, 2F),
            (7, 2, 2F),
            (6, 2, 2F),
            (7, 3, 2.2F),
            (5, 2, 2.25F),
            (1, 3, 2.35F),
            (4, 3, 3.5F),
            (3, 3, 4F),
            (5, 3, 4.1F),
            (6, 3, 5F),
        };
        private readonly static List<(byte Start, byte End, byte Res)> ChanceTable = new List<(byte Start, byte End, byte Res)>()
        {
            (1, 20, 0),  //20%
            (21, 38, 1),  //18%
            (39, 54, 2),  //16%
            (55, 64, 3),  //10%
            (65, 75, 4),  //11%
            (76, 82, 5),  //7%
            (83, 87, 6),  //5%
            (88, 100, 7), //13%
        };
        private readonly static List<string> Table = new List<string>()
        {
            ":cherries:",
            ":moneybag:",
            ":lemon:",
            ":seven:",
            ":star:",
            ":gem:",
            ":100:",
            ":tangerine:"
        };
    }
}