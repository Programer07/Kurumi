using Discord;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Kurumi.Services.Database.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Services.Database.Databases
{
    public class CharacterDatabase : IKurumiDatabase //TODO: Repair command
    {
        public static ConcurrentList<Item> DefaultItems = new ConcurrentList<Item>();
        public static ConcurrentList<Character> Characters = new ConcurrentList<Character>();


        public static Item GetItem(Func<Item, bool> Search) => DefaultItems.FirstOrDefault(Search) ?? new Item();
        public static Item GetItem(int Id)
        {
            for (int i = 0; i < DefaultItems.Count; i++)
            {
                if (DefaultItems[i].Id == Id)
                    return DefaultItems[i];
            }
            return null;
        }
        public static Character GetCharacter(ulong OwnerId)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].Owner == OwnerId)
                    return Characters[i];
            }
            return null;
        }
        public static Character GetCharacter(string Name)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].Name.Equals(Name, StringComparison.CurrentCultureIgnoreCase))
                    return Characters[i];
            }
            return null;
        }

        public void Load()
        {
            string path = KurumiPathConfig.Data + "Items.json";
            if (File.Exists(path))
            {
                string items = File.ReadAllText(path);
                DefaultItems = new ConcurrentList<Item>(JsonConvert.DeserializeObject<List<Item>>(items));
                ConsoleHelper.Write("Items loaded", ConsoleColor.Green);
            }
            else
                Utilities.Log(new LogMessage(LogSeverity.Warning, "CharacterDatabase", "Items.json not found."));

            ConsoleHelper.Write("Waiting for MongoDB...", ConsoleColor.Yellow);
            var DefaultItem = GetItem(x => x.Name == "Default") ?? new Item();
            var characters = DatabaseManager.Database.GetCollection<Character>("Character").Find(_ => true).ToList();
            for (int i = 0; i < characters.Count; i++)
            {
                var Character = characters[i];
                Character.BaseValues = DefaultItem;
                Characters.Add(Character);
                ConsoleHelper.Write($"Loaded {i}/{characters.Count} | Current: {Character.Name}", ConsoleColor.Yellow);
            }
            ConsoleHelper.ClearCurrentLine();
            ConsoleHelper.WriteLine("Characters loaded.", ConsoleColor.Green);
        }

        public void Save(bool Show)
        {
            if (Characters.Count == 0)
                return;
            var Collection = DatabaseManager.Database.GetCollection<Character>("Character_Temp");
            for (int i = 0; i < Characters.Count; i++)
            {
                var character = Characters[i];
                Collection.InsertOne(character);
                if (Show)
                    ConsoleHelper.Write($"Saved: {i}/{Characters.Count} | Current: {character.Name}", ConsoleColor.Yellow);
            }
            DatabaseManager.Database.DropCollection("Character");
            DatabaseManager.Database.RenameCollection("Character_Temp", "Character");
            if (Show)
            {
                ConsoleHelper.ClearCurrentLine();
                ConsoleHelper.Write("Characters saved.\n", ConsoleColor.Green);
            }
        }
    }
}