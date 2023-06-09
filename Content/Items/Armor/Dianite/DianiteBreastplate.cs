// using Macrocosm.Tiles;
using Macrocosm.Content.Items.Materials;
using Macrocosm.Content.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Armor.Dianite
{
    [AutoloadEquip(EquipType.Body)]
    public class DianiteBreastplate : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = 10000;
            Item.rare = ModContent.RarityType<MoonRarityT1>();
            Item.defense = 40;
        }

        public override void AddRecipes()
        {
            Recipe recipe = Recipe.Create(Type);
            recipe.AddIngredient(ModContent.ItemType<DianiteBar>(), 16);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}