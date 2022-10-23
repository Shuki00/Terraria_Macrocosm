using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Materials.Chunks
{
	public class SidusChunk : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Sidus Chunk");
			Tooltip.SetDefault("'The fire burns like a star'");
			ItemID.Sets.ItemNoGravity[Type] = true;
			ItemID.Sets.AnimatesAsSoul[Type] = true;
			Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(4, 4)); // NOTE: TicksPerFrame, Frames
		}

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.rare = ItemRarityID.Red;
			Item.maxStack = 9999;
		}
	}
}