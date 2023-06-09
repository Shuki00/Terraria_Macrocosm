using Macrocosm.Common.Drawing.Trails;
using Macrocosm.Common.Netcode;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Macrocosm.Content.Projectiles.Global
{
	public class MacrocosmProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;
		public Trail Trail { get; set; }

		public override void SetDefaults(Projectile projectile)
		{
			
		}

		public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
		{
			if(projectile.ModProjectile is IBullet)  
				Collision.HitTiles(projectile.position, oldVelocity, projectile.width, projectile.height);

			if (projectile.ModProjectile is IExplosive explosive)
			{
				explosive.OnCollide(projectile);
				return false;
			}

			return true;
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{ 
			if(projectile.ModProjectile is IExplosive explosive)
			{
				explosive.OnCollide(projectile);
				projectile.Kill();  
			}
		}


		public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
		{
			if (projectile.ModProjectile is IExplosive explosive)
				explosive.OnCollide(projectile);
		}

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
		{
			if (projectile.ModProjectile is null)
				return;

			//if (!projectile.ModProjectile.NetWriteFields(binaryWriter, bitWriter))
			//	binaryWriter.Dispose();

			projectile.ModProjectile.NetWriteFields(binaryWriter, bitWriter);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
		{
			if (projectile.ModProjectile is null)
				return;

			projectile.ModProjectile.NetReadFields(binaryReader, bitReader);
		}
	}
}