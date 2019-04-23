using Discord;
using Kurumi.Common;
using Kurumi.Services.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Games.Duel.Database
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
                if (Characters[i].Data.Owner == OwnerId)
                    return Characters[i];
            }
            return null;
        }
        public static Character GetCharacter(string Name)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].Data.Name.Equals(Name, StringComparison.CurrentCultureIgnoreCase))
                    return Characters[i];
            }
            return null;
        }
        public static Task DeleteCharacter(Character character)
        {
            Directory.Delete($"{KurumiPathConfig.CharacterDatabase}{character.Data.Name}", true);
            Characters.Remove(character);
            return Task.CompletedTask;
        }


        public void Load()
        {
            string path = KurumiPathConfig.Data + "Items.json";
            if (File.Exists(path))
            {
                string items = File.ReadAllText(path);
                DefaultItems = new ConcurrentList<Item>(JsonConvert.DeserializeObject<List<Item>>(items));
                Console.Write("\r#Items loaded");
            }
            else
                Utilities.Log(new LogMessage(LogSeverity.Warning, "CharacterDatabase", "Items.json not found."));

            var characters = Directory.GetDirectories(KurumiPathConfig.CharacterDatabase);
            var DefaultItem = GetItem(x => x.Name == "Default") ?? new Item();
            for (int i = 0; i < characters.Length; i++)
            {
                string Path = characters[i] + KurumiPathConfig.Separator + "Character.json";
                string Content = File.ReadAllText(Path);
                var Character = JsonConvert.DeserializeObject<Character>(Content);
                Character.Data.Name = new DirectoryInfo(characters[i]).Name;
                Character.Equipment.BaseValues = DefaultItem;
                Character.Equipment.CharData = Character.Data;
                Characters.Add(Character);
                Console.Write($"\r#Loaded {i}/{characters.Length} | Current: {Character.Data.Name}");
            }
            Console.Write("\r                                                                                                           ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\r#Characters loaded.\n");
        }

        public void Save(bool Show)
        {
            for (int i = 0; i < Characters.Count; i++)
            {
                var character = Characters[i];
                string Content = JsonConvert.SerializeObject(character, Formatting.Indented);
                string Path = $"{KurumiPathConfig.CharacterDatabase}{character.Data.Name}";
                Directory.CreateDirectory(Path);
                Path += $"{KurumiPathConfig.Separator}Character.json";
                File.WriteAllText(Path, Content);
                if (Show)
                    Console.Write($"\r#Saved: {i}/{Characters.Count} | Current: {character.Data.Name}");
            }
            if (Show)
            {
                Console.Write("\r                                                                                                           ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\r#Characters saved.\n");
            }
        }
    }
}