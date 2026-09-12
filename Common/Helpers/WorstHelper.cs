using System.Collections;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using WorstGame.Common.Configs;

namespace WorstGame.Common.Helpers;

/// <summary>
/// 模组通用静态工具帮助类，存放各类复用的判断、计数、工具方法
/// </summary>
public class WorstHelper
{
    /// <summary>
    /// 判断物品是否属于传送回出生点类物品（魔镜、冰镜、手机、回忆药水等）
    /// </summary>
    /// <param name="item">待检测物品</param>
    /// <returns>true：是回传类物品</returns>
    public static bool IsRecallItem(Item item)
    {
        int[] recallItems =
        [
            ItemID.MagicMirror, ItemID.IceMirror, ItemID.CellPhone, ItemID.RecallPotion,
            ItemID.Shellphone, ItemID.ShellphoneSpawn, ItemID.ShellphoneOcean,
            ItemID.ShellphoneHell, ItemID.PotionOfReturn
        ];
        return ((IList)recallItems).Contains(item.type);
    }

    /// <summary>
    /// 获取玩家背包可用行数，随游戏进度减少行数
    /// </summary>
    /// <returns>背包行数</returns>
    public static int GetInventoryRows()
    {
        if (PlayerConfigs.Instance.InventoryRows)
        {
            int rows = 5;
            // 困难模式开启 -1行
            if (Main.hardMode)
            {
                rows--;
            }
            // 击败任意机械Boss -1行
            if (NPC.downedMechBossAny)
            {
                rows--;
            }
            // 击败世纪之花 -1行
            if (NPC.downedPlantBoss)
            {
                rows--;
            }
            // 击败远古教徒 -1行
            if (NPC.downedAncientCultist)
            {
                rows--;
            }
            return rows;
        }
        return 5;
    }

    /// <summary>
    /// 统计床周围320×320范围内，属于本地玩家的召唤物/炮台数量
    /// </summary>
    /// <param name="tileX">床方块X坐标</param>
    /// <param name="tileY">床方块Y坐标</param>
    /// <returns>召唤物、炮台总数量</returns>
    public static int CountMinionsNearBed(int tileX, int tileY)
    {
        Player player = Main.LocalPlayer;
        int count = 0;
        // 床的检测区域：以床方块为中心，向外偏移160像素，总大小320*320
        Rectangle bedArea = new Rectangle(tileX * 16 - 160, tileY * 16 - 160, 320, 320);

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile proj = Main.projectile[i];
            // 弹幕激活、属于本地玩家、是召唤物/炮台
            if (proj.active && proj.owner == player.whoAmI && (proj.minion || proj.sentry || proj.minionSlots > 0))
            {
                if (bedArea.Intersects(proj.getRect()))
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 统计床周围320×320范围内敌对NPC怪物数量
    /// </summary>
    /// <param name="tileX">床方块X坐标</param>
    /// <param name="tileY">床方块Y坐标</param>
    /// <returns>附近敌对怪物数量</returns>
    public static int CountEntitiesNearBed1(int tileX, int tileY)
    {
        int count = 0;
        Rectangle bedArea = new Rectangle(tileX * 16 - 160, tileY * 16 - 160, 320, 320);

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            // NPC激活、非友好（怪物）
            if (npc.active && !npc.friendly)
            {
                if (bedArea.Intersects(npc.getRect()))
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 统计床周围：其他在线玩家 + 城镇NPC的总数量
    /// </summary>
    /// <param name="tileX">床方块X坐标</param>
    /// <param name="tileY">床方块Y坐标</param>
    /// <returns>其他玩家+城镇NPC总数</returns>
    public static int CountEntitiesNearBed(int tileX, int tileY)
    {
        Player player = Main.LocalPlayer;
        int count = 0;
        Rectangle bedArea = new Rectangle(tileX * 16 - 160, tileY * 16 - 160, 320, 320);

        // 统计其他存活玩家
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != player.whoAmI && Main.player[i].active && !Main.player[i].dead)
            {
                if (bedArea.Intersects(Main.player[i].getRect()))
                {
                    count++;
                }
            }
        }

        // 统计城镇NPC（NPC村民）
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.townNPC)
            {
                if (bedArea.Intersects(npc.getRect()))
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 检测床方块周围7×7方块范围内是否存在光源方块
    /// </summary>
    /// <param name="tileX">床方块X</param>
    /// <param name="tileY">床方块Y</param>
    /// <returns>true：附近存在光源方块</returns>
    public static bool HasLightSourcesNearBed(int tileX, int tileY)
    {
        // 遍历以床为中心，左右上下各3格，7×7方块范围
        for (int x = tileX - 3; x <= tileX + 3; x++)
        {
            for (int y = tileY - 3; y <= tileY + 3; y++)
            {
                if (WorldGen.InWorld(x, y))
                {
                    Tile tile = Main.tile[x, y];
                    if (tile != null && tile.HasTile)
                    {
                        if (IsLightSourceTile(tile.TileType))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 判断方块类型是否属于光源方块（火把、篝火、灯、烛台、水瓶萤火虫等）
    /// </summary>
    /// <param name="tileType">方块ID</param>
    /// <returns>true：是光源方块</returns>
    public static bool IsLightSourceTile(int tileType)
    {
        return tileType is TileID.Torches or TileID.Campfire or TileID.Candelabras
            or TileID.Lamps or TileID.Chandeliers or TileID.Candles
            or TileID.LavaMoss or TileID.WaterCandle or TileID.PeaceCandle
            or TileID.ShimmerflyinaBottle or TileID.FireflyinaBottle;
    }

    /// <summary>
    /// 判断弹幕是否为玩家召唤物 / 炮台
    /// </summary>
    /// <param name="proj">弹幕实例</param>
    /// <returns>true：召唤物/炮台</returns>
    public static bool IsSummonProjectile(Projectile proj)
    {
        return proj.minion || proj.sentry || proj.minionSlots > 0;
    }

    /// <summary>
    /// 使用回传物品时，清除本地玩家全部召唤物、炮台；客户端弹出提示文字+音效
    /// </summary>
    public static void DespawnMinionsOnRecall()
    {
        Player player = Main.LocalPlayer;
        int despawnCount = 0;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile proj = Main.projectile[i];
            if (proj.active && proj.owner == player.whoAmI && IsSummonProjectile(proj))
            {
                proj.Kill();
                despawnCount++;
            }
        }

        // 有被清除的召唤物，并且不是服务端，输出本地化提示文本并播放音效
        if (despawnCount > 0 && Main.netMode != NetmodeID.Server)
        {
            Main.NewText(
                Language.GetTextValue("Mods.WorstGame.NewText.Minion1") + $"{despawnCount}" + Language.GetTextValue("Mods.WorstGame.NewText.Minion2"),
                Color.LightBlue);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.2f }, player.Center);
        }
    }
}
