using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorstGame.Common.Configs
{
    public class TileConfigs: ModConfig
    {
        public static TileConfigs Instance;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        
        [DefaultValue(false)]
        public bool TileCanDrop;
        [DefaultValue(false)]
        public bool CampfireBurning;
        [DefaultValue(false)]
        public bool MaxTreeShakes;
        [DefaultValue(false)]
        public bool AutoOpenDoor;
        [DefaultValue(false)]
        public bool PlatformBreakEasy;
    }
}