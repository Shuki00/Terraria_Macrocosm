﻿using Macrocosm.Common.Enums;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace Macrocosm.Content.Items.Furniture.Heavenforge
{
    public class HeavenforgeBathtub : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.Furniture.Luminite.LuminiteBathtub>(), (int)LuminiteStyle.Heavenforge);
            Item.width = 34;
            Item.height = 20;
            Item.value = 150;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HeavenforgeBrick, 14)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
