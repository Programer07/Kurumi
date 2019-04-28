using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public interface IItem
    {
        int Id { get; set; }
        ItemType Type { get; set; }
        string Name { get; set; }
        int HP { get; set; }
        int Damage { get; set; }
        int Resistance { get; set; }
        int ResPenetration { get; set; }
        int CritChance { get; set; }
        int CritMultiplier { get; set; }
        int Combo { get; set; }
        int StackLimit { get; set; }
        long MaxDurability { get; set; }
    }
    public enum ItemType
    {
        Invalid,
        Weapon,
        Boots,
        Hat,
        Shirt,
        Coat,
        Glove,
        Leggings,
        Skill
    }
}