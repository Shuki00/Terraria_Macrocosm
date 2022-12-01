﻿using Macrocosm.Content.Rarities;
using Macrocosm.Content.Rocket;
using Macrocosm.Content.Subworlds.Moon;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Dev
{
	class RocketPlacer : ModItem
	{
		public override void SetStaticDefaults()
		{
			Tooltip.SetDefault("Places a rocket");
		}
		public override void SetDefaults()
		{
			Item.width = 36;
			Item.height = 36;
			Item.rare = ModContent.RarityType<MoonRarityT3>();
			Item.value = 100000;
			Item.maxStack = 1;
			Item.useTime = 1;
			Item.useAnimation = 1;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.UseSound = SoundID.Item6;
			Item.createTile = ModContent.TileType<RocketCommandModule>();

		}
	}
}
