using Macrocosm.Common.Bases;
using Macrocosm.Common.Utils;
using Macrocosm.Content.Gores;
using Macrocosm.Content.Items.Weapons.Ranged;
using Macrocosm.Content.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Projectiles.Friendly.Ranged
{
    public class LHBMinigunProjectile : ChargedHeldProjectile
    {
        private const int windupFrames = 4; // number of windup animaton frames
        private const int shootFrames = 6;  // number of shooting animaton frames

        private const int startTicksPerFrame = 8; // tpf at the start of the animation
        private const int maxTicksPerFrame = 2;   // tpf cap after fullFireRateTime

        private const int windupTime = 45;        // ticks till start of shooting 
        private const int fullFireRateTime = 80;  // ticks to reach full fire rate 

        private const int fireRateStart = 6;      // fireRateFreqStart the ticks period between fires at the start of shooting
        private const int fireRateCap = 1;        // fireRateFreqMaxthe ticks period between fires after fullFireRateTime

        public ref float AI_Windup => ref Projectile.ai[0];

        public override float CircularHoldoutOffset => 32f;

        public override void SetProjectileStaticDefaults()
        {
            Main.projFrames[Type] = 10;
        }

        public override void SetProjectileDefaults()
        {

        }

        private bool CanShoot => AI_Windup >= windupTime;
        public override void ProjectileAI()
        {
            Animate();
            Shoot();
            Visuals();

            if (!Main.dedServ)
                PlaySounds();

            AI_Windup++;
        }

        private float WindupProgress => MathHelper.Clamp((AI_Windup / fullFireRateTime), 0, 1);
        private int WindupTicksPerFrame => (int)MathHelper.Clamp(MathHelper.Lerp(startTicksPerFrame, maxTicksPerFrame, WindupProgress), maxTicksPerFrame, startTicksPerFrame);
        private void Animate()
        {
            Projectile.frameCounter++;

            if (!CanShoot)
            {
                if (Projectile.frameCounter >= WindupTicksPerFrame)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame >= Main.projFrames[Type] - shootFrames)
                        Projectile.frame = 0;
                }
            }
            else
            {
                if (Projectile.frameCounter >= WindupTicksPerFrame)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame >= Main.projFrames[Type])
                        Projectile.frame = windupFrames;
                }
            }
        }

        private void Shoot()
        {
            if (CanShoot)
            {
                // gradually increase fire rate
                int fireFreq = (int)(MathHelper.Clamp(MathHelper.Lerp(fireRateStart, fireRateCap, ((AI_Windup - windupTime) / (fullFireRateTime - windupTime)) * Player.GetAttackSpeed(DamageClass.Ranged)), fireRateCap, fireRateStart));

                if (Main.myPlayer == Projectile.owner)
                {
                    Item currentItem = Player.CurrentItem();
                    if (currentItem.type != ModContent.ItemType<LHBMinigun>())
                        Projectile.Kill();

                    if (StillInUse && AI_Windup % fireFreq == 0)
                    {
                        Vector2 rotPoint = Utility.RotatingPoint(Projectile.Center, new Vector2(40, 8 * Projectile.spriteDirection), Projectile.rotation);
                        if (Player.PickAmmo(currentItem, out int projToShoot, out float speed, out int damage, out float knockback, out _))
                        {
                            // We want to disable knockback for this weapon but keep it able to receive the Unreal modifier
                            knockback = 0f; 
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(), rotPoint, Vector2.Normalize(Projectile.velocity).RotatedByRandom(MathHelper.ToRadians(14)) * speed, projToShoot, damage, knockback, Projectile.owner);
                        }
                        else
                        {
                            Projectile.Kill();
                        }
                    }
                }

                Projectile.position += (new Vector2(Main.rand.NextFloat(2.4f), Main.rand.NextFloat(0.4f))).RotatedBy(Projectile.rotation) * WindupProgress;

                if (!Main.dedServ && AI_Windup % (fireFreq * 1.5) == 0)
                {
                    Vector2 position = Projectile.Center - new Vector2(-20, 0) * Projectile.spriteDirection;
                    Vector2 velocity = new(1.2f * Projectile.spriteDirection, 4f);
                    Gore.NewGore(Projectile.GetSource_FromThis(), position, velocity, ModContent.GoreType<MinigunShell>());
                }
            }
        }

        public void Visuals()
        {
            if (CanShoot)
                Lighting.AddLight(Projectile.position + Utility.PolarVector(80f, Projectile.rotation), TorchID.Torch);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawAnimated(lightColor, Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None, new Vector2(5, 10));
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D glowmask = ModContent.Request<Texture2D>("Macrocosm/Content/Projectiles/Friendly/Ranged/LHBMinigunProjectile_Glow").Value;
            Projectile.DrawAnimatedExtra(glowmask, Color.White, Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None, new Vector2(5, 10));
        }

        private SlotId playingSoundId_1 = SlotId.Invalid;
        private SlotId playingSoundId_2 = SlotId.Invalid;
        private void PlaySounds()
        {
            if (!StillInUse)
                return;

            if (!playingSoundId_1.IsValid && AI_Windup < windupTime)
            {
                playingSoundId_1 = SoundEngine.PlaySound(SFX.MinigunWindup with
                {
                    Volume = 0.15f,
                    IsLooped = true,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Projectile.position);
            }

            if (!playingSoundId_2.IsValid && AI_Windup >= windupTime)
            {
                playingSoundId_2 = SoundEngine.PlaySound(SFX.MinigunFire with
                {
                    Volume = 0.15f,
                    IsLooped = true,
                    SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
                },
                Projectile.position);

                if (SoundEngine.TryGetActiveSound(playingSoundId_1, out ActiveSound sound))
                    sound.Stop();
            }

            if (SoundEngine.TryGetActiveSound(playingSoundId_1, out ActiveSound playingSound1))
                playingSound1.Position = Projectile.position;

            if (SoundEngine.TryGetActiveSound(playingSoundId_2, out ActiveSound playingSound2))
                playingSound2.Position = Projectile.position;
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(playingSoundId_1, out ActiveSound playingSound1))
                playingSound1.Stop();

            if (SoundEngine.TryGetActiveSound(playingSoundId_2, out ActiveSound playingSound2))
                playingSound2.Stop();
        }
    }
}
