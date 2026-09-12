using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using WorstGame.Common.Configs;
using WorstGame.Common.Systems;
using WorstGame.Content.Prefixes;

namespace WorstGame.Common.GlobalItems;

/// <summary>
/// 全局物品，对全部物品生效，由模组配置开关控制各类负面/恶搞效果
/// </summary>
public class WorstGlobalItem : GlobalItem
{
    /// <summary>
    /// 开启实例按实体独立，每个物品实例拥有该GlobalItem独立数据
    /// </summary>
    public override bool InstancePerEntity => true;

    /// <summary>
    /// 克隆新物品实例时复制当前GlobalItem状态
    /// </summary>
    protected override bool CloneNewInstances => true;

    /// <summary>
    /// 提示文字单个字符最小缩放
    /// </summary>
    public const float MinScale = 0.5f;
    /// <summary>
    /// 提示文字单个字符最大缩放
    /// </summary>
    public const float MaxScale = 1.8f;

    /// <summary>
    /// 物品初始化，应用各类配置带来的物品属性修改
    /// </summary>
    /// <param name="item">正在初始化的物品实例</param>
    public override void SetDefaults(Item item)
    {
        // 关闭物品自动挥舞
        if (ItemConfigs.Instance.AutoReuse)
        {
            item.autoReuse = false;
        }

        // 鱼饵力减半
        if (ItemConfigs.Instance.BaitPowerHalved)
        {
            item.bait /= 2;
        }

        // 设置暴击为极大负数，实现完全无暴击效果
        if (ItemConfigs.Instance.CritChance)
        {
            item.crit = -114514;
        }

        // 将武器伤害类型强制改为默认伤害类
        if (ItemConfigs.Instance.DamageDefault)
        {
            item.DamageType = DamageClass.Default;
        }

        // 魔力消耗翻倍
        if (ItemConfigs.Instance.DoubleMP)
        {
            item.mana *= 2;
        }

        // 钓竿渔力减半
        if (ItemConfigs.Instance.FishingPowerHalved)
        {
            item.fishingPole /= 2;
        }

        // 物品最大堆叠强制设置为1，不可堆叠
        if (ItemConfigs.Instance.MaxStack)
        {
            item.maxStack = 1;
        }

        // 随机增大研究解锁所需物品数量
        if (ItemConfigs.Instance.RandomResearch)
        {
            // 仅处理原版物品，玩家背包不存在该物品才生效
            if (item.ResearchUnlockCount > 0 && item.type < ItemID.Count && !Main.LocalPlayer.HasItem(item.type))
            {
                // 1~9999随机倍率，成倍放大研究需求数量
                int multiplier = Main.rand.Next(1, 10000);
                item.ResearchUnlockCount *= multiplier;
            }
        }
    }

    /// <summary>
    /// 静态初始化，只执行一次，用于缓存原版物品原始价格，给动态售价功能使用
    /// </summary>
    public override void SetStaticDefaults()
    {
        if (ItemConfigs.Instance.DynamicPricing)
        {
            // 遍历全部物品ID，把原版物品的原始售价存入字典缓存
            for (int i = 1; i < ItemLoader.ItemCount; i++)
            {
                // 尚未缓存过该物品时才读取
                if (!WorstSystem.OriginalItemValues.ContainsKey(i))
                {
                    Item tempItem = new Item();
                    tempItem.SetDefaults(i);
                    // 只记录有价值的物品
                    if (tempItem.value > 0)
                    {
                        WorstSystem.OriginalItemValues[i] = tempItem.value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 物品获取词缀钩子，开启WorstPrefix时强制分配指定负面词缀
    /// </summary>
    /// <param name="item">待获取词缀的物品</param>
    /// <param name="rand">随机数生成器</param>
    /// <returns>词缀Type，返回base使用原版逻辑</returns>
    public override int ChoosePrefix(Item item, UnifiedRandom rand)
    {
        if (ItemConfigs.Instance.WorstPrefix)
        {
            // 近战武器 → Dreadful 可怕的
            if (item.DamageType == DamageClass.Melee || item.DamageType == DamageClass.MeleeNoSpeed)
            {
                return ModContent.PrefixType<Dreadful>();
            }
            // 远程武器 → Broken 破损的
            if (item.DamageType == DamageClass.Ranged)
            {
                return ModContent.PrefixType<Broken>();
            }
            // 魔法武器 → Futile 徒劳的
            if (item.DamageType == DamageClass.Magic)
            {
                return ModContent.PrefixType<Futile>();
            }
        }
        return base.ChoosePrefix(item, rand);
    }

    /// <summary>
    /// 物品处于玩家背包内每帧更新，动态修改物品售价（卖的越多价格越低，最低降至原价50%）
    /// </summary>
    /// <param name="item">背包内物品</param>
    /// <param name="player">持有该物品的玩家</param>
    public override void UpdateInventory(Item item, Player player)
    {
        if (ItemConfigs.Instance.DynamicPricing)
        {
            // 读取该物品累计销售次数、原始价格
            if (item.value > 0 && WorstSystem.ItemSalesCount.TryGetValue(item.type, out int salesCount) && WorstSystem.OriginalItemValues.TryGetValue(item.type, out int originalValue))
            {
                // 每卖出10件，价格衰减2%
                float priceReduction = salesCount / 10 * 0.02f;
                // 价格下限不低于原价的50%
                float priceMultiplier = Math.Max(1f - priceReduction, 0.5f);
                item.value = (int)(originalValue * priceMultiplier);
            }
        }
    }

    /// <summary>
    /// 绘制物品提示行前置钩子，ChaosTooltip开启时每个字符随机缩放，手写绘制混乱Tooltip
    /// </summary>
    /// <param name="item">目标物品</param>
    /// <param name="line">当前待绘制的提示行</param>
    /// <param name="yOffset">Y轴偏移</param>
    /// <returns>false代表接管本次绘制，不再走原版绘制逻辑</returns>
    public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
    {
        if (ItemConfigs.Instance.ChaosTooltip)
        {
            Vector2 position = new Vector2(line.X, line.Y);
            float maxHeight = 0f;
            // 逐个字符遍历绘制
            foreach (var t in line.Text)
            {
                // 每个字符独立随机缩放
                line.BaseScale = new Vector2(Main.rand.NextFloat(MinScale, MaxScale), Main.rand.NextFloat(MinScale, MaxScale));
                string charStr = t.ToString();
                Vector2 charSize = line.Font.MeasureString(charStr) * line.BaseScale;
                // 带黑色描边绘制单个字符，字符居中
                Utils.DrawBorderStringFourWay(Main.spriteBatch, line.Font, charStr, position.X + charSize.X / 2, position.Y + charSize.Y / 2, line.Color, Color.Black, new Vector2(0.3f));
                // X坐标偏移，绘制下一个字符
                position.X += charSize.X;
                maxHeight = Math.Max(maxHeight, charSize.Y);
            }
            // 阻止原版Tooltip绘制
            return false;
        }
        return base.PreDrawTooltipLine(item, line, ref yOffset);
    }

    /// <summary>
    /// 物品被生成/创建时触发，应用动态售价
    /// </summary>
    /// <param name="item">新生成的物品</param>
    /// <param name="context">物品生成上下文信息</param>
    public override void OnCreated(Item item, ItemCreationContext context)
    {
        if (ItemConfigs.Instance.DynamicPricing)
        {
            if (item.value > 0 && WorstSystem.ItemSalesCount.TryGetValue(item.type, out int salesCount) && WorstSystem.OriginalItemValues.TryGetValue(item.type, out int originalValue))
            {
                float priceReduction = salesCount / 10 * 0.02f;
                float priceMultiplier = Math.Max(1f - priceReduction, 0.5f);
                item.value = (int)(originalValue * priceMultiplier);
            }
        }
    }
}
