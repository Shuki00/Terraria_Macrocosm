﻿using Macrocosm.Common.Enums;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Furniture.LunarRust
{
    public class LunarRustBed : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.Furniture.Luminite.LuminiteBed>(), (int)LuminiteStyle.LunarRust);
            Item.width = 36;
            Item.height = 18;
            Item.value = 150;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LunarRustBrick, 15)
                .AddIngredient(ItemID.Silk, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
