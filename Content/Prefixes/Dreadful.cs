using Terraria;
using Terraria.ModLoader;

namespace WorstGame.Content.Prefixes
{
    public class Dreadful : ModPrefix
    {
        public override PrefixCategory Category => PrefixCategory.Melee;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 0f;
        }
        public override bool CanRoll(Item item)
        {
            return item.DamageType == DamageClass.Melee || item.DamageType == DamageClass.MeleeNoSpeed;
        }
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 0.85f;
            useTimeMult *= 1.10f;
            critBonus -= 5;
            scaleMult *= 0.90f;
            knockbackMult *= 0.85f;
        }
    }
}
