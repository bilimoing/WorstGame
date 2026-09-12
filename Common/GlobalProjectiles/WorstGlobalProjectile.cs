using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorstGame.Common.Configs;

namespace WorstGame.Common.GlobalProjectiles;

/// <summary>
/// 全局弹幕，对所有弹幕生效，通过模组配置开关控制各类恶搞/负面弹幕逻辑
/// </summary>
public class WorstGlobalProjectile : GlobalProjectile
{
    /// <summary>
    /// 每个弹幕实体拥有独立的GlobalProjectile实例数据
    /// </summary>
    public override bool InstancePerEntity => true;

    /// <summary>
    /// 克隆弹幕实例时复制本GlobalProjectile状态
    /// </summary>
    protected override bool CloneNewInstances => true;

    /// <summary>
    /// 弹幕击中玩家时修改受伤伤害
    /// </summary>
    /// <param name="projectile">发起攻击的弹幕</param>
    /// <param name="target">受击玩家</param>
    /// <param name="modifiers">受伤伤害修饰器</param>
    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        // 开启敌方弹幕增伤配置，仅对敌对弹幕生效
        if (ProjectileConfigs.Instance.ProjectileCritChance)
        {
            if (!projectile.friendly && projectile.hostile)
            {
                // 随机1.1‑1.5倍最终伤害模拟敌方暴击效果
                float critMultiplier = Main.rand.NextFloat(1.1f, 1.5f);
                modifiers.FinalDamage *= critMultiplier;
            }
        }
    }

    /// <summary>
    /// 弹幕击中NPC时修改命中伤害
    /// </summary>
    /// <param name="projectile">发起攻击的弹幕</param>
    /// <param name="target">受击NPC</param>
    /// <param name="modifiers">命中伤害修饰器</param>
    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (ProjectileConfigs.Instance.ProjectileCritChance)
        {
            if (!projectile.friendly && projectile.hostile)
            {
                float critMultiplier = Main.rand.NextFloat(1.1f, 1.5f);
                modifiers.FinalDamage *= critMultiplier;
            }
        }
    }

    /// <summary>
    /// 判断该弹幕类型是否应当不被绘制
    /// 逻辑：偶数返回true(不绘制)；质数返回true(不绘制)；合数返回false(正常绘制)
    /// </summary>
    /// <param name="projectileId">弹幕Type ID</param>
    /// <returns>true = 不绘制；false = 正常绘制</returns>
    public static bool ShouldNotDraw(int projectileId)
    {
        // 偶数弹幕ID，隐藏
        if (projectileId % 2 == 0)
        {
            return true;
        }
        // ID小于2直接允许绘制
        if (projectileId < 2)
        {
            return false;
        }
        // 质数判断：能被整除即为合数 → 返回false，正常绘制
        for (int i = 2; i * i <= projectileId; i++)
        {
            if (projectileId % i == 0)
            {
                return false;
            }
        }
        // 是质数，隐藏弹幕
        return true;
    }

    /// <summary>
    /// 弹幕绘制前置钩子，控制弹幕是否渲染
    /// </summary>
    /// <param name="projectile">待绘制弹幕</param>
    /// <param name="player">绘制目标玩家</param>
    /// <param name="lightColor">光照颜色</param>
    /// <returns>false：跳过绘制；true：执行原版绘制</returns>
    public override bool PreDraw(Projectile projectile, Player player, ref Color lightColor)
    {
        if (ProjectileConfigs.Instance.ProjectileInvisibility)
        {
            if (ShouldNotDraw(projectile.type))
            {
                return false;
            }
        }
        return base.PreDraw(projectile, player, ref lightColor);
    }

    /// <summary>
    /// 弹幕AI执行完成后运行的逻辑
    /// </summary>
    /// <param name="projectile">当前弹幕</param>
    public override void PostAI(Projectile projectile)
    {
        // 滚动仙人掌/仙人掌尖刺弹幕碰撞到世界物品时直接销毁物品
        if (ProjectileConfigs.Instance.ProjectileKillItem)
        {
            if (projectile.type is ProjectileID.RollingCactus or ProjectileID.RollingCactusSpike)
            {
                Rectangle projectileHitbox = projectile.Hitbox;
                // 遍历所有世界掉落物品
                for (int i = 0; i < Main.maxItems; i++)
                {
                    WorldItem item = Main.item[i];
                    if (item.active && item.type > ItemID.None)
                    {
                        Rectangle itemHitbox = new Rectangle((int)item.position.X, (int)item.position.Y, item.width, item.height);
                        // 碰撞检测，重叠则销毁物品
                        if (projectileHitbox.Intersects(itemHitbox))
                        {
                            item.TurnToAir();
                        }
                    }
                }
            }
        }
    }
}
