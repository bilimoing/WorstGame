using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorstGame.Common.Configs;

public class ItemConfigs : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    public static ItemConfigs Instance;
    
    [DefaultValue(false)] 
    [ReloadRequired] 
    public bool AutoReuse;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool MaxStack;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool DamageDefault;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool DoubleMP;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool FishingPowerHalved;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool BaitPowerHalved;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool CritChance;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool RandomResearch;
    [DefaultValue(false)]
    public bool PlatformGrappling;
    [DefaultValue(false)]
    public bool Itemmovement;
    [DefaultValue(false)]
    public bool DynamicPricing;
    [DefaultValue(false)]
    public bool ChaosTooltip;
    [DefaultValue(false)]
    public bool SelfHarmOnWeaponUse;
    [DefaultValue(false)]
    public bool WorstPrefix;
    [DefaultValue(false)]
    [ReloadRequired]
    public bool AlwaysConsumeAmmo;
    [DefaultValue(false)]
    public bool PotionFail;
}