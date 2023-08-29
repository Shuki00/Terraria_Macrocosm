using Macrocosm.Common.Drawing.Particles;
using Macrocosm.Common.Utils;
using Macrocosm.Content.Particles;
using Macrocosm.Content.Projectiles.Global;
using Macrocosm.Content.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Projectiles.Friendly.Ranged
{
	internal class TychoBullet : ModProjectile, IBullet
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.Bullet);
			AIType = ProjectileID.Bullet;
			Projectile.width = 4;
			Projectile.height = 4;
		}

		bool spawned = false;
		int flashTimer;
		public override bool PreAI()
		{
			if (!spawned)
			{
				if(!Main.dedServ)
					SoundEngine.PlaySound(SFX.DesertEagleShoot with { Volume = 0.3f }, Projectile.position);

				spawned = true;
			}

			if(flashTimer++ == 1)
				Particle.CreateParticle<GunFireRing>(Projectile.position, Projectile.velocity * 0.8f, 1f, Projectile.velocity.ToRotation(), false);

			Lighting.AddLight(Projectile.position, new Color(255, 202, 141).ToVector3() * 0.6f);

			return true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Projectile.DrawMagicPixelTrail(Vector2.Zero, 2.8f, 0.5f, new Color(255, 162, 141) * lightColor.GetLuminance(), new Color(184, 58, 24, 0) * lightColor.GetLuminance());
			return true;
		}
	}
}
