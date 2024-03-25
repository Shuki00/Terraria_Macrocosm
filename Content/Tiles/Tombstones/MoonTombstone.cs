﻿using Macrocosm.Common.Bases.Tiles;
using Macrocosm.Content.Dusts;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Macrocosm.Content.Tiles.Tombstones
{
    public class MoonTombstone : ModTile, ITombstoneTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			Main.tileSign[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.Origin = new Point16(0, 1);
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.DrawYOffset = 2;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.addTile(Type);

			HitSound = SoundID.Dig;
			DustType = ModContent.DustType<RegolithDust>();

			AddMapEntry(new Color(180, 180, 180));

            RegisterItemDrop(ModContent.ItemType<Items.Placeable.Tombstones.MoonTombstone>(), 0, 1);
            RegisterItemDrop(ModContent.ItemType<Items.Placeable.Tombstones.MoonHeadstone>(), 2, 3);
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            base.NearbyEffects(i, j, closer);
        }
    }
}
