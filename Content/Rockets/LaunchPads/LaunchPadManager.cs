﻿using Macrocosm.Common.Subworlds;
using Macrocosm.Common.UI;
using Macrocosm.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace Macrocosm.Content.Rockets.LaunchPads
{
	public class LaunchPadManager : ModSystem
	{
		private static Dictionary<string, List<LaunchPad>> launchPadStorage;

		public override void Load()
		{
			launchPadStorage = new Dictionary<string, List<LaunchPad>>();
		}

		public override void Unload()
		{
			launchPadStorage.Clear();
			launchPadStorage = null;
		}

		public static void Add(string subworldId, LaunchPad launchPad, bool shouldSync = true)
		{
			if (launchPadStorage.ContainsKey(subworldId))
			{
				launchPadStorage[subworldId].Add(launchPad);
			}
			else
			{
				launchPadStorage.Add(subworldId, new() { launchPad });
			}

			launchPad.Active = true;

			if(shouldSync)
				launchPad.NetSync(subworldId);
		}

		public static void Remove(string subworldId, LaunchPad launchPad, bool shouldSync = true)
		{
			if (launchPadStorage.ContainsKey(subworldId))
			{
				var toRemove = GetLaunchPadAtStartTile(subworldId, launchPad.StartTile);

 				if (toRemove is not null)
				{
					toRemove.Active = false;

					if (shouldSync)
						toRemove.NetSync(subworldId);
				}
			}
 		}

		public static void ClearAllLaunchPads(bool announce = true, bool shouldSync = true)
		{
			if (announce)
				Utility.Chat("Cleared all launch pads!", Color.Green);

			foreach(var lpKvp in launchPadStorage)
			{
				foreach (var lp in lpKvp.Value)
				{
					lp.Active = false;

					if (shouldSync)
						lp.NetSync(lpKvp.Key);
				}
			}
		}

		public static bool Any(string subworldId) => GetLaunchPads(subworldId).Any();
		public static bool None(string subworldId) => !Any(subworldId);


		public static List<LaunchPad> GetLaunchPads(string subworldId)
		{
			if (launchPadStorage.ContainsKey(subworldId))
				return launchPadStorage[subworldId];

			return new List<LaunchPad>();
		}

		public static LaunchPad GetLaunchPadAtTileCoordinates(string subworldId, Point16 tile)
		{
			return GetLaunchPads(subworldId).FirstOrDefault(lp =>
			{
				Rectangle coordinates = new(lp.StartTile.X, lp.StartTile.Y, lp.EndTile.X - lp.StartTile.X + 2, lp.EndTile.Y - lp.StartTile.Y + 2);
				return coordinates.Contains(tile.X, tile.Y);
			});
		}

		public static bool TryGetLaunchPadAtTileCoordinates(string subworldId, Point16 tile, out LaunchPad launchPad)
		{
			launchPad = GetLaunchPadAtTileCoordinates(subworldId, tile);
			return launchPad != null;
		}

		public static LaunchPad GetLaunchPadAtStartTile(string subworldId, Point16 startTile)
			=> GetLaunchPads(subworldId).FirstOrDefault(lp => lp.StartTile == startTile);

		public static bool TryGetLaunchPadAtStartTile(string subworldId, Point16 startTile, out LaunchPad launchPad)
		{
			launchPad = GetLaunchPadAtStartTile(subworldId, startTile);
			return launchPad != null;
		}

		private int checkTimer;
		public override void PostUpdateNPCs()
		{
			UpdateLaunchPads();
		}

		private void UpdateLaunchPads()
		{
			checkTimer++;

			if (checkTimer >= 10)
			{
				checkTimer = 0;

				if (launchPadStorage.ContainsKey(MacrocosmSubworld.CurrentWorld))
				{
					for (int i = 0; i < launchPadStorage[MacrocosmSubworld.CurrentWorld].Count; i++)
					{
						var launchPad = launchPadStorage[MacrocosmSubworld.CurrentWorld][i];

						if (!launchPad.Active)
						{
							launchPadStorage[MacrocosmSubworld.CurrentWorld].RemoveAt(i);
							i--;
						}
						else
						{
							launchPad.Update();
						}
					}
				}
			}
		}

		public override void PostDrawTiles()
		{
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, null, null, null, Main.GameViewMatrix.ZoomMatrix);

			if (launchPadStorage.ContainsKey(MacrocosmSubworld.CurrentWorld))
 				foreach (LaunchPad launchPad in launchPadStorage[MacrocosmSubworld.CurrentWorld])
 					launchPad.Draw(Main.spriteBatch, Main.screenPosition);
 
			Main.spriteBatch.End();
		}

		public override void ClearWorld()
		{
			launchPadStorage.Clear();
		}

		public override void SaveWorldData(TagCompound tag) => SaveLaunchPads(tag);

		public override void LoadWorldData(TagCompound tag) => LoadLaunchPads(tag);
			
		public static void SaveLaunchPads(TagCompound tag)
		{
			foreach (var lpKvp in launchPadStorage)
				tag[lpKvp.Key] = lpKvp.Value;
		}

		public static void LoadLaunchPads(TagCompound tag)
		{
			foreach (var lpKvp in tag)
				launchPadStorage[lpKvp.Key] = (List<LaunchPad>)tag.GetList<LaunchPad>(lpKvp.Key);
 		}
	}
}
