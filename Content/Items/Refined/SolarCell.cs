using Macrocosm.Content.Items.Ores;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Refined
{
    public class SolarCell : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = 100;
            Item.rare = ItemRarityID.LightRed;
            
        }

        public override void AddRecipes()
        {
            CreateRecipe(4)
                .AddIngredient<Silicon>(3)
                .AddIngredient(ItemID.Wire)
                .AddTile<Tiles.Crafting.Fabricator>()
                .Register();
        }
    }
}