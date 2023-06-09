using Macrocosm.Content.Items.Materials;
using Macrocosm.Content.Rarities;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Tools.Dianite
{
	public class DianitePickaxe : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}
		public override void SetDefaults()
		{
			Item.damage = 90;
			Item.DamageType = DamageClass.Melee;
			Item.width = 44;
			Item.height = 44;
			Item.useTime = 5;
			Item.useAnimation = 12;
			Item.pick = 250;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 6;
			Item.value = 10000;
			Item.rare = ModContent.RarityType<MoonRarityT1>();
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
			Item.tileBoost = 5;
		}

		public override void AddRecipes()
		{
			Recipe recipe = Recipe.Create(Type);
			recipe.AddIngredient<LuminiteCrystal>();
			recipe.AddIngredient<DianiteBar>(12);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}