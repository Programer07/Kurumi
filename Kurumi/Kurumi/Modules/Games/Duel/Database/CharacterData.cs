using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Games.Duel.Database
{
    public class CharacterData
    {
        [JsonIgnore]
        public string Name { get; set; }
        public string ProfilePicture { get; set; }
        public int Exp { get; set; }
        public bool Ai { get; set; }
        public ulong Owner { get; set; }

        public int GetLevel(Character enemy = null)
        {
            if (Ai && enemy != null)
                return enemy.Data.GetLevel();

            return (int)(1 + Math.Sqrt(1 + 8 * Exp / 100)) / 2;
        }
    }
}