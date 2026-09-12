using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorstGame.Common.Configs
{
    public class MapConfigs: ModConfig
    {
        public static MapConfigs Instance;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        
        [DefaultValue(false)]
        public bool RemoveShimmer;  
        [DefaultValue(false)]
        public bool LingningChange;
    }
}
