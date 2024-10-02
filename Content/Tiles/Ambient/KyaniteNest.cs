using Macrocosm.Content.Dusts;
using Macrocosm.Content.NPCs.Critters;
using Macrocosm.Content.NPCs.Enemies.Moon;
using Microsoft.Xna.Framework;
using Terraria;
using Macrocosm.Common.Utils;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Macrocosm.Content.Tiles.Ambient
{
    public class KyaniteNest : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false;
            Main.tileSolidTop[Type] = false;
            Main.tileTable[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;

            DustType = ModContent.DustType<ProtolithDust>();

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.Origin = new(0, 2);
            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;
            AddMapEntry(new Color(59, 63, 59), CreateMapEntryName());
        }
        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.gamePaused)
                return;
            int bugs=0;
            bugs+=Utility.AmountOfNPCs(ModContent.NPCType<KyaniteScarabSmall>());
            bugs+=Utility.AmountOfNPCs(ModContent.NPCType<KyaniteScarabCritter>());
            if(bugs<8){
            if(Main.rand.NextBool(120*(bugs+1))){
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int npcType = Main.rand.NextBool(4) ? ModContent.NPCType<KyaniteScarabSmall>() : ModContent.NPCType<KyaniteScarabCritter>();
                NPC.NewNPCDirect(Entity.GetSource_NaturalSpawn(), new Vector2(i, j+1) * 16f, npcType);
            }
            }
            }
        }
        public override void RandomUpdate(int i, int j)
        {
            
        }
    }
}