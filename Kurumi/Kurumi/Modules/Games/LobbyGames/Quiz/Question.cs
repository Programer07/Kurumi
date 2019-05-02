using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Games.LobbyGames.Quiz
{
    public class Question
    {
        [JsonProperty("Question")]
        public string _Question { get; set; }
        public string[] Answers { get; set; }
        public byte Correct { get; set; }
        public string ImageUrl { get; set; }
    }
}