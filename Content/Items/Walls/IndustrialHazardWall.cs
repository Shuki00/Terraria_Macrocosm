using Macrocosm.Content.Items.Blocks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Walls
{
    [LegacyName("MoonBaseHazardWall")]
    public class IndustrialHazardWall : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall(ModContent.WallType<Tiles.Walls.IndustrialHazardWall>());
            Item.width = 22;
            Item.height = 22;
        }

        public override void AddRecipes()
        {
            CreateRecipe(4)
                .AddIngredient<IndustrialPlating>()
                .AddTile(TileID.WorkBenches)
                .DisableDecraft()
                .AddCustomShimmerResult(ModContent.ItemType<IndustrialHazardWallUnsafe>())
                .Register();
        }
    }

    [LegacyName("MoonBaseHazardWallUnsafe")]
    public class IndustrialHazardWallUnsafe : IndustrialHazardWall
    {
        public override string Texture => base.Texture.Replace("Unsafe", "");

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ItemID.Sets.DrawUnsafeIndicator[Type] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.createWall = ModContent.WallType<Tiles.Walls.IndustrialHazardWallUnsafe>();
        }
    }
}