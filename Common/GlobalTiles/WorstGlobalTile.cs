using Terraria;
using Terraria.ModLoader;
using WorstGame.Common.Configs;

namespace WorstGame.Common.GlobalTiles;

/// <summary>
/// 全局方块，对全部方块生效，通过模组配置开关控制方块相关恶搞逻辑
/// </summary>
public class WorstGlobalTile : GlobalTile
{
    /// <summary>
    /// 方块被破坏时，判断是否允许掉落物品
    /// </summary>
    /// <param name="i">方块X坐标</param>
    /// <param name="j">方块Y坐标</param>
    /// <param name="type">方块ID</param>
    /// <returns>false = 禁止掉落；base走原版逻辑</returns>
    public override bool CanDrop(int i, int j, int type)
    {
        if (TileConfigs.Instance.TileCanDrop)
        {
            // 10%概率阻止方块掉落物品
            if (Main.rand.NextFloat() < 0.1f)
            {
                return false;
            }
        }
        return base.CanDrop(i, j, type);
    }

    /// <summary>
    /// 方块附近的帧更新逻辑，方块处于屏幕附近时执行
    /// </summary>
    /// <param name="i">方块X坐标</param>
    /// <param name="j">方块Y坐标</param>
    /// <param name="type">方块ID</param>
    /// <param name="closer">是否距离玩家更近</param>
    public override void NearbyEffects(int i, int j, int type, bool closer)
    {
        if (TileConfigs.Instance.CampfireBurning)
        {
            // 篝火方块逻辑：靠近篝火的玩家会被施加燃烧Debuff
            if (type == Terraria.ID.TileID.Campfire)
            {
                // 篝火方块中心点（方块格子中心）
                var campfireCenter = new Microsoft.Xna.Framework.Vector2(i * 16 + 8, j * 16 + 8);
                // 遍历所有玩家
                for (int p = 0; p < Main.maxPlayers; p++)
                {
                    Player player = Main.player[p];
                    if (player.active && !player.dead)
                    {
                        float distance = Microsoft.Xna.Framework.Vector2.Distance(player.Center, campfireCenter);
                        // 距离小于60像素，给予燃烧Buff，持续90帧（1.5秒）
                        if (distance < 60f)
                        {
                            player.AddBuff(Terraria.ID.BuffID.OnFire, 90);
                        }
                    }
                }
            }
        }
    }
}
