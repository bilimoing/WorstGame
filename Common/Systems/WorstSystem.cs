using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using WorstGame.Common.Configs;
using WorstGame.Common.Helpers;
using WorstGame.Common.Players;

namespace WorstGame.Common.Systems;

/// <summary>
/// 模组全局System，存放MonoMod IL/On钩子、世界数据存储、世界生成修改、各类全局逻辑
/// </summary>
public class WorstSystem : ModSystem
{
    /// <summary>
    /// 物品销售计数字典：key=物品ID，value=累计卖出堆叠总数，用于动态降价系统
    /// </summary>
    public static Dictionary<int, int> ItemSalesCount = [];

    /// <summary>
    /// 缓存原版物品基础售价，避免运行时反复new Item读取默认价格
    /// </summary>
    public static Dictionary<int, int> OriginalItemValues = [];

    /// <summary>
    /// 模组加载：注册全部MonoMod IL钩子与On钩子
    /// </summary>
    public override void Load()
    {
        IL_Main.DrawInventory += ModifyDrawInventory;
        IL_WorldGen.SpawnStormLightning += ModifySpawnStormLightning;

        On_Player.AddBuff += PlayerOnAddBuff;
        On_Player.QuickHeal += PlayerOnQuickHeal;
        On_Player.IsAmmoFreeThisShot += PlayerOnIsAmmoFreeThisShot;
        On_Player.CheckIceBreak += On_PlayerOnCheckIceBreak;

        On_Projectile.AI_007_GrapplingHooks_CanTileBeLatchedOnTo += ModifyGrappleCheck;
        On_Projectile.AI_007_GrapplingHooks += ModifyGrappleAI;

        On_DoorOpeningHelper.Update += On_DoorOpeningHelper_Update;

        On_WorldGen.ShakeTree += ShakeTree_OnExecute;

        On_Main.DrawInterface_35_YouDied += Main_DrawInterface_35_YouDied;
        On_Main.DoUpdate_Enter_ToggleChat += Main_DoUpdate_Enter_ToggleChat;

        On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += ExtendAllGoreLifetime;
    }

    /// <summary>
    /// 玩家坠落撞击冰/平台钩子；高速下落会打碎冰与平台
    /// </summary>
    /// <param name="orig">原方法</param>
    /// <param name="self">目标玩家</param>
    private void On_PlayerOnCheckIceBreak(On_Player.orig_CheckIceBreak orig, Player self)
    {
        orig(self);
        if (TileConfigs.Instance.PlatformBreakEasy)
        {
            // Y向速度大于7，判定为高速坠落
            if (self.velocity.Y > 7f)
            {
                Vector2 vector = self.position + self.velocity;
                int num = (int)(vector.X / 16f);
                int num2 = (int)((vector.X + self.width) / 16f);
                int num3 = (int)((self.position.Y + self.height + 1f) / 16f);

                // 遍历玩家底部范围内方块
                for (int i = num; i <= num2; i++)
                {
                    for (int j = num3; j <= num3 + 1 && Main.tile[i, j] != null; j++)
                    {
                        // 可破坏冰块
                        if (Main.tile[i, j].HasUnactuatedTile && Main.tile[i, j].TileType == TileID.BreakableIce && !WorldGen.SolidTile(i, j - 1))
                        {
                            WorldGen.KillTile(i, j);
                            // 客户端发送方块修改网络包
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j);
                            }
                        }

                        // 平台方块，打碎不掉落物品
                        if (Main.tile[i, j].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[i, j].TileType])
                        {
                            WorldGen.KillTile(i, j, noItem: true);
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 包裹快速治疗流程。QuickHeal 会直接调用 AddBuff，因此在原版流程前记录药水上下文。
    /// </summary>
    private static void PlayerOnQuickHeal(On_Player.orig_QuickHeal orig, Player self)
    {
        WorstPlayer worstPlayer = self.GetModPlayer<WorstPlayer>();
        Item item = self.QuickHeal_GetItemToUse();

        if (IsPotionBuffItem(item) && Main.rand.NextFloat() < 0.5f)
        {
            worstPlayer.FailedPotionBuff = item.buffType;
        }

        try
        {
            orig(self);
        }
        finally
        {
            worstPlayer.FailedPotionBuff = -1;
        }
    }

    /// <summary>
    /// 拦截失败药水的 Buff。只拦截 QuickHeal 设置的本次失败上下文，其他来源的同名 Buff 不受影响。
    /// </summary>
    private static void PlayerOnAddBuff(On_Player.orig_AddBuff orig, Player self, int type, int time, bool fromNetPvP = false)
    {
        WorstPlayer worstPlayer = self.GetModPlayer<WorstPlayer>();
        if (ItemConfigs.Instance.PotionFail && worstPlayer.FailedPotionBuff == type)
        {
            worstPlayer.FailedPotionBuff = -1;
            return;
        }

        orig(self, type, time, fromNetPvP);
    }

    private static bool IsPotionBuffItem(Item item)
    {
        return ItemConfigs.Instance.PotionFail && item is { IsAir: false, consumable: true, potion: true, buffType: > 0 };
    }

    /// <summary>
    /// 修改弹药消耗判定：AlwaysConsumeAmmo开启时永远返回false，强制消耗弹药
    /// </summary>
    private bool PlayerOnIsAmmoFreeThisShot(On_Player.orig_IsAmmoFreeThisShot orig, Player self, Item weapon, Item ammo, int projToShoot)
    {
        if (ItemConfigs.Instance.AlwaysConsumeAmmo)
        {
            return false;
        }
        return orig(self, weapon, ammo, projToShoot);
    }

    /// <summary>
    /// IL钩子：修改闪电生成逻辑，非风暴下雨也生成闪电，降低闪电生成间隔
    /// </summary>
    private void ModifySpawnStormLightning(ILContext il)
    {
        if (MapConfigs.Instance.LingningChange)
        {
            var cursor = new ILCursor(il);

            // 将 Main.IsItStorming 判断替换为 Main.raining，下雨即可触发闪电
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchCall<Main>("get_IsItStorming")))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, il.Import(typeof(Main).GetField("raining", BindingFlags.Public | BindingFlags.Static)));
            }

            cursor.Index = 0;
            // 将闪电间隔常量150替换为25，提高闪电频率
            if (cursor.TryGotoNext(MoveType.After, x => x.OpCode == OpCodes.Ldc_I4 && x.Operand is 150))
            {
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4, 25);
            }
        }
    }

    /// <summary>
    /// IL钩子：修改背包绘制行数，调用WorstHelper.GetInventoryRows动态获取行数
    /// 替换原版写死的常量5
    /// </summary>
    public void ModifyDrawInventory(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        // 匹配加载常量5的指令（原版背包行数）
        if (cursor.TryGotoNext(instr => instr.OpCode == OpCodes.Ldc_I4_5))
        {
            cursor.Remove();
            MethodInfo method = typeof(WorstHelper).GetMethod(nameof(WorstHelper.GetInventoryRows),
                BindingFlags.Public | BindingFlags.Static);
            if (method != null)
            {
                cursor.Emit(OpCodes.Call, il.Import(method));
            }
        }
    }

    /// <summary>
    /// 钩子：按下回车打开聊天；开启配置强制关闭玩家聊天框
    /// </summary>
    private static void Main_DoUpdate_Enter_ToggleChat(On_Main.orig_DoUpdate_Enter_ToggleChat orig)
    {
        if (MultiplayerModeConfigs.Instance.ClosePlayerChat)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                Main.chatRelease = false;
                Main.ClosePlayerChat();
            }
        }
        else
        {
            orig();
        }
    }

    /// <summary>
    /// 拦截接收网络包，RandomPacketSending开启时随机修改接收方playerNumber
    /// </summary>
    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (MultiplayerModeConfigs.Instance.RandomPacketSending)
        {
            playerNumber = Main.rand.Next(Main.CurrentFrameFlags.ActivePlayersCount);
        }
        return base.HijackGetData(ref messageType, ref reader, playerNumber);
    }

    /// <summary>
    /// 拦截发送网络包，RandomPacketSending开启时随机修改发送源whoAmI
    /// </summary>
    public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (MultiplayerModeConfigs.Instance.RandomPacketSending)
        {
            whoAmI = Main.rand.Next(Main.CurrentFrameFlags.ActivePlayersCount);
        }
        return base.HijackSendData(whoAmI, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
    }

    /// <summary>
    /// 钩爪判定钩子：PlatformGrappling开启，钩爪不可以勾住平台方块
    /// </summary>
    public static bool ModifyGrappleCheck(On_Projectile.orig_AI_007_GrapplingHooks_CanTileBeLatchedOnTo orig, Projectile self, int x, int y)
    {
        if (ItemConfigs.Instance.PlatformGrappling)
        {
            if (TileID.Sets.Platforms[Main.tile[x, y].TileType])
            {
                return false;
            }
        }
        return orig(self, x, y);
    }

    /// <summary>
    /// 钩爪AI钩子：碰到平台时反弹，改变速度，延长存活时间
    /// </summary>
    public static void ModifyGrappleAI(On_Projectile.orig_AI_007_GrapplingHooks orig, Projectile self)
    {
        if (ItemConfigs.Instance.PlatformGrappling)
        {
            float oldLocalAI0 = self.localAI[0];
            // ⚠️注意：oldLocalAI0 ==0 判断，后面又判断self.localAI[0]==2，逻辑有问题
            if (oldLocalAI0 == 0f && self.localAI[0] == 2f)
            {
                Point tilePos = self.Center.ToTileCoordinates();
                if (WorldGen.InWorld(tilePos.X, tilePos.Y))
                {
                    Tile tile = Main.tile[tilePos.X, tilePos.Y];
                    if (tile.HasTile && TileID.Sets.Platforms[tile.TileType])
                    {
                        self.ai[0] = 0f;
                        self.localAI[0] = 0f;
                        self.velocity = self.velocity.RotatedByRandom(0.2f);
                        self.timeLeft += 15;
                    }
                }
            }
        }
        orig(self);
    }

    /// <summary>
    /// 门更新钩子：AutoOpenDoor开启时强制调用关门逻辑
    /// </summary>
    public static void On_DoorOpeningHelper_Update(On_DoorOpeningHelper.orig_Update orig, DoorOpeningHelper self, Player player)
    {
        if (TileConfigs.Instance.AutoOpenDoor)
        {
            self.LookForDoorsToClose(player);
        }
        else
        {
            orig(self, player);
        }
    }

    /// <summary>
    /// 摇树钩子：MaxTreeShakes开启，全局最大摇树次数临时设置为1
    /// </summary>
    public static void ShakeTree_OnExecute(On_WorldGen.orig_ShakeTree orig, int i, int j)
    {
        Type worldGenType = typeof(WorldGen);
        FieldInfo maxTreeShakesField = worldGenType.GetField("maxTreeShakes", BindingFlags.Static | BindingFlags.NonPublic);
        int originalValue = (int)maxTreeShakesField.GetValue(null);

        if (TileConfigs.Instance.MaxTreeShakes)
        {
            maxTreeShakesField.SetValue(null, 1);
        }

        orig(i, j);
        // 恢复原始静态字段，避免永久修改原版全局状态
        maxTreeShakesField.SetValue(null, originalValue);
    }

    /// <summary>
    /// 保存世界数据：把动态售价的销售计数、原始价格存入TagCompound世界存档
    /// </summary>
    public override void SaveWorldData(TagCompound tag)
    {
        if (ItemConfigs.Instance.DynamicPricing)
        {
            var salesData = new List<int>();
            foreach (var kvp in ItemSalesCount)
            {
                salesData.Add(kvp.Key);
                salesData.Add(kvp.Value);
            }
            tag["ItemSalesCount"] = salesData;

            var valueData = new List<int>();
            foreach (var kvp in OriginalItemValues)
            {
                valueData.Add(kvp.Key);
                valueData.Add(kvp.Value);
            }
            tag["OriginalItemValues"] = valueData;
        }
    }

    /// <summary>
    /// 加载世界存档：读取物品销售计数、原版价格字典
    /// </summary>
    public override void LoadWorldData(TagCompound tag)
    {
        if (ItemConfigs.Instance.DynamicPricing)
        {
            ItemSalesCount.Clear();
            OriginalItemValues.Clear();

            if (tag.ContainsKey("ItemSalesCount"))
            {
                var salesData = tag.Get<List<int>>("ItemSalesCount");
                for (int i = 0; i < salesData.Count; i += 2)
                {
                    ItemSalesCount[salesData[i]] = salesData[i + 1];
                }
            }

            if (tag.ContainsKey("OriginalItemValues"))
            {
                var valueData = tag.Get<List<int>>("OriginalItemValues");
                for (int i = 0; i < valueData.Count; i += 2)
                {
                    OriginalItemValues[valueData[i]] = valueData[i + 1];
                }
            }
        }
    }

    /// <summary>
    /// 每18000游戏帧（5分钟）衰减物品销售计数，随时间降价慢慢恢复
    /// </summary>
    public override void PostUpdateEverything()
    {
        if (ItemConfigs.Instance.DynamicPricing)
        {
            // 18000帧 = 5分钟
            if (Main.GameUpdateCount % 18000 == 0)
            {
                foreach (var itemId in ItemSalesCount.Keys)
                {
                    ItemSalesCount[itemId] = (int)(ItemSalesCount[itemId] * 0.9f);
                    if (ItemSalesCount[itemId] < 1)
                    {
                        ItemSalesCount[itemId] = 0;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 死亡界面绘制钩子：DeadTextDeleted开启，强制把死亡文字颜色设置完全透明，达到隐藏死亡文字效果
    /// </summary>
    public static void Main_DrawInterface_35_YouDied(On_Main.orig_DrawInterface_35_YouDied orig)
    {
        if (PlayerConfigs.Instance.DeadTextDeleted)
        {
            Player player = Main.LocalPlayer;
            if (player.dead)
            {
                float num = -60f;
                string value = Lang.inter[38].Value;
                Main.spriteBatch.DrawString(FontAssets.DeathText.Value, value,
                    new Vector2(Main.screenWidth / 2 - FontAssets.DeathText.Value.MeasureString(value).X / 2f, Main.screenHeight / 2 + num),
                    player.GetDeathAlpha(Color.Transparent), 0f, default, 1f, SpriteEffects.None, 0f);

                if (player.lostCoins > 0)
                {
                    num += 50f;
                    string textValue = Language.GetTextValue("Game.DroppedCoins", player.lostCoinString);
                    Main.spriteBatch.DrawString(FontAssets.MouseText.Value, textValue,
                        new Vector2(Main.screenWidth / 2 - FontAssets.MouseText.Value.MeasureString(textValue).X / 2f, Main.screenHeight / 2 + num),
                        player.GetDeathAlpha(Color.Transparent), 0f, default, 1f, SpriteEffects.None, 0f);
                }
            }
        }
        else
        {
            orig();
        }
    }

    /// <summary>
    /// 生成碎块钩子，NPCGore开启时大幅延长碎块存活时间（60*60帧 = 1分钟）
    /// </summary>
    public static int ExtendAllGoreLifetime(On_Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
    {
        int goreIndex = orig(source, Position, Velocity, Type, Scale);
        if (NPCConfigs.Instance.NPCGore)
        {
            if (goreIndex >= 0 && goreIndex < Main.gore.Length)
            {
                Main.gore[goreIndex].timeLeft = 60 * 60;
            }
        }
        return goreIndex;
    }

    /// <summary>
    /// 世界生成完成后：RemoveShimmer开启，把微光液体替换成水，销毁微光方块
    /// </summary>
    public override void PostWorldGen()
    {
        if (MapConfigs.Instance.RemoveShimmer)
        {
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    Tile tile = Main.tile[i, j];
                    // 微光液体替换成水
                    if (tile.LiquidType == LiquidID.Shimmer)
                    {
                        tile.LiquidType = LiquidID.Water;
                    }
                    // 销毁微光方块
                    if (tile is { HasTile: true, TileType: TileID.ShimmerBlock })
                    {
                        WorldGen.KillTile(i, j);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 修改世界生成Pass：移除原版微光生成步骤，插入自定义逻辑修改GenVars.shimmerPosition
    /// </summary>
    public override void ModifyWorldGenTasks(List<GenPass> tasks)
    {
        if (MapConfigs.Instance.RemoveShimmer)
        {
            // 删除原版"Shimmer"生成阶段
            int index = tasks.FindIndex(genPass => genPass.Name.Equals("Shimmer"));
            if (index != -1)
            {
                tasks.RemoveAt(index);
            }

            // 在祭坛生成前插入一个LegacyPass，改写微光全局坐标变量
            index = tasks.FindIndex(genPass => genPass.Name.Equals("Altars"));
            if (index != -1)
            {
                tasks.Insert(index, new PassLegacy("Remove Shimmer", delegate
                {
                    int shimmerPositionYMin = (int)(Main.worldSurface + Main.rockLayer) / 2 + 50;
                    int shimmerPositionYMax = (int)((Main.maxTilesY - 250) * 2 + Main.rockLayer) / 3;

                    if (shimmerPositionYMax > Main.maxTilesY - 460)
                    {
                        shimmerPositionYMax = Main.maxTilesY - 460;
                    }
                    if (shimmerPositionYMax <= shimmerPositionYMin)
                    {
                        shimmerPositionYMax = shimmerPositionYMin + 50;
                    }

                    int shimmerPositionX = Main.dungeonX < Main.maxTilesX / 2
                        ? WorldGen.genRand.Next((int)(Main.maxTilesX * 0.89), Main.maxTilesX - 200)
                        : WorldGen.genRand.Next(200, (int)(Main.maxTilesX * 0.11));

                    int shimmerPositionY = WorldGen.genRand.Next(shimmerPositionYMin, shimmerPositionYMax);
                    GenVars.shimmerPosition = new Vector2D(shimmerPositionX, shimmerPositionY);
                }));
            }
        }
    }

    /// <summary>
    /// 模组卸载，全部MonoMod钩子取消订阅，防止内存泄漏
    /// </summary>
    public override void Unload()
    {
        IL_Main.DrawInventory -= ModifyDrawInventory;
        IL_WorldGen.SpawnStormLightning -= ModifySpawnStormLightning;

        On_Player.AddBuff -= PlayerOnAddBuff;
        On_Player.QuickHeal -= PlayerOnQuickHeal;
        On_Player.IsAmmoFreeThisShot -= PlayerOnIsAmmoFreeThisShot;
        On_Player.CheckIceBreak -= On_PlayerOnCheckIceBreak;

        On_Projectile.AI_007_GrapplingHooks_CanTileBeLatchedOnTo -= ModifyGrappleCheck;
        On_Projectile.AI_007_GrapplingHooks -= ModifyGrappleAI;

        On_DoorOpeningHelper.Update -= On_DoorOpeningHelper_Update;

        On_WorldGen.ShakeTree -= ShakeTree_OnExecute;

        On_Main.DrawInterface_35_YouDied -= Main_DrawInterface_35_YouDied;
        On_Main.DoUpdate_Enter_ToggleChat -= Main_DoUpdate_Enter_ToggleChat;

        On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float -= ExtendAllGoreLifetime;
    }
}