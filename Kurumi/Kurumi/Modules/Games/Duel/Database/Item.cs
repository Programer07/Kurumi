using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Games.Duel.Database
{
    public class Item : IItem
    {
        #region Interface
        public int Id { get; set; } = int.MaxValue;
        public ItemType Type { get; set; } = ItemType.Invalid;
        public string Name { get; set; } = "Invalid-item";
        public int HP { get; set; } = 0;
        public int Damage { get; set; } = 0;
        public int Resistance { get; set; } = 0;
        public int ResPenetration { get; set; } = 0;
        public int CritChance { get; set; } = 0;
        public int CritMultiplier { get; set; } = 0;
        public int Combo { get; set; } = 0;
        public int StackLimit { get; set; } = 99;
        public long MaxDurability { get; set; } = 99;
        #endregion Interface

        public uint Price { get; set; } = 0;
        public bool Hidden { get; set; } = true;
        public string Collection { get; set; } = "Unknown";

        public override string ToString() => Name;
    }
}