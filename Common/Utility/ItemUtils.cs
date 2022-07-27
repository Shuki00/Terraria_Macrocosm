﻿using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace Macrocosm.Common.Utility {
    public static class ItemUtils {

        /// <summary>
        /// Helper function that converts the first rocket ammo found in the inventory 
        /// to the projectile ID generated by rocket ammo of a vanilla launcher 
        /// (either Grenade, Rocket or Mine)
        /// </summary>
        /// <param name="player"> The player using the picked rocket ammo </param>
        /// <param name="copyWeaponType"> The launcher type to copy </param>
        /// <returns> The projectile ID, defaults to Rocket I </returns>
        public static int ToRocketProjectileID(Player player, int copyWeaponType) {

            if (copyWeaponType != ItemID.GrenadeLauncher && copyWeaponType != ItemID.RocketLauncher && copyWeaponType != ItemID.ProximityMineLauncher)
                return ProjectileID.RocketI;

            Item launcher = new(copyWeaponType);
            Item ammo = player.ChooseAmmo(launcher);

            int type;

            // for mini nukes, liquid rockets
            if (TryFindingSpecificMatches(copyWeaponType, ammo.type, out int pickedProjectileId)) {
                type = pickedProjectileId;
            }
            // for rockets I to IV
            else if (ammo.ammo == AmmoID.Rocket) {
                type = launcher.shoot + ammo.shoot;
            }
            else {
                type = ProjectileID.RocketI;
            }

            return type;

        }

        /// <summary>
        /// Copied private method from vanilla 
        /// </summary>
        private static bool TryFindingSpecificMatches(int launcher, int ammo, out int pickedProjectileId) {
            pickedProjectileId = 0;
            return AmmoID.Sets.SpecificLauncherAmmoProjectileMatches.TryGetValue(launcher, out Dictionary<int, int> value) && value.TryGetValue(ammo, out pickedProjectileId);
        }

    }
}
