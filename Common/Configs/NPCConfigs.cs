using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorstGame.Common.Configs
{
    public class NPCConfigs : ModConfig
    {
        public static NPCConfigs Instance;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        [DefaultValue(false)]
        public bool HideHealthBar;
        [DefaultValue(false)]
        public bool NPCNoDrop;
        [DefaultValue(false)]
        public bool NPCDropsMoney;
        [DefaultValue(false)]
        public bool NPCSplit;        
        [DefaultValue(false)]
        public bool NPCShopRandomDeleteItem;
        [DefaultValue(false)]
        public bool WallNPCinvincible;
        [DefaultValue(false)]
        public bool NPCGore;
        [DefaultValue(false)] 
        public bool RandomizeChatButtons;
    }
}
