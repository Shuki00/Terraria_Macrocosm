﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Placeable.Tombstones
{
	public class MoonGoldTombstone : ModItem
	{
		public override void SetStaticDefaults()
		{
			
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 30;
			Item.useTurn = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.maxStack = 9999;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Tiles.Tombstones.MoonTombstone>();
			Item.placeStyle = Tiles.Tombstones.MoonTombstone.ItemToStyle(Type);
		}
	}
}