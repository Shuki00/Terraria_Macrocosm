﻿using Macrocosm.Common.Enums;
using Macrocosm.Common.Systems.Power;
using Terraria;
using Terraria.ModLoader;

namespace Macrocosm.Content.Machines
{
    public class SolarPanelTileTE : GeneratorTE
    {
        public override MachineTile MachineTile => ModContent.GetInstance<SolarPanelTile>();

        public override bool PoweredOn => Main.dayTime;

        public override void OnFirstUpdate()
        {
        }

        public override void MachineUpdate()
        {
            MaxGeneratedPower = 0.083f;
            GeneratedPower = PoweredOn ? MaxGeneratedPower : 0;
        }
    }
}
