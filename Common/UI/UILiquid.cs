﻿using Macrocosm.Common.DataStructures;
using Macrocosm.Common.Graphics;
using Macrocosm.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace Macrocosm.Common.UI
{
    public class UILiquid : UIElement
    {
        private readonly Asset<Texture2D> texture;
        private Rectangle surfaceSourceRectangle;
        private Rectangle fillSourceRectangle;

        private /*const*/ readonly int sliceSize = 1;
        private /*const*/ readonly int surfaceSliceHeight = 3;
        private int cornerSize = 10;

        public bool DrawPanel { get; set; } = true;
        public float LiquidLevel { get; set; } = 0f;
        public float WaveFrequency { get; set; } = 5f;
        public float WaveAmplitude { get; set; } = 0.1f;

        /// <summary>
        /// Use <see cref="WaterStyleID"/>!
        /// </summary>
        public UILiquid(int liquidId)
        {
            texture = LiquidRenderer.Instance._liquidTextures[liquidId];
            surfaceSourceRectangle = new(16, 1280, sliceSize * 2, surfaceSliceHeight);
            fillSourceRectangle = new(16, 64 - 1, sliceSize, sliceSize);
            OverflowHidden = true;
        }

        public UILiquid() : this(0)
        {
            texture = ModContent.Request<Texture2D>("Macrocosm/Content/Liquids/Oil");
        }

        SpriteBatchState state;
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            LiquidLevel = Utility.PositiveSineWave(800);

            if (Parent.OverflowHidden)
            {
                state.SaveState(spriteBatch);
                spriteBatch.End();
                spriteBatch.Begin(state.SpriteSortMode, BlendState.AlphaBlend, SamplerState.PointClamp, state.DepthStencilState, CustomRasterizerStates.ScissorTest, state.Effect, state.Matrix);
            }

            Rectangle fillArea = GetFillArea();
            DrawWaves(spriteBatch, fillArea);

            if (Parent.OverflowHidden)
            {
                spriteBatch.End();
                spriteBatch.Begin(state);
            }
        }

        private void DrawWaves(SpriteBatch spriteBatch, Rectangle fillArea)
        {
            float time = (float)Main.gameTimeCache.TotalGameTime.TotalSeconds;

            float secondaryWaveAmplitude = WaveAmplitude * 0.5f;
            float secondaryWaveFrequency = WaveFrequency * 1.5f;
            float tertiaryWaveAmplitude = WaveAmplitude * 0.25f;
            float tertiaryWaveFrequency = WaveFrequency * 2f;
            float quaternaryWaveAmplitude = WaveAmplitude * 0.15f;
            float quaternaryWaveFrequency = WaveFrequency * 2f;

            int radius = cornerSize;
            Rectangle dims = GetDimensions().ToRectangle();

            for (int x = 0; x < fillArea.Width; x += sliceSize)
            {
                float primaryWaveOffset = (float)Math.Sin(time * WaveFrequency + x * 0.1f) * WaveAmplitude;
                float secondaryWaveOffset = (float)Math.Sin(time * secondaryWaveFrequency + x * 0.1f) * secondaryWaveAmplitude;
                float tertiaryWaveOffset = (float)Math.Sin(time * tertiaryWaveFrequency + x * 0.05f) * tertiaryWaveAmplitude;
                float quaternaryWaveOffset = (float)Math.Sin(time * quaternaryWaveFrequency + x * 0.15f) * quaternaryWaveAmplitude;
                float totalWaveOffset = primaryWaveOffset + secondaryWaveOffset + tertiaryWaveOffset + quaternaryWaveOffset;

                float waveTop = fillArea.Top + totalWaveOffset;
                waveTop = Math.Max(waveTop, CalculateCornerYOffsetTop(radius, x, dims.Width, dims.Top));

                spriteBatch.Draw(texture.Value, new Vector2(fillArea.X + x, waveTop - surfaceSliceHeight), surfaceSourceRectangle, Color.White);

                int fillBottom = CalculateCornerYOffsetBottom(radius, x, dims.Width, dims.Bottom);
                int waveFillHeight = fillBottom - (int)waveTop;
                if (waveFillHeight > 0)
                {
                    spriteBatch.Draw(texture.Value, new Rectangle(fillArea.X + x, (int)waveTop, sliceSize, waveFillHeight), fillSourceRectangle, Color.White);
                }
            }
        }
        private int CalculateCornerYOffsetTop(int radius, int x, int fullWidth, int topYOffset)
        {
            if (x < radius)
            {
                int dx = radius - x;  
                int dy = (int)Math.Sqrt(radius * radius - dx * dx);  
                return (topYOffset + radius) - dy;  
            }
            else if (x >= fullWidth - radius)
            {
                int dx = x - (fullWidth - radius); 
                int dy = (int)Math.Sqrt(radius * radius - dx * dx); 
                return (topYOffset + radius) - dy;  
            }

            return topYOffset;
        }

        private int CalculateCornerYOffsetBottom(int radius, int x, int fullWidth, int bottomYOffset)
        {
            if (x < radius)
            {
                int dx = radius - x;
                int dy = radius - (int)Math.Sqrt(radius * radius - dx * dx);
                return bottomYOffset - dy;
            }
            else if (x >= fullWidth - radius)
            {
                int dx = x - (fullWidth - radius);
                int dy = radius - (int)Math.Sqrt(radius * radius - dx * dx);
                return bottomYOffset - dy;
            }
            return bottomYOffset;
        }

        private Rectangle GetFillArea()
        {
            Rectangle baseArea = GetDimensions().ToRectangle();
            int fluidHeight = (int)(baseArea.Height * LiquidLevel);
            return new Rectangle(baseArea.X, baseArea.Y + baseArea.Height - fluidHeight, baseArea.Width, fluidHeight);
        }
    }
}
