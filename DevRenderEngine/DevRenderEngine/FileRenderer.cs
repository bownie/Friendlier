#region File Description
//-----------------------------------------------------------------------------
// FileRenderer.cs
//
// Copyright (C) Xyglo. All rights reserved.
//
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Xyglo
{
    class FileRenderer
    {
        protected String            filename;

        // Our graphics device and the effect we use to render the shapes
        //
        protected GraphicsDevice    graphics;
        protected BasicEffect       effect;

        public FileRenderer(string fnIn)
        {
            filename = fnIn;
        }


        public void Initialize(GraphicsDevice graphicsDevice)
        {
            // If we already have a graphics device, we've already initialized once. We don't allow that.
            if (graphics != null)
                throw new InvalidOperationException("Initialize can only be called once.");

            // Save the graphics device
            graphics = graphicsDevice;

            // Create and initialize our effect
            effect = new BasicEffect(graphicsDevice);
            effect.VertexColorEnabled = true;
            effect.TextureEnabled = false;
            effect.DiffuseColor = Vector3.One;
            effect.World = Matrix.Identity;

            // Create our unit sphere vertices
            //InitializeSphere();

            

        }



        GraphicsDevice graphics_Device;
        RenderTarget2D  rtFont;             // Render Target to render sprite font to  
        Texture3D       shadowMap;
        SpriteBatch spriteBatch;

        void doit()
        {

        }


        /*
        void initSpriteFontToTexture(GraphicsDevice graphics_Device, SpriteBatch sprite_Batch)
        {
            this.graphics_Device = graphics_Device;
            this.sprite_Batch = sprite_Batch;

            RT_Font = new RenderTarget2D(
                graphics_Device,
                graphics_Device.PresentationParameters.BackBufferWidth,
                graphics_Device.PresentationParameters.BackBufferHeight,
                1,
                SurfaceFormat.Color //SurfaceFormat.Single);  
                );
        }

        public Texture2D GetTexture(SpriteFont spriteFont, String text)
        {
            // Set render target for sprite font  
            graphics_Device.SetRenderTarget(0, RT_Font);

            graphics_Device.Clear(Color.TransparentBlack);

            // Render the sprite font  
            string string_SpriteFont = string.Format(text);
            sprite_Batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            sprite_Batch.DrawString(spriteFont, string_SpriteFont, Vector2.Zero, Color.White);
            sprite_Batch.End();

            // Set all render targets to null  
            graphics_Device.SetRenderTarget(0, null);

            return RT_Font.GetTexture();
        }
         * */

    }
}
