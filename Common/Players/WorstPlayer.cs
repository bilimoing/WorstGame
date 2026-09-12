using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using WorstGame.Common.Configs;
using WorstGame.Common.Helpers;
using WorstGame.Common.Systems;

namespace WorstGame.Common.Players;

/// <summary>
/// 玩家全局ModPlayer，存储玩家专属状态，实现各类恶搞/负面玩家逻辑
/// </summary>
public class WorstPlayer : ModPlayer
{
    /// <summary>
    /// 背包物品乱序切换槽位索引
    /// </summary>
    public int slot = 1;

    /// <summary>
    /// 死亡提示本地化文本
    /// </summary>
    public LocalizedText DeathMessage;

    /// <summary>
    /// 使用武器时自伤概率常量 5%
    /// </summary>
    public const float SelfHarmChance = 0.05f;

    /// <summary>
    /// 本帧是否使用过回传类物品（魔镜/回忆药水等）
    /// </summary>
    public bool usedRecallItem;

    /// <summary>
    /// 本次失败药水使用对应的Buff ID，供Player.AddBuff钩子拦截
    /// </summary>
    public int FailedPotionBuff = -1;

    /// <summary>
    /// 物品使用前置钩子，检测是否使用回传道具
    /// </summary>
    /// <param name="item">尝试使用的物品</param>
    /// <returns>base走原版使用判定</returns>
    public override bool CanUseItem(Item item)
    {
        bool canUse = base.CanUseItem(item);
        if (!canUse)
        {
            return false;
        }

        if (ProjectileConfigs.Instance.MinionsDisappear)
        {
            // 如果是回传类物品，标记标志位，后续PostUpdate清除召唤物
            if (WorstHelper.IsRecallItem(item))
            {
                usedRecallItem = true;
            }
        }

        return true;
    }

    /// <summary>
    /// 输入触发器钩子，按键触发执行逻辑
    /// </summary>
    /// <param name="triggersSet">输入触发器集合</param>
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        // 背包物品随机打乱、交换行逻辑
        if (ItemConfigs.Instance.Itemmovement)
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                slot++;
                if (slot > 4)
                {
                    slot = 1;
                }
                // 交换第一行与slot对应背包行
                for (int i = 0; i < 10; i++)
                {
                    (Player.inventory[i], Player.inventory[slot * 10 + i]) = (Player.inventory[slot * 10 + i], Player.inventory[i]);
                    // 服务端同步装备物品
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, i, Main.LocalPlayer.inventory[i].prefix);
                        NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, slot * 10 + i, Main.LocalPlayer.inventory[slot * 10 + i].prefix);
                    }
                }
                // 再随机打乱前10格物品位置
                for (int i = 0; i < 10; i++)
                {
                    int pos = Main.rand.Next(10, 50);
                    (Player.inventory[i], Player.inventory[pos]) = (Player.inventory[pos], Player.inventory[i]);
                }
            }
        }

        // 右键时有15%概率强制开启智能光标
        if (PlayerConfigs.Instance.SmartCursor)
        {
            if (Main.mouseRight)
            {
                if (Main.rand.NextFloat() < 0.15f)
                {
                    Player.controlSmart = true;
                }
            }
        }
    }

    /// <summary>
    /// 玩家卖出物品之后触发，动态售价系统：统计物品卖出总堆叠数
    /// </summary>
    /// <param name="vendor">商店NPC</param>
    /// <param name="shopInventory">商店物品数组</param>
    /// <param name="item">被卖出的物品</param>
    public override void PostSellItem(NPC vendor, Item[] shopInventory, Item item)
    {
        if (ItemConfigs.Instance.DynamicPricing)
        {
            // 如果还没有缓存该物品原始价格，创建临时物品读取原版value
            if (!WorstSystem.OriginalItemValues.ContainsKey(item.type))
            {
                Item tempItem = new Item();
                tempItem.SetDefaults(item.type);
                WorstSystem.OriginalItemValues[item.type] = tempItem.value;
            }

            if (item.value > 0)
            {
                // 累加卖出堆叠数量
                if (WorstSystem.ItemSalesCount.ContainsKey(item.type))
                {
                    WorstSystem.ItemSalesCount[item.type] += item.stack;
                }
                else
                {
                    WorstSystem.ItemSalesCount[item.type] = item.stack;
                }

                string itemName = Lang.GetItemNameValue(item.type);
                int salesCount = WorstSystem.ItemSalesCount[item.type];
                int priceReductionBatches = salesCount / 10;
                float priceReduction = priceReductionBatches * 0.02f;
                float priceReductionPercent = priceReduction * 100f;

                // 每累计卖出10个，在屏幕打印降价百分比提示
                if (salesCount % 10 == 0)
                {
                    Main.NewText($"{itemName}" + Language.GetTextValue("Mods.WorstGame.NewText.Sell") + $"{priceReductionPercent:0}%", Color.Orange);
                }
            }
        }
    }

    /// <summary>
    /// 玩家帧更新后置，每帧执行各类状态检测
    /// </summary>
    public override void PostUpdate()
    {
        // 使用武器时有5%概率用自身武器伤害的30%伤害自残
        if (ItemConfigs.Instance.SelfHarmOnWeaponUse)
        {
            if (Player.controlUseItem)
            {
                float chance = Main.rand.NextFloat();
                if (chance <= SelfHarmChance)
                {
                    int damage = (int)(Player.HeldItem.damage * 0.3f);
                    Player.Hurt(PlayerDeathReason.ByCustomReason(DeathMessage.ToNetworkText(Player.name)), damage, 0);
                }
            }
        }

        // 使用回传道具之后清除全部召唤物
        if (ProjectileConfigs.Instance.MinionsDisappear)
        {
            if (usedRecallItem)
            {
                usedRecallItem = false;
                WorstHelper.DespawnMinionsOnRecall();
            }
        }

        // 睡觉检测：床附近存在光源方块则强制起床
        if (PlayerConfigs.Instance.HighLightSleep)
        {
            if (Player.sleeping.isSleeping)
            {
                Point bedTilePos = Player.Bottom.ToTileCoordinates();
                if (WorstHelper.HasLightSourcesNearBed(bedTilePos.X, bedTilePos.Y))
                {
                    Player.sleeping.StopSleeping(Player);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Main.NewText(Language.GetTextValue("Mods.WorstGame.NewText.Sleep"), Color.Orange);
                    }
                }
            }
        }

        // 睡觉检测：床周围其他玩家+城镇NPC>=3，禁止睡觉
        if (PlayerConfigs.Instance.MorePeopleDontSleep)
        {
            if (Player.sleeping.isSleeping)
            {
                Point bedTilePos = Player.Bottom.ToTileCoordinates();
                int entityCount = WorstHelper.CountEntitiesNearBed(bedTilePos.X, bedTilePos.Y);
                if (entityCount >= 3)
                {
                    Player.sleeping.StopSleeping(Player);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Main.NewText(Language.GetTextValue("Mods.WorstGame.NewText.Sleep1"), Color.Orange);
                    }
                }
            }
        }

        // 睡觉检测：床附近自己召唤物>=3，强制起床
        if (PlayerConfigs.Instance.ToManyMinionDontSleep)
        {
            if (Player.sleeping.isSleeping)
            {
                Point bedTilePos = Player.Bottom.ToTileCoordinates();
                int minionCount = WorstHelper.CountMinionsNearBed(bedTilePos.X, bedTilePos.Y);
                if (minionCount >= 3)
                {
                    Player.sleeping.StopSleeping(Player);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Main.NewText(Language.GetTextValue("Mods.WorstGame.NewText.Sleep2"), Color.Orange);
                    }
                }
            }
        }

        // 睡觉检测：床附近敌对怪物>=3，强制起床
        if (PlayerConfigs.Instance.ToManyMonstersDontSleep)
        {
            if (Player.sleeping.isSleeping)
            {
                Point bedTilePos = Player.Bottom.ToTileCoordinates();
                int entityCount = WorstHelper.CountEntitiesNearBed1(bedTilePos.X, bedTilePos.Y);
                if (entityCount >= 3)
                {
                    Player.sleeping.StopSleeping(Player);
                    if (Main.myPlayer == Player.whoAmI)
                    {
                        Main.NewText(Language.GetTextValue("Mods.WorstGame.NewText.Sleep3"), Color.Orange);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Buff更新后置，实现多人Buff共享逻辑
    /// </summary>
    public override void PostUpdateBuffs()
    {
        if (MultiplayerModeConfigs.Instance.SharedBuff)
        {
            // 将自己身上的buff复制给其他所有活跃玩家
            for (int i = 0; i < Main.CurrentFrameFlags.ActivePlayersCount; i++)
            {
                for (int j = 0; j < BuffLoader.BuffCount; j++)
                {
                    if (Player.HasBuff(j) && !Main.player[i].HasBuff(j))
                    {
                        Main.player[i].AddBuff(j, 300);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 每帧重置效果，在这里修改属性会每帧覆盖玩家属性
    /// </summary>
    public override void ResetEffects()
    {
        // 防御减半
        if (PlayerConfigs.Instance.Defensehalved)
        {
            Player.statDefense /= 2;
        }
        // 生命上限、魔力上限减半
        if (PlayerConfigs.Instance.HalvesMPHP)
        {
            Player.statManaMax2 /= 2;
            Player.statLifeMax2 /= 2;
        }
        // 禁止生命再生
        if (PlayerConfigs.Instance.NoRegen)
        {
            Player.lifeRegen = 0;
            Player.lifeRegenTime = 0;
        }
    }

    /// <summary>
    /// 钓鱼捕获物品钩子，50%概率直接丢失任务鱼并刷新渔夫任务
    /// </summary>
    /// <param name="attempt">钓鱼尝试信息</param>
    /// <param name="itemDrop">产出物品ID</param>
    /// <param name="npcSpawn">产出NPCID</param>
    /// <param name="sonar">声呐球弹窗</param>
    /// <param name="sonarPosition">声呐球位置</param>
    public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
    {
        if (PlayerConfigs.Instance.QuestFishDisappeared)
        {
            // 判断钓到的是当前渔夫任务鱼
            if (itemDrop == Main.anglerQuestItemNetIDs[Main.anglerQuest])
            {
                if (Main.rand.NextFloat() < 0.5f)
                {
                    itemDrop = ItemID.None;
                    Main.AnglerQuestSwap();
                    Main.NewText(Language.GetTextValue("Mods.WorstGame.NewText.Fish"), 255, 100, 100);
                }
            }
        }
    }

    /// <summary>
    /// 玩家重生时触发，设置重生后血量为1
    /// </summary>
    public override void OnRespawn()
    {
        if (PlayerConfigs.Instance.InitialHP)
        {
            Player.statLife = 1;
        }
    }

    /// <summary>
    /// 玩家PreUpdate，更新前执行
    /// </summary>
    public override void PreUpdate()
    {
        // 修改最大水下呼吸时间
        if (PlayerConfigs.Instance.Breathingtime)
        {
            Player.breathMax = 30;
        }
    }

    /// <summary>
    /// 玩家死亡钩子，死亡连锁、修改重生倒计时
    /// </summary>
    /// <param name="damage">死亡伤害</param>
    /// <param name="hitDirection">受击方向</param>
    /// <param name="pvp">是否PVP</param>
    /// <param name="damageSource">死亡原因</param>
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // 重生时间翻倍设置为3600帧（1分钟）
        if (PlayerConfigs.Instance.Respawntimedoubled)
        {
            Player.respawnTimer = 3600;
        }

        // 死亡连锁：一人死亡其他玩家也死亡（当前仅客户端执行）
        if (MultiplayerModeConfigs.Instance.DeathLink)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                foreach (var target in Main.player)
                {
                    if (target.active && !target.dead)
                    {
                        if (target.whoAmI != Player.whoAmI)
                        {
                            if (target.whoAmI == Main.myPlayer)
                            {
                                target.KillMe(PlayerDeathReason.ByCustomReason(DeathMessage.ToNetworkText(Player.name)), 228, 0);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 装备更新后置，修改翅膀最大飞行时间
    /// </summary>
    public override void PostUpdateEquips()
    {
        // 翅膀最大飞行时间减半
        if (PlayerConfigs.Instance.Wingflighttimehalved)
        {
            if (Player.wings > 0)
            {
                Player.wingTimeMax /= 2;
            }
        }
    }
}
