using Kurumi.Common;
using Kurumi.Common.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public class Character
    {
        public ObjectId Id { get; set; }
        #region Data
        public string Name { get; set; }
        public string ProfilePicture { get; set; }
        public int Exp { get; set; }
        public bool Ai { get; set; }
        public ulong Owner { get; set; }

        public int GetLevel(Character enemy = null)
        {
            if (Ai && enemy != null)
                return enemy.GetLevel();

            return (int)(1 + Math.Sqrt(1 + 8 * Exp / 100)) / 2;
        }
        #endregion Data

        #region Equipment
        [BsonIgnore]
        public PlayerItem BaseValues { get; set; }

        public PlayerItem Weapon { get; set; }
        public PlayerItem Boots { get; set; }
        public PlayerItem Hat { get; set; }
        public PlayerItem Shirt { get; set; }
        public PlayerItem Coat { get; set; }
        public PlayerItem Glove { get; set; }
        public PlayerItem Leggings { get; set; }
        public List<PlayerItem> Inventory { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int A { get; set; }


        public string InventoryToString(LanguageDictionary lang)
        {
            if (Inventory.Count == 0)
                return lang["character_inv_empty"];
            else
            {
                var Inventory = new List<PlayerItem>(this.Inventory);
                string s = string.Empty;
                for (; Inventory.Count != 0;)
                {
                    var item = Inventory[0];
                    if (item.StackLimit > 1)
                    {
                        int count = Inventory.Count(x => x.Id == item.Id);
                        Inventory.RemoveAll(x => x.Id == item.Id);
                        s += $"{(count.ToString() + "x").Space(2)} | {item.Name}\n";
                    }
                    else
                    {
                        Inventory.Remove(item);
                        s += $"{"1x".Space(2)} | {item.ToString(lang, 5)}\n";
                    }
                }
                return s;
            }
        }
        public bool HasItem(Item item)
        {
            for (int i = 0; i < Inventory.Count; i++)
            {
                if (Inventory[i].Id == item.Id)
                    return true;
            }
            return false;
        }
        public List<PlayerItem> DamageItems(long WeaponDamage, long ArmorDamage)
        {
            List<PlayerItem> BrokenItems = new List<PlayerItem>();

            if (Ai)
                return BrokenItems;

            if (Weapon != null && WeaponDamage > 0)
            {
                Weapon.Durability -= WeaponDamage;
                if (Weapon.Durability <= 0)
                {
                    BrokenItems.Add(Weapon);
                    Weapon = null;
                }
            }

            if (Boots != null && ArmorDamage > 0)
            {
                Boots.Durability -= ArmorDamage;
                if (Boots.Durability <= 0)
                {
                    BrokenItems.Add(Boots);
                    Boots = null;
                }
            }

            if (Hat != null && ArmorDamage > 0)
            {
                Hat.Durability -= ArmorDamage;
                if (Hat.Durability <= 0)
                {
                    BrokenItems.Add(Hat);
                    Hat = null;
                }
            }

            if (Shirt != null && ArmorDamage > 0)
            {
                Shirt.Durability -= ArmorDamage;
                if (Shirt.Durability <= 0)
                {
                    BrokenItems.Add(Shirt);
                    Shirt = null;
                }
            }

            if (Coat != null && ArmorDamage > 0)
            {
                Coat.Durability -= ArmorDamage;
                if (Coat.Durability <= 0)
                {
                    BrokenItems.Add(Coat);
                    Coat = null;
                }
            }

            if (Glove != null && ArmorDamage > 0)
            {
                Glove.Durability -= ArmorDamage;
                if (Glove.Durability <= 0)
                {
                    BrokenItems.Add(Glove);
                    Glove = null;
                }
            }

            if (Leggings != null && ArmorDamage > 0)
            {
                Leggings.Durability -= ArmorDamage;
                if (Leggings.Durability <= 0)
                {
                    BrokenItems.Add(Leggings);
                    Leggings = null;
                }
            }

            return BrokenItems;
        }


        public int TotalDamage(Character enemy = null)
        {
            return (int)((BaseValues?.Damage ?? 0) +
                        (Weapon?.Damage ?? 0) * DurabilityModifier(Weapon) +
                        (Boots?.Damage ?? 0) * DurabilityModifier(Boots) +
                        (Hat?.Damage ?? 0) * DurabilityModifier(Hat) +
                        (Shirt?.Damage ?? 0) * DurabilityModifier(Shirt) +
                        (Coat?.Damage ?? 0) * DurabilityModifier(Coat) +
                        (Glove?.Damage ?? 0) * DurabilityModifier(Glove) +
                        (Leggings?.Damage ?? 0) * DurabilityModifier(Leggings) +
                   (int)Math.Floor(GetLevel(enemy) * 0.5M));
        }
        public int TotalHP(Character enemy = null)
        {
            return (int)((BaseValues?.HP ?? 0) +
                        (Weapon?.HP ?? 0) * DurabilityModifier(Weapon) +
                        (Boots?.HP ?? 0) * DurabilityModifier(Boots) +
                        (Hat?.HP ?? 0) * DurabilityModifier(Hat) +
                        (Shirt?.HP ?? 0) * DurabilityModifier(Shirt) +
                        (Coat?.HP ?? 0) * DurabilityModifier(Coat) +
                        (Glove?.HP ?? 0) * DurabilityModifier(Glove) +
                        (Leggings?.HP ?? 0) * DurabilityModifier(Leggings) +
                        GetLevel(enemy) * 2);
        }
        public int TotalResistance(Character enemy = null)
        {
            return (int)((BaseValues?.Resistance ?? 0) +
                        (Weapon?.Resistance ?? 0) * DurabilityModifier(Weapon) +
                        (Boots?.Resistance ?? 0) * DurabilityModifier(Boots) +
                        (Hat?.Resistance ?? 0) * DurabilityModifier(Hat) +
                        (Shirt?.Resistance ?? 0) * DurabilityModifier(Shirt) +
                        (Coat?.Resistance ?? 0) * DurabilityModifier(Coat) +
                        (Glove?.Resistance ?? 0) * DurabilityModifier(Glove) +
                        (Leggings?.Resistance ?? 0) * DurabilityModifier(Leggings) +
                   (int)Math.Floor(GetLevel(enemy) * 0.2M));
        }
        public int TotalResPenetration()
        {
            return (int)((BaseValues?.ResPenetration ?? 0) +
                        (Weapon?.ResPenetration ?? 0) * DurabilityModifier(Weapon) +
                        (Boots?.ResPenetration ?? 0) * DurabilityModifier(Boots) +
                        (Hat?.ResPenetration ?? 0) * DurabilityModifier(Hat) +
                        (Shirt?.ResPenetration ?? 0) * DurabilityModifier(Shirt) +
                        (Coat?.ResPenetration ?? 0) * DurabilityModifier(Coat) +
                        (Glove?.ResPenetration ?? 0) * DurabilityModifier(Glove) +
                        (Leggings?.ResPenetration ?? 0) * DurabilityModifier(Leggings));
        }
        public int TotalCritChance(Character enemy = null)
        {
            int crit = (int)((BaseValues?.CritChance ?? 0) +
                            (Weapon?.CritChance ?? 0) * DurabilityModifier(Weapon) +
                            (Boots?.CritChance ?? 0) * DurabilityModifier(Boots) +
                            (Hat?.CritChance ?? 0) * DurabilityModifier(Hat) +
                            (Shirt?.CritChance ?? 0) * DurabilityModifier(Shirt) +
                            (Coat?.CritChance ?? 0) * DurabilityModifier(Coat) +
                            (Glove?.CritChance ?? 0) * DurabilityModifier(Glove) +
                            (Leggings?.CritChance ?? 0) * DurabilityModifier(Leggings) +
                        (int)Math.Floor(GetLevel(enemy) * 0.05M));
            if (crit > 100)
                crit = 100;
            return crit;
        }
        public int TotalCritMultiplier()
        {
            return (int)((BaseValues?.CritMultiplier ?? 0) +
                        (Weapon?.CritMultiplier ?? 0) * DurabilityModifier(Weapon) +
                        (Boots?.CritMultiplier ?? 0) * DurabilityModifier(Boots) +
                        (Hat?.CritMultiplier ?? 0) * DurabilityModifier(Hat) +
                        (Shirt?.CritMultiplier ?? 0) * DurabilityModifier(Shirt) +
                        (Coat?.CritMultiplier ?? 0) * DurabilityModifier(Coat) +
                        (Glove?.CritMultiplier ?? 0) * DurabilityModifier(Glove) +
                        (Leggings?.CritMultiplier ?? 0) * DurabilityModifier(Leggings) + 2);
        }

        private double DurabilityModifier(PlayerItem item)
        {
            if (item == null || Ai)
                return 1;
            int durabilityPercent = (int)Math.Round(((double)item.Durability / item.MaxDurability) * 100, 0);
            if (durabilityPercent >= 50)
                return 1;
            else
                return (double)(50 + durabilityPercent) / 100;
        }
        #endregion Equipment
    }
}