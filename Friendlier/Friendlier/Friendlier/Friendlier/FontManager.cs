using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Xyglo
{
    public class FontManager
    {
        public enum FontType
        {
            Small,
            Window,
            Full,
            Overlay
        }

        protected FontType m_state;

        protected Microsoft.Xna.Framework.Content.ContentManager m_contentManager;

        /// <summary>
        /// Initialise the font manager with some basic details
        /// </summary>
        /// <param name="contentManager"></param>
        /// <param name="fontFamily"></param>
        /// <param name="aspectRatio"></param>
        public void initialise(Microsoft.Xna.Framework.Content.ContentManager contentManager, string fontFamily, float aspectRatio)
        {
            m_fontFamily = fontFamily;
            m_contentManager = contentManager;
            m_aspectRatio = aspectRatio;

            Logger.logMsg("FontManager.initialise() - attempting to load fonts");
            try
            {
                m_smallWindowFont = m_contentManager.Load<SpriteFont>(fontFamily + "_smallWindow");
                Logger.logMsg("FontManager.initialise() - Loaded font " + fontFamily + "_smallWindow");

                m_windowFont = m_contentManager.Load<SpriteFont>(fontFamily + "_window");
                Logger.logMsg("FontManager.initialise() - Loaded font " + fontFamily + "_window");

                m_fullScreenFont = m_contentManager.Load<SpriteFont>(fontFamily + "_fullScreen");
                Logger.logMsg("FontManager.initialise() - Loaded font " + fontFamily + "_fullScreen");

                m_overlayFont = m_contentManager.Load<SpriteFont>(fontFamily + "_overlay");
                Logger.logMsg("FontManager.initialise() - Loaded font " + fontFamily + "_overlay");

                // Set default characters
                //
                m_smallWindowFont.DefaultCharacter = ' ';
                m_windowFont.DefaultCharacter = ' ';
                m_fullScreenFont.DefaultCharacter = ' ';
                m_overlayFont.DefaultCharacter = ' ';

            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load font : " + e.ToString());
            }
        }

        /// <summary>
        /// Our Font Family
        /// </summary>
        protected string m_fontFamily;

        /// <summary>
        /// SpriteFont
        /// </summary>
        protected SpriteFont m_smallWindowFont;

        /// <summary>
        /// SpriteFont
        /// </summary>
        protected SpriteFont m_windowFont;

        /// <summary>
        /// SpriteFont
        /// </summary>
        protected SpriteFont m_fullScreenFont;

        /// <summary>
        /// SpriteFont
        /// </summary>
        protected SpriteFont m_overlayFont;

        /// <summary>
        /// The aspect ratio of our screen
        /// </summary>
        protected float m_aspectRatio;

        /// <summary>
        /// Return the overlay font
        /// </summary>
        /// <returns></returns>
        public SpriteFont getOverlayFont()
        {
            return m_overlayFont;
        }

        /// <summary>
        /// Get the font state we've selected
        /// </summary>
        /// <returns></returns>
        public FontType getFontState()
        {
            return m_state;
        }

        /// <summary>
        /// Set the font state
        /// </summary>
        /// <param name="state"></param>
        public void setFontState(FontType state)
        {
            Logger.logMsg("FontManager:setFontState() - setting font state = " + state.ToString());
            m_state = state;
        }

        /// <summary>
        /// Return the font we have selected
        /// </summary>
        /// <returns></returns>
        public SpriteFont getFont()
        {
            switch(m_state)
            {
                case FontType.Small:
                    return m_smallWindowFont;

                case FontType.Window:
                    return m_windowFont;

                case FontType.Full:
                    return m_fullScreenFont;

                default:
                case FontType.Overlay:
                    return null;
            }
        }

        /// <summary>
        /// Get the character width of the given font
        /// </summary>
        /// <returns></returns>
        public float getCharWidth()
        {
            return getTextScale() * getFont().MeasureString("X").X;
        }


        /// <summary>
        /// Get the Line height of the selected font
        /// </summary>
        /// <returns></returns>
        public float getLineHeight()
        {
            return getTextScale() * getFont().MeasureString("X").Y;
        }

        /// <summary>
        /// Get the line height of a specific font type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public float getLineHeight(FontType type)
        {
            switch (type)
            {
                case FontType.Small:
                    return getTextScale() * m_smallWindowFont.MeasureString("X").Y;

                case FontType.Window:
                    return getTextScale() * m_windowFont.MeasureString("X").Y;

                case FontType.Full:
                    return getTextScale() * m_fullScreenFont.MeasureString("X").Y;

                case FontType.Overlay:
                    return m_overlayFont.MeasureString("X").Y;

                default:
                    return 0.0f;
            }
        }

        public float getCharWidth(FontType type)
        {
            switch (type)
            {
                case FontType.Small:
                    return getTextScale() * m_smallWindowFont.MeasureString("X").X;

                case FontType.Window:
                    return getTextScale() * m_windowFont.MeasureString("X").X;

                case FontType.Full:
                    return getTextScale() * m_fullScreenFont.MeasureString("X").X;

                case FontType.Overlay:
                    return m_overlayFont.MeasureString("X").X;

                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Line spacing - not the same as Line Height of course
        /// </summary>
        /// <returns></returns>
        public float getLineSpacing()
        {
            return (float)(getFont().LineSpacing);
        }

        /// <summary>
        /// For each font size we have to scale according to how we want it to look.  This scale
        /// affects all of the dimensions of the displaying code along with the character width
        /// and line height so we are careful to keep this scale in line at all times.
        /// </summary>
        /// <returns></returns>
        public float getTextScale()
        {
            switch (m_state)
            {
                case FontType.Small:
                    return 8.0f / getLineSpacing() * m_aspectRatio;

                case FontType.Window:
                    return 8.0f / getLineSpacing() * m_aspectRatio;

                case FontType.Full:
                    return 8.0f / getLineSpacing() * m_aspectRatio;

                default:
                    return 0.0f;
            }
        }
    }
}
