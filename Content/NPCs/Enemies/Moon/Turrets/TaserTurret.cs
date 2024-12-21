using Macrocosm.Common.Sets;
using Macrocosm.Content.Dusts;
using Macrocosm.Content.Items.Tech;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using ReLogic.Content;
using Macrocosm.Content.Projectiles.Hostile;

namespace Macrocosm.Content.NPCs.Enemies.Moon.Turrets
{
    public class TaserTurret : ModNPC
    {
        private static Asset<Texture2D> turretTexture;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;

            NPCSets.MoonNPC[Type] = true;
            NPCSets.DropsMoonstone[Type] = false;
        }
        public override string Texture => "Macrocosm/Content/NPCs/Enemies/Moon/Turrets/Turret_Base";
        public override void SetDefaults()
        {
            NPC.width = 38;
            NPC.height = 40;
            NPC.damage = 40;
            NPC.defense = 50;
            NPC.lifeMax = 2000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 60f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
        }
        public Vector2 TurretHeight=new Vector2(0,-22);
        private float TurretRotation =0f;  
        int AI_Timer=0;
        
        public override void AI()
        {
            NPC.TargetClosest(faceTarget:true);
            Player player = Main.player[NPC.target];
           
            if( NPC.ai[0]==0f){
            Vector2 turningVector = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
            TurretRotation = (new Vector2(5,0).RotatedBy(TurretRotation) + (turningVector * 0.3f)).ToRotation();

            bool clearLineOfSight = Collision.CanHitLine(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height);
            if (NPC.HasPlayerTarget && clearLineOfSight)
            {
                if((NPC.direction>=0&&player.Center.X>NPC.Center.X)||(NPC.direction<0&&player.Center.X<=NPC.Center.X))
                    AI_Timer++;
                if(AI_Timer==120)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center+TurretHeight+new Vector2(15,0).RotatedBy(TurretRotation), new Vector2(15f,0).RotatedBy(TurretRotation), ModContent.ProjectileType<TurretTaserProjectile>(), 50, 1f, Main.myPlayer, ai1 : NPC.whoAmI);
                    NPC.ai[0]=1f;
                    AI_Timer=0;
                }
                
            }
            else{
                AI_Timer=0;
            }
            }
            
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            turretTexture ??= ModContent.Request<Texture2D>("Macrocosm/Content/NPCs/Enemies/Moon/Turrets/Turret_Taser_Top");
            Rectangle frameOfGun = turretTexture.Frame(1, 2, 0, NPC.ai[0]==0f ? 0 : 1);
            spriteBatch.Draw(turretTexture.Value, NPC.Center + TurretHeight - Main.screenPosition, frameOfGun, drawColor, TurretRotation, frameOfGun.Size() / 2, NPC.scale,  NPC.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0f);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return 0f;
        }

        public override void ModifyNPCLoot(NPCLoot loot)
        {
            loot.Add(ItemDropRule.Common(ModContent.ItemType<PrintedCircuitBoard>(), 2));
            loot.Add(ItemDropRule.Common(ModContent.ItemType<Motor>(), 2));
            loot.Add(ItemDropRule.Common(ModContent.ItemType<Gear>(), 2));
            loot.Add(ItemDropRule.Common(ModContent.ItemType<Battery>(), 2));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 10; i++)
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, ModContent.DustType<IndustrialPlatingDust>());
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X *= dust.velocity.X * 1.25f * hit.HitDirection + Main.rand.Next(0, 100) * 0.015f;
                dust.velocity.Y *= dust.velocity.Y * 0.25f + Main.rand.Next(-50, 51) * 0.01f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
            }
        }
    }
}