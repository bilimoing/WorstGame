## ItemConfigs 物品配置

- **AutoReuse（禁用自动挥动）**：开启后，物品将不能自动挥动
- **MaxStack（移除最大叠加）**：开启后，物品最大叠加变为 1
- **DamageDefault（默认伤害）**：开启后，物品伤害变为默认值
- **DoubleMP（双倍魔力消耗）**：开启后，物品魔力消耗变为原来的两倍
- **RandomResearch（随机研究）**：开启后，物品的研究成本变为随机值（1‑200）
- **FishingPowerHalved（钓鱼力减半）**：开启后，钓竿的钓鱼力减半
- **BaitPowerHalved（饵力减半）**：开启后，鱼饵的饵力减半
- **CritChance（暴击率降低）**：开启后，暴击率大幅度降低
- **PlatformGrappling（钩爪抓不住平台）**：开启后，钩爪再也不能抓住平台了
- **Itemmovement（物品移动）**：开启后，物品会在玩家背包中移动
- **DynamicPricing（动态价格）**：开启后，商店的物品价格将会动态变化，每 10 次售卖后价格下降 2%，每 10 分钟恢复一个售卖计数
- **ChaosTooltip（混乱工具提示）**：开启后，物品的工具提示将变为混乱乱动的文本
- **SelfHarmOnWeaponUse（使用武器伤害自己）**：开启后，使用武器时有概率会对自己造成伤害
- **WorstPrefix（最差前缀）**：开启后，所有物品的前缀将变为最差前缀
- **AlwaysConsumeAmmo（弹药无节省）**：开启后，所有弹药节省效果失效，必定消耗弹药
- **PotionFail（药水概率失效）**：开启后，喝药水有 50% 概率消耗药水但不产生效果（测试，暂时没用）

## PlayerConfigs 玩家配置

- **InventoryRows（减少背包行数）**：开启后，根据游戏进度减少背包行数
- **Defensehalved（防御减半）**：开启后，玩家的防御将减半
- **HalvesMPHP（魔法生命减半）**：开启后，玩家的魔法和生命值将减半
- **InitialHP（初始生命值）**：开启后，玩家重生初始生命值将变为 1
- **Respawntimedoubled（重生时间翻倍）**：开启后，玩家重生时间将翻倍
- **Breathingtime（呼吸时间）**：开启后，玩家的水下呼吸时间将变为 1 秒
- **Wingflighttimehalved（翅膀飞行时间减半）**：开启后，玩家翅膀的飞行时间将减半
- **QuestFishDisappeared（任务鱼消失）**：开启后，钓到任务鱼的时候有 50% 概率跑掉，并刷新渔夫任务
- **SmartCursor（右键智能光标）**：开启后，每次右键都有可能变为智能光标
- **DeadTextDeleted（删除死亡重生倒计时）**：开启后，隐藏玩家死亡重生倒计时文字
- **HighLightSleep（亮度过高无法睡觉）**：开启后，附近亮度过高将无法睡觉
- **MorePeopleDontSleep（附近人太多无法睡觉）**：开启后，附近玩家、城镇 NPC 太多将无法睡觉
- **ToManyMinionDontSleep（太多召唤物无法睡觉）**：开启后，附近自身召唤物太多将无法睡觉
- **ToManyMonstersDontSleep（太多怪物无法睡觉）**：开启后，附近有太多敌对怪物将无法睡觉
**NoRegen（禁止生命自然回复）**：开启后，关闭玩家原生生命再生，只能依靠药水、饰品回血

## TileConfigs 物块配置

- **TileCanDrop（物块掉落）**：开启后，破坏物块有概率不掉落物品
- **CampfireBurning（篝火燃烧）**：开启后，玩家靠近篝火会被点燃持续掉血
- **MaxTreeShakes（树摇动次数）**：开启后，限制一天内树木可摇动的次数
- **AutoOpenDoor（自动开门）**：开启后，移除原版自动开门功能，强制关门
- **PlatformBreakEasy（平台易破碎）**：开启后，高速坠落撞击会打碎平台，效果类似薄冰

## LinkConfigs 联动配置

- **BigBag（削弱大背包）**：开启后，削弱「更好的体验」模组的大背包存储空间

## NPCConfigs NPC 配置

- **HideHealthBar（隐藏血条）**：开启后，NPC 的血条将被隐藏
- **NPCNoDrop（NPC 不掉落物品）**：开启后，NPC 死亡有 50% 概率不会掉落任何物品
- **NPCDropsMoney（NPC 不掉落钱）**：开启后，NPC 击杀不会掉落任何钱币
- **NPCSplit（NPC 分裂）**：开启后，NPC 被攻击至半血会分裂生成一个新的同类型 NPC
- **NPCShopRandomDeleteItem（NPC 商店物品删除）**：开启后，NPC 商店内每个物品有 50% 概率被随机删除
- **WallNPCinvincible（穿墙怪无敌）**：开启后，无视物块的穿墙 NPC 待在方块内部时处于无敌状态
- **NPCGore（NPC 尸块不会消失）**：开启后，NPC 死亡生成的尸块长时间不会消失
- **RandomizeChatButtons（随机聊天按钮）**：开启后，NPC 对话交互按钮顺序被随机打乱

## MapConfigs 世界配置

- **RemoveShimmer（移除微光）**：开启后，移除世界内微光液体与微光物块
- **LingningChange（闪电修改）**：开启后，下雨即可生成闪电，不再仅限雷雨，闪电频率提升 6 倍

## MultiplayerModeConfigs 多人配置

- **DamageMax（NPC 最大伤害）**：开启后，NPC 伤害会根据在线玩家数量进行放大
- **DefenseMax（NPC 最大防御）**：开启后，NPC 防御会根据在线玩家数量进行放大
- **LifeMax（NPC 最大生命值）**：开启后，NPC 生命值会根据在线玩家数量进行放大
- **SpawnRate（NPC 刷怪率）**：开启后，怪物生成速率随在线玩家数量提高
- **SharedBuff（共享 Buff）**：开启后，全部玩家互相共享 Buff，包含负面效果
- **RandomPacketSending（随机发送数据包）**：开启后，游戏网络数据包发送目标被随机篡改，恶搞功能
- **DeathLink（死亡连锁）**：开启后，任意一名玩家死亡，其余所有玩家一同死亡
- **ClosePlayerChat（禁用聊天）**：开启后，禁止玩家使用聊天框

## ProjectileConfigs 弹幕配置

- **ProjectileCritChance（敌对弹幕暴击）**：开启后，敌对弹幕命中获得伤害增伤
- **ProjectileInvisibility（特殊弹幕隐藏）**：开启后，ID 为偶数、质数的弹幕不进行绘制，弹幕实体依旧生效
- **MinionsDisappear（召唤物消失）**：开启后，使用传送回家类物品时清除玩家全部召唤物与炮台
- **ProjectileKillItem（仙人球破坏掉落物）**：开启后，滚动仙人掌弹幕会摧毁地图上的掉落物品