using Kurumi.Common;
using Kurumi.Common.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Games.Duel.Database
{
    public class PlayerItem : IItem
    {
        #region Interface
        public int Id { get; set; }
        [JsonIgnore]
        public ItemType Type { get; set; }
        [JsonIgnore]
        public string Name { get; set; }
        [JsonIgnore]
        public int HP { get; set; }
        [JsonIgnore]
        public int Damage { get; set; }
        [JsonIgnore]
        public int Resistance { get; set; }
        [JsonIgnore]
        public int ResPenetration { get; set; }
        [JsonIgnore]
        public int CritChance { get; set; }
        [JsonIgnore]
        public int CritMultiplier { get; set; }
        [JsonIgnore]
        public int Combo { get; set; }
        [JsonIgnore]
        public int StackLimit { get; set; }
        [JsonIgnore]
        public long MaxDurability { get; set; }

        public long Durability { get; set; }
        #endregion Interface

        public Dictionary<string, int> CustomEffects { get; set; }

        [JsonConstructor]
        public PlayerItem(int Id, int Durability, Dictionary<string, int> CustomEffects)
        {
            this.Id = Id;
            this.Durability = Durability;
            this.CustomEffects = CustomEffects;
            Load(CharacterDatabase.GetItem(Id));
            ApplyEffects();
        }
        public PlayerItem(IItem item)
        {
            Load(item);
            CustomEffects = new Dictionary<string, int>();
        }
        public PlayerItem(IItem item, Dictionary<string, int> CustomEffects)
        {
            Load(item);
            this.CustomEffects = CustomEffects;
        }


        public static implicit operator PlayerItem(Item item) => new PlayerItem(item);
        public string ToString(LanguageDictionary lang, int Indent = 10)
        {
            string s = Name;
            if (MaxDurability != -1)
            {
                double Percentage = Math.Round(((double)Durability / MaxDurability) * 100, 0);
                if (Percentage != 100 && Percentage > 0)
                    s += $" ({Percentage}%)";
            }

            if (CustomEffects.Count != 0)
            {
                s += "\n" + $"**{lang["character_effects"]}**".Space(Indent, false, false);
                foreach (var Effect in CustomEffects)
                {
                    s += "\n" + $"▸{lang[AsLangString(Effect.Key)]}: {(Effect.Value > 0 ? $"+{Effect.Value}" : $"{Effect.Value}")}".Space(Indent + 2, false, false);
                }
            }
            return s;
        }

        private string AsLangString(string Effect)
        {
            switch (Effect)
            {
                case "Damage":
                    return "character_damage";
                case "HP":
                    return "character_hp";
                case "Resistance":
                    return "character_resistance";
                case "ResPenetration":
                    return "character_resistance_pen";
                case "CritChance":
                    return "character_critical";
                case "CritMultiplier":
                    return "character_critical_multiplier";
                default:
                    return "null";
            }
        }
        private void ApplyEffects()
        {
            if (CustomEffects.ContainsKey("Damage"))
                Damage += CustomEffects["Damage"];
            if (CustomEffects.ContainsKey("Resistance"))
                Resistance += CustomEffects["Resistance"];
            if (CustomEffects.ContainsKey("ResPenetration"))
                ResPenetration += CustomEffects["ResPenetration"];
            if (CustomEffects.ContainsKey("CritChance"))
                CritChance += CustomEffects["CritChance"];
            if (CustomEffects.ContainsKey("CritMultiplier"))
                CritMultiplier += CustomEffects["CritMultiplier"];
            if (CustomEffects.ContainsKey("Combo"))
                Combo += CustomEffects["Combo"];
            if (CustomEffects.ContainsKey("Durability"))
                MaxDurability += CustomEffects["Durability"];
        }
        private void Load(IItem item)
        {
            if (item == null)
                return;
            Type = item.Type;
            Name = item.Name;
            HP = item.HP;
            Damage = item.Damage;
            Resistance = item.Resistance;
            ResPenetration = item.ResPenetration;
            CritChance = item.CritChance;
            CritMultiplier = item.CritMultiplier;
            Combo = item.Combo;
            StackLimit = item.StackLimit;
            Id = item.Id;
            MaxDurability = item.MaxDurability;

            if (Durability == 0)
                Durability = item.MaxDurability;
        }
    }
}