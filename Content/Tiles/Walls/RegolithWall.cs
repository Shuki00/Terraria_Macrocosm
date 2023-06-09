using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace Macrocosm.Content.Tiles.Walls
{
    public class RegolithWall : ModWall
    {
        public override void SetStaticDefaults()
        {
            Main.wallHouse[Type] = false; // Unsafe wall
            AddMapEntry(new Color(45, 45, 45));
        }
    }
}