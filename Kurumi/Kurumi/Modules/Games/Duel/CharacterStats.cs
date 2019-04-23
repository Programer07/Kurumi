using Kurumi.Modules.Games.Duel.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Games.Duel
{
    public class CharacterStats
    {
        public DataCollector TotalData { get; set; }
        public Character Character { get; set; }
        public int FullHp { get; }
        public int Hp { get; set; }
        public int Damage { get; set; }
        public int Resistance { get; set; }
        public int BonusResistance { get; set; }
        public int ResPenetration { get; set; }
        public int Critical { get; set; }
        public int CritMultiplier { get; set; }
        public CharacterStats(Character Character, Character Enemy)
        {
            TotalData = new DataCollector();
            this.Character = Character;
            FullHp = Character.Equipment.TotalHP(Enemy);
            Hp = FullHp;
            Damage = Character.Equipment.TotalDamage(Enemy);
            Resistance = Character.Equipment.TotalResistance(Enemy);
            ResPenetration = Character.Equipment.TotalResPenetration();
            Critical = Character.Equipment.TotalCritChance(Enemy);
            CritMultiplier = Character.Equipment.TotalCritMultiplier();
        }

        public CharacterStats(CharacterStats o)
        {
            TotalData = new DataCollector(o.TotalData);
            Character = o.Character;
            FullHp = o.FullHp;
            Hp = o.Hp;
            Damage = o.Damage;
            Resistance = o.Resistance;
            BonusResistance = o.BonusResistance;
            ResPenetration = o.ResPenetration;
            Critical = o.Critical;
            CritMultiplier = o.CritMultiplier;
        }
    }
    public class DataCollector
    {
        public int DamageDealt { get; set; }
        public int DamageBlocked { get; set; }
        public int HighestCombo { get; set; }
        public int HpHealed { get; set; }
        public long Durability { get; set; }

        public DataCollector() { }
        public DataCollector(DataCollector c)
        {
            DamageDealt = c.DamageDealt;
            DamageBlocked = c.DamageBlocked;
            HighestCombo = c.HighestCombo;
            HpHealed = c.HpHealed;
            Durability = c.Durability;
        }
    }
}