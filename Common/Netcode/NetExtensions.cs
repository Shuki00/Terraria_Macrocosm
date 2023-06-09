﻿using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Chat;
using Terraria.ModLoader.IO;

namespace Macrocosm.Common.Netcode
{
	/// <summary> Code by sucss & Feldy @ PellucidMod </summary>
	public static class NetExtensions
	{
		/// <summary>
		/// Returns the <see cref="FieldInfo"/> of every field of <c>this</c> that has the <see cref="NetSyncAttribute"/>.
		/// </summary>
		public static FieldInfo[] GetNetSyncFields(this object obj) => obj.GetType().GetFields()
									.Where(fieldInfo => fieldInfo.GetCustomAttribute<NetSyncAttribute>() is not null)
									.OrderBy(fieldInfo => fieldInfo.Name).ToArray();

		/// <summary>
		/// Writes all the fields of <c>this</c> that have the <see cref="NetSyncAttribute"/> to the <see cref="BinaryWriter"/>. 
		/// If the <see cref="BitWriter"/> is not null, boolean fields are written to it instead.
		/// </summary>
		/// <returns><c>true</c> if all fields were written succesfully else <c>false</c>.</returns>
		public static bool NetWriteFields(this object obj, BinaryWriter binaryWriter, BitWriter bitWriter = null)
		{
			foreach (FieldInfo fieldInfo in obj.GetNetSyncFields())
			{
				string fieldType = fieldInfo.FieldType.Name;

				if (fieldType == "Vector2")
				{
					binaryWriter.WriteVector2((Vector2)fieldInfo.GetValue(obj));
				}
				else if (fieldType == "bool" && bitWriter is not null)
				{
					bitWriter.WriteBit((bool)fieldInfo.GetValue(obj));
				}
				else
				{
					MethodInfo methodInfo = typeof(BinaryWriter).GetMethod("Write", new Type[] { fieldInfo.FieldType });
					if (methodInfo is not null)
					{
						methodInfo.Invoke(binaryWriter, new object[] { fieldInfo.GetValue(obj) });
					}
					else
					{
						Macrocosm.Instance.Logger.Warn(Terraria.Localization.NetworkText.FromLiteral($"{obj.GetType().FullName}: Couldn't write NetSync field \"{fieldInfo.Name}\" value with type <{fieldInfo.FieldType.Name}>."));
						ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral($"{obj.GetType().FullName}: Couldn't write NetSync field \"{fieldInfo.Name}\" value with type <{fieldInfo.FieldType.Name}>."), Color.Red);
						return false;
					}
				}
			}
			
			return true;
		}

		/// <summary>
		/// Reads all the fields of <c>this</c> that have the <see cref="NetSyncAttribute"/> from the <see cref="BinaryReader"/>. 
		/// If the <see cref="BitReader"/> is not null, boolean fields are read from it instead.
		/// </summary>
		/// <param name="reader"></param>
		public static void NetReadFields(this object obj, BinaryReader binaryReader, BitReader bitReader = null)
		{
			foreach (FieldInfo fieldInfo in obj.GetNetSyncFields())
			{
				string fieldType = fieldInfo.FieldType.Name;

				if (fieldType == "Vector2")
				{
 					fieldInfo.SetValue(obj, binaryReader.ReadVector2());
				}
 				else if (fieldType == "bool" && bitReader is not null)
				{
 					fieldInfo.SetValue(obj, bitReader.ReadBit());	
				}
				else
				{
 					fieldInfo.SetValue(obj, typeof(BinaryReader).GetMethod($"Read{fieldInfo.FieldType.Name}").Invoke(binaryReader, null)); 	
				}
			}
		}
	}
}