using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Database.Models;
using Kurumi.Services.Random;
using Kurumi.StartUp;

namespace Kurumi.Modules.Games.Duel
{
    public class DuelGame : IGame
    {
        public int MinPlayers { get; private set; }
        public int MaxPlayers { get; private set; }
        public Dictionary<string, object> Settings { get; private set; }
        public List<ulong> Surrender { get; private set; }
        public bool AI { get; private set; }

        public DuelGame()
        {
            MinPlayers = 2;
            MaxPlayers = 2;
            Settings = new Dictionary<string, object>();
            Surrender = new List<ulong>();
            AI = true;
        }
        public bool ValidPlayer(IUser user) => CharacterDatabase.GetCharacter(user.Id) != null;
        public override string ToString() => "lobby_duel";

        #region Game
        public string Selected = "";
        public ulong SelectingUser;
        private KurumiRandom rng;
        private LanguageDictionary lang;
        private ICommandContext Context;
        public Task Start(Lobby lobby, ICommandContext Context)
        {
            this.Context = Context;
            rng = new KurumiRandom();
            return Task.Run(async () => 
            {
                try
                {
                    lang = Language.GetLanguage(Context.Guild);
                    var Players = lobby.GetPlayers();
                    var State = new GameState(Players[0], Players[1]);
                    int Count = 0;
                    HitResult Result = null;
                    while (true)
                    {
                        Count++;
                        Item Selected;
                        if (State.CurrentCharacter.Ai)
                        {
                            Selected = Ai.SelectSkill(State.CurrentPlayerStats,
                                                      State.OtherPlayerStats,
                                                      State.CurrentCharacter,
                                                      State.OtherCharacter,
                                                      this);
                        }
                        else
                            Selected = await WaitForUserSelect(State.CurrentPlayer,
                                                               State.OtherPlayer,
                                                               State.CurrentCharacter);

                        if (Surrender.Contains(State.Player1.Id))
                        {
                            State.Player1Won = false;
                            //_Surrender = true;
                            break;
                        }
                        else if (Surrender.Contains(State.Player2.Id))
                        {
                            State.Player1Won = true;
                            //_Surrender = true;
                            break;
                        }

                        Result = await Hit(State.OtherPlayerStats, State.CurrentPlayerStats, Selected);


                        if (State.Player1Stats.Hp <= 0)
                        {
                            State.Player1Won = false;
                            break;
                        }
                        else if (State.Player2Stats.Hp <= 0)
                        {
                            State.Player1Won = true;
                            break;
                        }

                        EmbedBuilder Embed = new EmbedBuilder()
                            .WithColor(Config.EmbedColor)
                            .WithTitle(State.OtherCharacter.Name)
                            .AddField(lang["character_hp"], $":heart: {State.OtherPlayerStats.Hp}/{State.OtherPlayerStats.FullHp}")
                            .AddField(lang["character_damage_last"], $"{(Result.Crit ? ":zap:" : ":dagger:")} {Result.Damage * Result.Combo}")
                            .AddField(lang["character_damage_blocked"],
                             $":shield: {(State.OtherPlayerStats.Resistance + Result.ResistanceBonus) * Result.Combo}\n‏‏‎ ");
                        if (Result.Healed != 0)
                            Embed.AddField(State.CurrentCharacter.Name, 
                                            $"{lang["character_healed"]}\n:green_heart: {Result.Healed}\n{lang["character_new_hp"]}\n" +
                                            $":heart: {State.CurrentPlayerStats.Hp}/{State.CurrentPlayerStats.FullHp}");
                        await Context.Channel.SendEmbedAsync(Embed);

                        State.NextTurn();
                    }

                    var Player1Damages = State.Player1Character.DamageItems(State.Player1Stats.TotalData.Durability, State.Player2Stats.TotalData.Durability);
                    string Player1DamageString = null;
                    for (int i = 0; i < Player1Damages.Count; i++)
                        Player1DamageString += "\n" + Player1Damages[i].ToString(lang).Remove("**");

                    var Player2Damages = State.Player2Character.DamageItems(State.Player2Stats.TotalData.Durability, State.Player1Stats.TotalData.Durability);
                    string Player2DamageString = null;
                    for (int i = 0; i < Player2Damages.Count; i++)
                        Player2DamageString += "\n" + Player2Damages[i].ToString(lang).Remove("**");

                    await Context.Channel.SendEmbedAsync($"\n**[{State.WinnerCharacter.Name}]**\n" +
                                                         $"▸{lang["character_hp"]}: {State.WinnerStats.Hp}/{State.WinnerStats.FullHp}\n" +
                                                         $"▸{lang["character_total_damage"]}{State.WinnerStats.TotalData.DamageDealt}\n" +
                                                         $"▸{lang["character_total_blocked"]}{State.WinnerStats.TotalData.DamageBlocked}\n" +
                                                         $"▸{lang["character_highest_combo"]}{State.WinnerStats.TotalData.HighestCombo}\n" +
                                                         $"▸{lang["character_total_healed"]}{State.WinnerStats.TotalData.HpHealed}\n" +
                                                         $"{(Player1DamageString == null ? "" : $"▸{lang["character_damages"]}```{Player1DamageString}```\n")}" +
                                                         $"\n" +
                                                         $"\n" +
                                                         $"**[{State.LoserCharacter.Name}]**\n" +
                                                         $"▸{lang["character_hp"]}: {State.LoserStats.Hp}/{State.LoserStats.FullHp}\n" +
                                                         $"▸{lang["character_total_damage"]}{State.LoserStats.TotalData.DamageDealt}\n" +
                                                         $"▸{lang["character_total_blocked"]}{State.LoserStats.TotalData.DamageBlocked}\n" +
                                                         $"▸{lang["character_highest_combo"]}{State.LoserStats.TotalData.HighestCombo}\n" +
                                                         $"▸{lang["character_total_healed"]}{State.LoserStats.TotalData.HpHealed}\n" +
                                                         $"{(Player2DamageString == null ? "" : $"▸{lang["character_damages"]}```{Player2DamageString}```")}",
                        Title: lang["character_end", "WINNER", State.WinnerCharacter.Name, "LOSER", State.LoserCharacter.Name]);

                    await Utilities.Log(new LogMessage(LogSeverity.Info, "Duel", "success"), Context);
                }
                catch (Exception ex)
                {
                    await Utilities.Log(new LogMessage(LogSeverity.Error, "Duel", null, ex), Context);
                }
                return Task.CompletedTask;
            });
        }

        private async Task<Item> WaitForUserSelect(IUser player, IUser otherPlayer, Character character)
        {
            Item X = CharacterDatabase.GetItem(x => x.Id == character.X);
            Item Y = CharacterDatabase.GetItem(x => x.Id == character.Y);
            Item A = CharacterDatabase.GetItem(x => x.Id == character.A);
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Config.EmbedColor)
                .WithTitle(lang["character_select", "PLAYER", player.Username])
                .AddField("X", X.Name, true)
                .AddField("Y", Y.Name, true)
                .AddField("A", A.Name, true)
                .AddField("B", lang["character_no_skill"]);
            await Context.Channel.SendEmbedAsync(embed);

            int Timer = 300;
            SelectingUser = player.Id;
            Selected = null;
            while (Timer != 0 && Selected == null && !(Surrender.Contains(player.Id) || Surrender.Contains(otherPlayer.Id)))
            {
                await Task.Delay(100);
                Timer--;
            }

            if(!(Surrender.Contains(player.Id) || Surrender.Contains(otherPlayer.Id)) && Timer != 0 && Selected != null)
            {
                switch (Selected.ToLower())
                {
                    case "x":
                        return X;
                    case "a":
                        return A;
                    case "y":
                        return Y;
                }
            }
            Selected = "";
            return null;
        }

        public Task<HitResult> Hit(CharacterStats Target, CharacterStats Player, Item Skill, bool CritEnabled = true)
        {
            var Result = new HitResult();
            int Combo = Skill?.Combo ?? 1;
            Result.Combo = Combo;
            Player.TotalData.HighestCombo = Math.Max(Combo, Player.TotalData.HighestCombo);
        Hit:
            int TempCritChance = Player.Critical;
            int TempCritMultiplier = Player.CritMultiplier;
            int TempDamage = Player.Damage;

            if (Skill != null)
            {
                Player.BonusResistance = Skill.Resistance;

                int Healed = Player.Hp;
                if (Player.Hp < Player.FullHp)
                {
                    Player.Hp += Skill.HP;
                    if (Player.Hp > Player.FullHp) Player.Hp = Player.FullHp;
                    Healed = Player.Hp - Healed;
                }
                else
                    Healed = 0;

                Player.TotalData.HpHealed += Healed;

                if (Combo == 0)
                {
                    Result.ResistanceBonus = Skill.Resistance;
                    Result.Healed = Healed;
                    return Task.FromResult(Result);
                }

                TempCritChance += Skill.CritChance;
                TempCritMultiplier += Skill.CritMultiplier;
                TempDamage += Skill.Damage;
            }

            if ((CritEnabled || TempCritChance > 50) && Crit(TempCritChance))
            {
                Result.Crit = true;
                TempDamage = TempCritMultiplier * TempDamage;
            }

                Player.TotalData.Durability += (long)Math.Ceiling((double)TempDamage / 10);

            int Res = Target.Resistance + Target.BonusResistance - Player.ResPenetration;
            if (TempDamage > Res)
            {
                if (Res < 0)
                    Res = 0;
                TempDamage -= Res;
                    Target.TotalData.DamageBlocked += Res;
                    Player.TotalData.DamageDealt += TempDamage;
                Target.Hp -= TempDamage;
            }
            else
                TempDamage = 0;

            Combo--;
            if (Combo > 0)
                goto Hit;

            Target.BonusResistance = 0;
            Result.Damage = TempDamage;
            return Task.FromResult(Result);
        }

        private bool Crit(int Chance)
        {
            if (Chance == 100)
                return true;
            return rng.Next(0, 100) < Chance;
        }


        private class GameState
        {
            public IUser Player1 { get; set; }
            public IUser Player2 { get; set; }
            public Character Player1Character { get; set; }
            public Character Player2Character { get; set; }
            public CharacterStats Player1Stats { get; set; }
            public CharacterStats Player2Stats { get; set; }
            public bool Player1Turn { get; set; }
            public bool Player1Won { get; set; }

            public GameState(IUser Player1, IUser Player2)
            {
                this.Player1 = Player1;
                this.Player2 = Player2;
                Player1Character = CharacterDatabase.GetCharacter(Player1.Id);
                Player2Character = CharacterDatabase.GetCharacter(Player2.Id);
                Player1Stats = new CharacterStats(Player1Character, Player2Character);
                Player2Stats = new CharacterStats(Player2Character, Player1Character);
                Player1Turn = new KurumiRandom().Next(0, 2) == 1;
            }

            #region Ingame
            public IUser CurrentPlayer => Player1Turn ? Player1 : Player2;
            public IUser OtherPlayer => Player1Turn ? Player2 : Player1;
            public Character CurrentCharacter => Player1Turn ? Player1Character : Player2Character;
            public Character OtherCharacter => Player1Turn ? Player2Character : Player1Character;
            public CharacterStats CurrentPlayerStats => Player1Turn ? Player1Stats : Player2Stats;
            public CharacterStats OtherPlayerStats => Player1Turn ? Player2Stats : Player1Stats;
            #endregion Ingame

            #region End
            public IUser WinnerPlayer => Player1Won ? Player1 : Player2;
            public IUser LoserPlayer => Player1Won ? Player2 : Player1;
            public Character WinnerCharacter => Player1Won ? Player1Character : Player2Character;
            public Character LoserCharacter => Player1Won ? Player2Character : Player1Character;
            public CharacterStats WinnerStats => Player1Won ? Player1Stats : Player2Stats;
            public CharacterStats LoserStats => Player1Won ? Player2Stats : Player1Stats;
            #endregion End


            public void NextTurn() => Player1Turn = !Player1Turn;
        }
        #endregion Game
    }

    public class HitResult
    {
        public int Healed { get; set; }
        public int ResistanceBonus { get; set; }
        public bool Crit { get; set; }
        public int Damage { get; set; }
        public int Combo { get; set; }
        public int BlockedDamage { get; set; }

        public int DurabilityChange { get; set; }
    }
}