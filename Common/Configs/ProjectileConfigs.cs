using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorstGame.Common.Configs;

public class ProjectileConfigs: ModConfig
{
    public static ProjectileConfigs Instance;
    public override ConfigScope Mode => ConfigScope.ServerSide;
    
    [DefaultValue(false)]
    public bool ProjectileCritChance;
    [DefaultValue(false)]
    public bool ProjectileInvisibility;
    [DefaultValue(false)]
    public bool MinionsDisappear;
    [DefaultValue(false)]
    public bool ProjectileKillItem;
}
