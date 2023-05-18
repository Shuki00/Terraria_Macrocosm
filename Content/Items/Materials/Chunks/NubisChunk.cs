using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Materials.Chunks
{
	public class NubisChunk : ModItem
	{
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Nubis Chunk");
			// Tooltip.SetDefault("'It spins akin to a galaxy'");
			ItemID.Sets.ItemNoGravity[Type] = true;
			ItemID.Sets.AnimatesAsSoul[Type] = true;
			Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 7)); // NOTE: TicksPerFrame, Frames
		}

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.rare = ItemRarityID.Pink;
			Item.maxStack = 9999;
		}
	}
}