using Kurumi.Modules.Games.Duel.Database;
using Kurumi.Services.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kurumi.Modules.Games.Duel
{
    public class Ai
    {
        /* Fundraising: AI
         * 
         * Hiring: ML
         * 
         * Implementing: Linear regression
         * 
         * Kurumi: 8 if statements
         * 
         * (https://www.reddit.com/r/ProgrammerHumor/comments/b22qm3/itd_be_like_that_sometimes/)
         */


        //I wrote this in 2017 and changed it a bit to make it work in the new update. TODO: Write a better
        public static Item SelectSkill(CharacterStats AIStats, CharacterStats PlayerStats, Character AI, Character Player, DuelGame game)
        {
            CharacterStats[] AiStatsArray = new CharacterStats[4]
            {
                new CharacterStats(AIStats),
                new CharacterStats(AIStats),
                new CharacterStats(AIStats),
                new CharacterStats(AIStats)
            };

            CharacterStats[] PlayerStatsArray = new CharacterStats[4]
            {
                new CharacterStats(PlayerStats),
                new CharacterStats(PlayerStats),
                new CharacterStats(PlayerStats),
                new CharacterStats(PlayerStats),
            };

            Item[] AiSkills = new Item[]
            {
                CharacterDatabase.GetItem(x => x.Id == AI.Equipment.X),
                CharacterDatabase.GetItem(x => x.Id == AI.Equipment.A),
                CharacterDatabase.GetItem(x => x.Id == AI.Equipment.Y),
                null
            };

            Item[] PlayerSkills = new Item[]
            {
                CharacterDatabase.GetItem(x => x.Id == Player.Equipment.X),
                CharacterDatabase.GetItem(x => x.Id == Player.Equipment.A),
                CharacterDatabase.GetItem(x => x.Id == Player.Equipment.Y),
                null
            };
            
            for (int i = 0; i < 4; i++)
            {
                game.Hit(new CharacterStats(PlayerStats), AiStatsArray[i], AiSkills[i], false);
            }

            for (int i = 0; i < 4; i++)
            {
                game.Hit(PlayerStatsArray[i], new CharacterStats(AIStats), PlayerSkills[i], false);
            }


            if (Dead(PlayerStatsArray)) //Check if Kurumi can kill the player with one skill
            {
                return HighestDamage(PlayerStatsArray, AiSkills);
            }
            else
            {
                if (Dead(AiStatsArray)) //Check if Kurumi dies next round
                {
                    //Get all heal skill
                    Item[] HealSkills = AiSkills.Where(x => x != null && x.HP != 0).ToArray();
                    //Check if the HP is full or there are no heal skills
                    if (AIStats.Hp == AIStats.FullHp || HealSkills.Length == 0)
                        //Select a random skill
                        return RandomSkill(HealSkills);
                    //Select best heal skill
                    Item BestHealSkill;
                    if (HealSkills.Length == 1)
                        BestHealSkill = HealSkills[0];
                    else
                        BestHealSkill = HealSkills.OrderByDescending(x => x.HP).ElementAt(0);

                    return BestHealSkill;
                }
                else
                {
                    return HighestDamage(PlayerStatsArray, AiSkills);
                }
            }
        }
        private static Item RandomSkill(Item[] healSkills)
        {
            KurumiRandom random = new KurumiRandom();
            return healSkills[random.Next(0, healSkills.Length)];
        }

        private static bool Dead(CharacterStats[] Characters)
        {
            foreach (CharacterStats c in Characters)
            {
                if (c.Hp <= 0)
                    return true;
            }
            return false;
        }

        private static Item HighestDamage(CharacterStats[] Characters, Item[] Skills)
        {
            Item LowestEnemyHp = null;
            int LowestHp = int.MaxValue;
            for (int i = 0; i < Characters.Length; i++)
            {
                CharacterStats c = Characters[i];
                if (c.Hp <= 0)
                    return Skills[i];
                int Min = Math.Min(LowestHp, c.Hp);
                if (Min != LowestHp)
                {
                    LowestEnemyHp = Skills[i];
                    LowestHp = Min;
                }
            }
            return LowestEnemyHp;
        }
    }
}