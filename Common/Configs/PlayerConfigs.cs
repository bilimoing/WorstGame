using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorstGame.Common.Configs;

public class PlayerConfigs : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    public static PlayerConfigs Instance;

    [DefaultValue(false)] 
    public bool InventoryRows;
    [DefaultValue(false)]
    public bool Defensehalved;
    [DefaultValue(false)]
    public bool HalvesMPHP;
    [DefaultValue(false)]
    public bool InitialHP;
    [DefaultValue(false)]
    public bool Respawntimedoubled;
    [DefaultValue(false)]
    public bool Breathingtime;
    [DefaultValue(false)]
    public bool Wingflighttimehalved;
    [DefaultValue(false)]
    public bool QuestFishDisappeared;
    [DefaultValue(false)]
    public bool SmartCursor;
    [DefaultValue(false)]
    public bool DeadTextDeleted;
    [DefaultValue(false)]
    public bool HighLightSleep;
    [DefaultValue(false)]
    public bool MorePeopleDontSleep;
    [DefaultValue(false)]
    public bool ToManyMinionDontSleep;
    [DefaultValue(false)]
    public bool ToManyMonstersDontSleep;
    
}