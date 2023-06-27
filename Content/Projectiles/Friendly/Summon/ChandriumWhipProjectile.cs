﻿using Macrocosm.Common.Drawing.Particles;
using Macrocosm.Common.Utils;
using Macrocosm.Content.Buffs.GoodBuffs;
using Macrocosm.Content.Dusts;
using Macrocosm.Content.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Projectiles.Friendly.Summon
{
    public class ChandriumWhipProjectile : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.IsAWhip[Type] = true;
		}

		public override void SetDefaults()
		{

			Projectile.DefaultToWhip();

			Projectile.WhipSettings.Segments = 29;
			Projectile.WhipSettings.RangeMultiplier = 1.9f;
		}

		// AI timer for whip swing 
		private float Timer
		{
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		// flag that stores if an npcs has already been hit in the current swing 
		private bool HitNPC
		{
			get => Projectile.ai[1] != 0f;
			set => Projectile.ai[1] = value ? 1f : 0f;
		}

		public Vector2 WhipTipPosition;

		private ref int HitStacks => ref Main.player[Projectile.owner].Macrocosm().ChandriumEmpowermentStacks;

		// Extra AI data used for the on-hit effects 
		private bool onHitEffect = false;
		private bool empoweredHit = false;
		private int hitNpcId = 0;

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			// increase count only once per whip swing 
			if(!HitNPC && HitStacks < 3)
			{
				HitStacks++;   // this is a ref to a ModPlayer
				HitNPC = true; // set hit flag to true so stacks won't increase on every npc hit 
			}

			onHitEffect = true;
			hitNpcId = target.whoAmI;

			// if empowered hit is ready, ignore hit flag  
			if (HitStacks >= 3)
			{
				// increase damage 
				modifiers.FinalDamage *= 1.4f;

				// clear buff on successful hit 
				Main.player[Projectile.owner].ClearBuff(ModContent.BuffType<ChandriumEmpowerment>());

				empoweredHit = true;
			}
			else  
			{
				empoweredHit = false;
			}

			Projectile.netUpdate = true;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(onHitEffect);
			writer.Write(empoweredHit);
			writer.Write(hitNpcId);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			onHitEffect = reader.ReadBoolean();
			empoweredHit = reader.ReadBoolean();
			hitNpcId = reader.ReadInt32();
		}

		/// <summary> Spawn dusts on hit, called on the owner client </summary>
		public void SpawnDusts(NPC target, bool empowered, bool update = false)
		{
			if (empowered)
			{
  				for (int i = 0; i < 60; i++)
				{
					Vector2 position = target.position + new Vector2(Main.rand.Next(target.width), Main.rand.Next(target.height));
					Vector2 velocity = Main.rand.NextVector2Circular(2f, 2f);
					Dust dust;
					if (i % 20 == 0)
 						Particle.CreateParticle<ChandriumCrescentMoon>(position, velocity, scale: Main.rand.NextFloat(0.8f, 1.2f));

 					// chandrium dust 
					dust = Dust.NewDustDirect(position, target.width, target.height, ModContent.DustType<ChandriumDust>(), velocity.X, velocity.Y, Scale: Main.rand.NextFloat(0.8f, 1.2f));
				}
			}
			else
			{
				// fewer regular dust on non-empowered hit (regardless of hit flag) 
				for (int i = 0; i < 5; i++)
				{
					Vector2 position = target.position;
					Vector2 velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
					Dust dust = Dust.NewDustDirect(position, target.width, target.height, ModContent.DustType<ChandriumDust>(), velocity.X, velocity.Y, Scale: Main.rand.NextFloat(0.8f, 1.2f));
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// minions will attack the npcs hit with this whip 
			Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
		}

		public override void OnSpawn(IEntitySource source)
		{
			// on spawn reset the hit npc flag 
			HitNPC = false;
		}

		public override void AI()
		{
			if (onHitEffect)
			{
				SpawnDusts(Main.npc[hitNpcId], empoweredHit);
				onHitEffect = false;
			}
		}

		Particle sparkle;

		public override void Kill(int timeLeft)
		{
			if (Projectile.owner == Main.myPlayer && HitNPC)
			{
				if (HitNPC)
				{
					// reset stacks on end of successful empowered hit 
					if (HitStacks >= 3)
					{
						HitStacks = 0;
						ChandriumEmpowerment.KillParticle();
					}

					// add buff 
					if (HitStacks == 2)
					{
						sparkle = Particle.CreateParticle<ChandriumSparkle>(particle =>
						{
							Main.player[Projectile.owner].AddBuff(ModContent.BuffType<ChandriumEmpowerment>(), 60 * 5);

							particle.Position = WhipTipPosition;
							particle.Owner = (byte)Projectile.owner;
						});
					}

					if (HitStacks == 0 && HitNPC && sparkle is not null)
						sparkle.Kill();
				}
			}
 		}

		private int frameWidth = 14;
		private int frameHeight = 26;

		// This method draws a line between all points of the whip, in case there's empty space between the sprites.
		private void DrawLine(List<Vector2> list)
		{
			Texture2D texture = TextureAssets.FishingLine.Value;
			Rectangle frame = texture.Frame();
			Vector2 origin = new(frame.Width / 2, 2);

			Vector2 pos = list[0];
			for (int i = 0; i < list.Count - 1; i++)
			{
				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation() - MathHelper.PiOver2;
				Color color = Lighting.GetColor(element.ToTileCoordinates(), new Color(60, 27, 120, byte.MaxValue));
				Vector2 scale = new(1, (diff.Length() + 2) / frame.Height);

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

				pos += diff;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			List<Vector2> list = new List<Vector2>();
			Projectile.FillWhipControlPoints(Projectile, list);

			DrawLine(list);

			SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			Main.instance.LoadProjectile(Type);
			Texture2D texture = TextureAssets.Projectile[Type].Value;

			Vector2 pos = list[0];

			for (int i = 0; i < list.Count - 1; i++)
			{
				Rectangle frame = new(0, 0, frameWidth, frameHeight);
				Vector2 origin = new(frameWidth / 2, frameHeight / 2);
				float scale = 1;

				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation() - MathHelper.PiOver2; // This projectile's sprite faces down, so PiOver2 is used to correct rotation.
				Color color = Lighting.GetColor(element.ToTileCoordinates());

				bool tip = i == list.Count - 2;

				if (tip)
				{
					frame.Y = 4 * frameHeight;

					// For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
					Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
					float t = Timer / timeToFlyOut;
					scale = MathHelper.Lerp(0.4f, 1.3f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));

					WhipTipPosition = pos;

					/*#region Shine whip tip
					if (Main.player[Projectile.owner].HasBuff(ModContent.BuffType<ChandriumEmpowerment>()))
					{
						if (Projectile.localAI[0] < 1f)
							Projectile.localAI[0] += 0.2f;

						Main.EntitySpriteDraw(TextureAssets.Extra[89].Value, pos - Main.screenPosition, null, new Color(177, 107, 219, 255), 0f + rotation, TextureAssets.Extra[89].Size() / 2f, Projectile.localAI[0], flip, 0);
						Main.EntitySpriteDraw(TextureAssets.Extra[89].Value, pos - Main.screenPosition, null, new Color(177, 107, 219, 255), MathHelper.PiOver2 + rotation, TextureAssets.Extra[89].Size() / 2f, Projectile.localAI[0], flip, 0);
						Lighting.AddLight(pos, new Vector3(0.607f, 0.258f, 0.847f));
					}
					#endregion*/

					#region Dusts
					// Depends on whip extenstion
					float dustChance = Utils.GetLerpValue(0.1f, 0.7f, t, clamped: true) * Utils.GetLerpValue(0.9f, 0.7f, t, clamped: true);

					// Spawn dust
					if (dustChance > 0.5f && Main.rand.NextFloat() < dustChance * 0.7f)
					{
						Vector2 outwardsVector = list[^2].DirectionTo(list[^1]).SafeNormalize(Vector2.Zero);
						Dust dust = Dust.NewDustDirect(list[^1] - texture.Size() / 2, texture.Width, texture.Height, ModContent.DustType<ChandriumDust>(), 0f, 0f, 100, default, Main.rand.NextFloat(1f, 1.5f));

						dust.noGravity = true;
						dust.velocity *= Main.rand.NextFloat() * 0.8f;
						dust.velocity += outwardsVector * 0.8f;
					}
					#endregion
				}
				else if (i >= 19)
 					frame.Y = 3 * frameHeight; 
 				else if (i >= 10)
 					frame.Y = 2 * frameHeight;  
 				else if (i >= 1)
 					frame.Y = frameHeight; 
 				else
 					frame.Y = 0;
 
				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, flip, 0);

				pos += diff;
			}
			return false;
		}
	}
}
