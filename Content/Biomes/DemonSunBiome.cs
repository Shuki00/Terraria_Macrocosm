﻿using Macrocosm.Common.Systems;
using Macrocosm.Content.Backgrounds.Moon;
using Macrocosm.Content.Subworlds;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;

namespace Macrocosm.Content.Biomes
{
    public class DemonSunBiome : ModBiome
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override string BestiaryIcon => Macrocosm.TexturesPath + "Icons/BloodMoon";
        public override string BackgroundPath => Macrocosm.TexturesPath + "MapBackgrounds/Moon";
        public override string MapBackground => BackgroundPath;

        public override Color? BackgroundColor => base.BackgroundColor;
        public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.GetInstance<MoonSurfaceBackgroundStyle>();
        public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.GetInstance<MoonUndergroundBackgroundStyle>();
        public override int Music => Main.dayTime ? MusicLoader.GetMusicSlot(Mod, "Assets/Music/Deadworld") : MusicLoader.GetMusicSlot(Mod, "Assets/Music/Requiem");

        public override void SetStaticDefaults()
        {
        }

        public override bool IsBiomeActive(Player player) => SubworldSystem.IsActive<Moon>() && TileCounts.Instance.IrradiatedRockCount > 400;

        public override void OnInBiome(Player player)
        {
        }

        public override void OnEnter(Player player)
        {
        }

        public override void OnLeave(Player player)
        {
        }
    }
}
