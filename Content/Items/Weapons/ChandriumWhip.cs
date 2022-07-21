﻿using Macrocosm.Content.Items.Materials;
using Macrocosm.Content.Projectiles.Friendly.Weapons;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Weapons
{
	public class ChandriumWhip : ModItem
	{
		public override void SetStaticDefaults() {
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults() {
			Item.DefaultToWhip(ModContent.ProjectileType<ChandriumWhipProjectile>(), 220, 2, 4); 

			Item.shootSpeed = 4;
			Item.rare = ItemRarityID.Green;

			Item.channel = true;
		}

		public override void AddRecipes() {
			Recipe recipe = Recipe.Create(Type);
			recipe.AddIngredient<LuminiteCrystal>();
			recipe.AddIngredient<ChandriumBar>(12);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
