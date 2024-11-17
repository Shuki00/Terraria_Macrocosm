﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Macrocosm.Common.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace Macrocosm.Common.Systems.Power
{
    public abstract class BatteryTE : MachineTE
    {
        /// <summary> Current stored energy </summary>
        public float StoredEnergy { get; set; }

        /// <summary> Maximum storage energy </summary>
        public abstract float EnergyCapacity { get; }

        public override void UpdatePowerState()
        {
            if (PoweredOn && StoredEnergy <= 0f)
                PowerOff();
            else if (!PoweredOn && StoredEnergy > 0f)
                PowerOn();
        }

        public override Color DisplayColor => Color.Cyan;
        public override string GetPowerInfo() => $"{Language.GetText($"Mods.Macrocosm.Machines.Common.PowerInfo.Battery").Format($"{StoredEnergy:F2}", $"{EnergyCapacity:F2}")}";
        public override void DrawMachinePowerInfo(SpriteBatch spriteBatch)
        {
            Vector2 position = Position.ToWorldCoordinates() - Main.screenPosition;
            string active = Language.GetText($"Mods.Macrocosm.Machines.Common.PowerInfo.Energy").Format($"{StoredEnergy:F0}");
            string total = Language.GetText($"Mods.Macrocosm.Machines.Common.PowerInfo.Energy").Format($"{EnergyCapacity:F0}");
            string line = new('_', Math.Max(active.Length, total.Length) / 2);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, active, position - new Vector2(active.Length, 50), DisplayColor, 0f, Vector2.Zero, Vector2.One);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, line, position - new Vector2(line.Length + 5, 48), DisplayColor, 0f, Vector2.Zero, Vector2.One);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, total, position - new Vector2(total.Length, 26), DisplayColor, 0f, Vector2.Zero, Vector2.One);
        }
    }
}