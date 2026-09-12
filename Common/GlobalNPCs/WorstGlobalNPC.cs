using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WorstGame.Common.Configs;

namespace WorstGame.Common.GlobalNPCs;

/// <summary>
/// 全局NPC，对全部NPC生效，由模组配置开关控制各类恶搞/负面NPC逻辑
/// </summary>
public class WorstGlobalNPC : GlobalNPC
{
    /// <summary>
    /// 标记NPC是否已经执行过分裂，索引对应NPC.whoAmI
    /// </summary>
    private static bool[] hasSplit = new bool[Main.maxNPCs];

    /// <summary>
    /// 每个NPC实体拥有独立GlobalNPC数据
    /// </summary>
    public override bool InstancePerEntity => true;

    /// <summary>
    /// 克隆NPC实例时复制GlobalNPC状态
    /// </summary>
    protected override bool CloneNewInstances => true;

    /// <summary>
    /// 判断NPC是否为穿墙NPC（无方块碰撞、非Boss、非友好NPC）
    /// </summary>
    /// <param name="npc">目标NPC</param>
    /// <returns>true = 符合穿墙NPC条件</returns>
    private static bool IsWallPhasingNPC(NPC npc)
    {
        return npc.noTileCollide && !npc.boss && !npc.friendly;
    }

    /// <summary>
    /// 判断NPC包围盒是否处在实心方块内部
    /// </summary>
    /// <param name="npc">目标NPC</param>
    /// <returns>true = NPC身体处于实心方块内</returns>
    private static bool IsInsideSolidTile(NPC npc)
    {
        Rectangle npcRect = npc.getRect();
        // 把NPC包围盒坐标转换为方块坐标
        int startX = (int)(npcRect.Left / 16f);
        int endX = (int)(npcRect.Right / 16f);
        int startY = (int)(npcRect.Top / 16f);
        int endY = (int)(npcRect.Bottom / 16f);

        // 遍历NPC占据范围内所有方块
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                // 判断方块在世界内、存在方块、实心方块、不是平台(tileSolidTop)
                if (WorldGen.InWorld(x, y) && Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType] && !Main.tileSolidTop[Main.tile[x, y].TileType])
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 玩家物品攻击判定：穿墙怪在墙内时无法被玩家物品击中
    /// </summary>
    /// <param name="npc">被攻击NPC</param>
    /// <param name="player">攻击者</param>
    /// <param name="item">使用的武器物品</param>
    /// <returns>null走原版逻辑；false代表不能被击中</returns>
    public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
    {
        if (NPCConfigs.Instance.WallNPCinvincible && IsWallPhasingNPC(npc) && IsInsideSolidTile(npc))
        {
            return false;
        }
        return base.CanBeHitByItem(npc, player, item);
    }

    /// <summary>
    /// 弹幕攻击判定：穿墙怪在墙内时无法被弹幕击中
    /// </summary>
    /// <param name="npc">被攻击NPC</param>
    /// <param name="projectile">攻击弹幕</param>
    /// <returns>null走原版逻辑；false代表不能被击中</returns>
    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        if (NPCConfigs.Instance.WallNPCinvincible && IsWallPhasingNPC(npc) && IsInsideSolidTile(npc))
        {
            return false;
        }
        return base.CanBeHitByProjectile(npc, projectile);
    }

    /// <summary>
    /// NPC之间互相攻击判定：穿墙怪在墙内时不会被其他NPC攻击
    /// </summary>
    /// <param name="npc">受击NPC</param>
    /// <param name="attacker">攻击方NPC</param>
    /// <returns>false代表无法被击中</returns>
    public override bool CanBeHitByNPC(NPC npc, NPC attacker)
    {
        if (NPCConfigs.Instance.WallNPCinvincible && IsWallPhasingNPC(npc) && IsInsideSolidTile(npc))
        {
            return false;
        }
        return base.CanBeHitByNPC(npc, attacker);
    }

    /// <summary>
    /// 修改NPC商店商品，随机删除店内物品
    /// </summary>
    /// <param name="npc">商店所属NPC</param>
    /// <param name="shopName">商店名称标识</param>
    /// <param name="items">商店物品数组</param>
    public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
    {
        if (NPCConfigs.Instance.NPCShopRandomDeleteItem)
        {
            Random random = new Random();
            foreach (var t in items)
            {
                // 50%概率把非空气物品置空
                if (random.Next(2) == 0 && t is { IsAir: false })
                {
                    t.TurnToAir();
                }
            }
        }
    }

    /// <summary>
    /// 被玩家物品击中，触发分裂检测
    /// </summary>
    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        if (NPCConfigs.Instance.NPCSplit && !npc.boss)
        {
            TrySplit(npc);
        }
    }

    /// <summary>
    /// 被弹幕击中，触发分裂检测
    /// </summary>
    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        if (NPCConfigs.Instance.NPCSplit && !npc.boss)
        {
            TrySplit(npc);
        }
    }

    /// <summary>
    /// NPC死亡，重置该NPC的分裂标记
    /// </summary>
    public override void OnKill(NPC npc)
    {
        if (NPCConfigs.Instance.NPCSplit && !npc.boss)
        {
            if (npc.whoAmI < hasSplit.Length)
            {
                hasSplit[npc.whoAmI] = false;
            }
        }
    }

    /// <summary>
    /// 尝试执行NPC分裂逻辑：生命低于一半、未分裂过才允许分裂
    /// 仅服务端/单机执行生成，客户端禁止生成NPC
    /// </summary>
    /// <param name="npc">受击的原NPC</param>
    private static void TrySplit(NPC npc)
    {
        if (!hasSplit[npc.whoAmI] && npc.life <= npc.lifeMax / 2)
        {
            hasSplit[npc.whoAmI] = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SplitNPC(npc);
            }
        }
    }

    /// <summary>
    /// 生成分裂出来的克隆小NPC
    /// </summary>
    /// <param name="original">原始父NPC</param>
    private static void SplitNPC(NPC original)
    {
        // ai3=1 标记这是分裂生成的NPC
        int newNPC = NPC.NewNPC(original.GetSource_FromAI(), (int)original.Center.X, (int)original.Center.Y, original.type, ai3: 1);
        if (newNPC < Main.maxNPCs)
        {
            NPC clone = Main.npc[newNPC];
            // 属性缩放：血量减半，体型0.75倍，伤害0.8倍，防御0.8倍
            clone.lifeMax = original.lifeMax / 2;
            clone.life = clone.lifeMax;
            clone.scale = original.scale * 0.75f;
            clone.width = (int)(original.width * 0.75f);
            clone.height = (int)(original.height * 0.75f);
            clone.damage = (int)(original.damage * 0.8f);
            clone.defense = (int)(original.defense * 0.8f);

            // 生成流血粒子特效
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(original.position, original.width, original.height, DustID.Blood, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f));
            }

            // 服务端向所有客户端同步新NPC
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newNPC);
            }
        }
    }

    /// <summary>
    /// 修改NPC生成速率与最大同时存在NPC数量；同时扩容hasSplit数组适配最大NPC数量
    /// </summary>
    /// <param name="player">触发生成的玩家</param>
    /// <param name="spawnRate">生成速率</param>
    /// <param name="maxSpawns">地图最大同时存在敌怪数</param>
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        // 如果开启NPC分裂，数组长度不足则扩容复制旧标记
        if (NPCConfigs.Instance.NPCSplit)
        {
            if (hasSplit.Length < Main.maxNPCs)
            {
                bool[] newArray = new bool[Main.maxNPCs];
                for (int i = 0; i < hasSplit.Length; i++)
                {
                    newArray[i] = hasSplit[i];
                }
                hasSplit = newArray;
            }
        }

        // 根据在线玩家数量放大生成速率与最大敌怪上限
        if (MultiplayerModeConfigs.Instance.SpawnRate)
        {
            spawnRate *= Main.CurrentFrameFlags.ActivePlayersCount;
            maxSpawns *= Main.CurrentFrameFlags.ActivePlayersCount;
        }
    }

    /// <summary>
    /// NPC血条绘制钩子，开启配置后隐藏血条
    /// </summary>
    /// <returns>false = 不绘制血条</returns>
    public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
    {
        if (NPCConfigs.Instance.HideHealthBar)
        {
            return false;
        }
        return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
    }

    /// <summary>
    /// NPC即将死亡前置钩子，处理掉金钱、随机取消掉落
    /// </summary>
    /// <returns>true正常执行死亡；false阻止NPC死亡</returns>
    public override bool PreKill(NPC npc)
    {
        // 清除NPC击杀金钱奖励
        if (NPCConfigs.Instance.NPCDropsMoney)
        {
            npc.value = 0f;
        }

        // 50%概率阻止NPC死亡（也就不会执行掉落）
        if (NPCConfigs.Instance.NPCNoDrop)
        {
            return !Main.rand.NextBool(2);
        }

        return base.PreKill(npc);
    }

    /// <summary>
    /// 多人难度缩放，自定义放大NPC生命、防御、伤害
    /// </summary>
    /// <param name="numPlayers">玩家数量</param>
    /// <param name="balance">原版平衡系数</param>
    /// <param name="bossAdjustment">Boss调整系数</param>
    public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)
    {
        if (MultiplayerModeConfigs.Instance.DamageMax)
        {
            npc.damage *= (int)(numPlayers * 2 * bossAdjustment);
        }
        if (MultiplayerModeConfigs.Instance.DefenseMax)
        {
            npc.defense *= (int)(numPlayers * 2 * bossAdjustment);
        }
        if (MultiplayerModeConfigs.Instance.LifeMax)
        {
            npc.lifeMax *= (int)(numPlayers * 2 * bossAdjustment);
        }
    }

    /// <summary>
    /// 打乱NPC对话交互按钮顺序（反射修改私有字段）
    /// </summary>
    /// <param name="npc">对话NPC</param>
    /// <param name="interactions">交互按钮列表</param>
    public override void RegisterChatButtons(NPC npc, NPCInteractionList interactions)
    {
        if (NPCConfigs.Instance.RandomizeChatButtons)
        {
            var random = new Random();
            var entries = new List<NPCInteractionList.Entry>(interactions.Entries);

            // Fisher‑Yates 洗牌算法打乱列表
            for (int i = entries.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (entries[i], entries[j]) = (entries[j], entries[i]);
            }

            // 通过反射写入私有 _entries 字段
            var fieldInfo = typeof(NPCInteractionList).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(interactions, entries);
            }
        }
    }
}
