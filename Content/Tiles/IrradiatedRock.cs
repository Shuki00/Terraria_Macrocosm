using Macrocosm.Common.Utils;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Tiles
{
	public class IrradiatedRock : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			Main.tileLighted[Type] = true;
			Main.tileMerge[Type][ModContent.TileType<Protolith>()] = true;
			Main.tileMerge[Type][ModContent.TileType<Regolith>()] = true;
			MinPick = 275;
			MineResist = 3f;
			ItemDrop = ModContent.ItemType<Items.Placeable.BlocksAndWalls.IrradiatedRock>();
			AddMapEntry(new Color(129, 117, 0));
			HitSound = SoundID.Tink;
		}

		public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
		{
			return true;
		}
	}
}	