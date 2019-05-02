using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Modules.LobbyGames.Games;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Databases;
using Kurumi.Services.Random;
using Newtonsoft.Json;

namespace Kurumi.Modules.Games.LobbyGames.Quiz
{
    public class QuizGame : IGame
    {
        public int MinPlayers { get; private set; }
        public int MaxPlayers { get; private set; }
        public Dictionary<string, object> Settings { get; private set; }
        public List<ulong> Surrender { get; private set; }
        public bool AI { get; private set; }

        public QuizGame()
        {
            MinPlayers = 1;
            MaxPlayers = 1;
            Settings = new Dictionary<string, object>();
            Surrender = new List<ulong>();
            rng = new KurumiRandom();
            AI = false;
        }
        public bool ValidPlayer(IUser user) => true;

        #region Game
        private readonly KurumiRandom rng;
        public QuizAnswer? Answer = QuizAnswer.A;
        public IUser player;

        public Task Start(Lobby lobby, ICommandContext Context)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var lang = Language.GetLanguage(Context.Guild);
                    player = lobby.Players[0];
                    //Load questions
                    List<Question> Questions = new List<Question>();
                    string[] Files = Directory.GetFiles(KurumiPathConfig.Quiz);
                    for (int i = 0; i < Files.Length; i++)
                    {
                        Questions.Add(JsonConvert.DeserializeObject<Question>(File.ReadAllText(Files[i])));
                    }
                    //Setup game variables
                    int HP = 3;
                    int Score = 0;
                    bool Fail = false;
                    //Start
                    while (Surrender.Count == 0)
                    {
                        //Get lang
                        lang = Language.GetLanguage(Context.Guild);

                        //Select next question
                        int Index = rng.Next(0, Questions.Count);
                        var NextQuestion = Questions[Index];

                        //Send Question
                        bool Image = NextQuestion.ImageUrl != "-";
                        string Answers = string.Empty;
                        char[] ABC = new char[4] { 'A', 'B', 'C', 'D' };
                        for (int i = 0; i < 4; i++)
                        {
                            Answers += $"{ABC[i]}) {NextQuestion.Answers[i]}\n";
                        }
                        if (Image)
                            Answers += $"[{lang["quiz_imageurl"]}]({NextQuestion.ImageUrl})";

                        int Answered = Score / 300 + (3 - HP);
                        await Context.Channel.SendEmbedAsync(Answers, Title: NextQuestion._Question, Footer: lang["quiz_footer", "HP", HP, "SCORE", Score,
                                            "QUESTION", $"{Answered}/{Questions.Count + Answered}"], ImageUrl: Image ? NextQuestion.ImageUrl : null);

                        //Wait for answer
                        Answer = null;
                        int Timer = 300; //30 sec
                        while (Answer == null && Timer != 0 && !Surrender.Contains(player.Id))
                        {
                            Timer--;
                            await Task.Delay(100);
                        }

                        //Surrender
                        if (Surrender.Contains(player.Id))
                        {
                            Fail = true; 
                            break;
                        }

                        //Add score
                        if (Timer == 0 || Answer == null || NextQuestion.Correct != (byte)Answer)
                            HP--;
                        else
                            Score += 300;

                        //Fail
                        if (HP == 0)
                        {
                            Fail = true;
                            break;
                        }

                        //Ran out of questions
                        Questions.Remove(NextQuestion);
                        if(Questions.Count == 0)
                            break;
                        
                    }
                    //Send message
                    if (Fail)
                    {
                        await Context.Channel.SendEmbedAsync(lang["quiz_lost", "SCORE", Score]);
                    }
                    else
                    {
                        await Context.Channel.SendEmbedAsync(lang["quiz_won", "SCORE", Score, "HP", HP]);
                        UserDatabase.GetOrCreate(player.Id).Credit += 50;
                    }

                    await Utilities.Log(new LogMessage(LogSeverity.Info, "Quiz", "success"), Context);
                }
                catch (Exception ex)
                {
                    await Utilities.Log(new LogMessage(LogSeverity.Error, "Quiz", null, ex), Context);
                }
                return Task.CompletedTask;
            });
        }
        public enum QuizAnswer : byte
        {
            A,
            B,
            C,
            D
        }
        #endregion Game

        public override string ToString() => "lobby_quiz";
    }
}