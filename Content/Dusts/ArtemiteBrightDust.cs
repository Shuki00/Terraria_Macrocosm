using Macrocosm.Common.Utils;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Macrocosm.Content.Dusts
{
    public class ArtemiteBrightDust : ModDust
    {
        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.rotation += dust.dustIndex % 2 == 0 ? 0.5f : -0.5f;

            float scale = dust.scale;
            if (scale > 1f)
                scale = 1f;

            if (!dust.noLight)
                Lighting.AddLight(dust.position, new Color(177, 230, 204).ToVector3() * scale);

            if (dust.noGravity)
                dust.velocity *= 0.92f;
            else
                dust.velocity.Y += 0.1f;

            if (dust.customData != null && dust.customData is Player player)
                dust.position += player.position - player.oldPosition;

            dust.scale -= 0.03f;
            if (dust.scale < 0.2f)
                dust.active = false;

            return false;
        }

        public override bool MidUpdate(Dust dust) => true;

        public override Color? GetAlpha(Dust dust, Color lightColor)
            => Color.White.WithAlpha(127);
    }
}