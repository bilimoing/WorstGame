using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorstGame.Common.Configs;

public class MultiplayerModeConfigs : ModConfig
{
    public static MultiplayerModeConfigs Instance;
    public override ConfigScope Mode => ConfigScope.ServerSide;
    
    [DefaultValue(false)]
    public bool DamageMax;
    [DefaultValue(false)]
    public bool DefenseMax;
    [DefaultValue(false)]
    public bool LifeMax;
    [DefaultValue(false)]
    public bool SpawnRate;
    [DefaultValue(false)]
    public bool SharedBuff;
    [DefaultValue(false)]
    public bool RandomPacketSending;
    [DefaultValue(false)]
    public bool DeathLink;
    [DefaultValue(false)]
    public bool ClosePlayerChat;
}