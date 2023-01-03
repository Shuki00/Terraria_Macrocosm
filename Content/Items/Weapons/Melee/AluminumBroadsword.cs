using Macrocosm.Common.Utils;
using Macrocosm.Content.Items.Materials;
using Macrocosm.Content.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Weapons.Melee
{
	public class AluminumBroadsword : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults()
		{
			Item.damage = 11;
			Item.DamageType = DamageClass.Melee;
			Item.width = 36;
			Item.height = 36;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 5.5f;
			Item.value = Item.sellPrice(silver: 3, copper: 10);
			Item.rare = ItemRarityID.White;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;  
 		}

		public override void AddRecipes()
		{
			Recipe recipe = Recipe.Create(Type);
			recipe.AddIngredient(ModContent.ItemType<AluminumBar>(), 8);
 			recipe.AddTile(TileID.Anvils);
			recipe.Register();
		}
	}
}