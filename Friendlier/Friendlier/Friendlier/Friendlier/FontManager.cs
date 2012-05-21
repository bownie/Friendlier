using System;
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

        /// <summary>
        /// Current font state selected - not sure if we use this at the moment
        /// </summary>
        protected FontType m_state;

        /// <summary>
        /// The font processor we're using
        /// </summary>
        protected string m_processor = "";

        /// <summary>
        /// The content manager
        /// </summary>
        protected Microsoft.Xna.Framework.Content.ContentManager m_contentManager;

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
        /// Overlay font is the font used for HUD - small screen is just a smaller size
        /// </summary>
        protected SpriteFont m_overlayFontSmallScreen;

        /// <summary>
        /// Overlay font is the font used for HUD
        /// </summary>
        protected SpriteFont m_overlayFontBigScreen;

        /// <summary>
        /// The aspect ratio of our screen
        /// </summary>
        protected float m_aspectRatio;


        /// <summary>
        /// Initialise the font manager with some basic details
        /// </summary>
        /// <param name="contentManager"></param>
        /// <param name="fontFamily"></param>
        /// <param name="aspectRatio"></param>
        public void initialise(Microsoft.Xna.Framework.Content.ContentManager contentManager, string fontFamily, float aspectRatio, string processor = "")
        {
            m_fontFamily = fontFamily;
            m_contentManager = contentManager;
            m_aspectRatio = aspectRatio;
            m_processor = processor;

            // If we have passed a processor then append this to the name
            if (processor != "")
            {
                processor += "_";
            }

            Logger.logMsg("FontManager.initialise() - attempting to load fonts");
            try
            {
                m_smallWindowFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_smallWindow");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_smallWindow");

                m_windowFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_window");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_window");

                m_fullScreenFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_fullScreen");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_fullScreen");

                m_overlayFontSmallScreen = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_overlaySmallScreen");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_overlaySmallScreen");

                m_overlayFontBigScreen = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_overlayBigScreen");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_overlayBigScreen");

                // Set default characters
                //
                m_smallWindowFont.DefaultCharacter = ' ';
                m_windowFont.DefaultCharacter = ' ';
                m_fullScreenFont.DefaultCharacter = ' ';
                m_overlayFontSmallScreen.DefaultCharacter = ' ';
                m_overlayFontBigScreen.DefaultCharacter = ' ';

            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load font : " + e);
            }
        }

        /// <summary>
        /// Return the overlay font
        /// </summary>
        /// <returns></returns>
        public SpriteFont getOverlayFont()
        {
            return (m_isSmallScreen == true ? m_overlayFontSmallScreen : m_overlayFontBigScreen);
        }

        /// <summary>
        /// Is the screen we're attached to small or big (decides Overlay Font size)
        /// </summary>
        protected bool m_isSmallScreen = false;

        /// <summary>
        /// Are we on a small screen regards overlay font size?
        /// </summary>
        /// <returns></returns>
        public bool isSmallScreen()
        {
            return m_isSmallScreen;
        }

        /// <summary>
        /// Set the small screen variable
        /// </summary>
        /// <param name="smallScreen"></param>
        public void setSmallScreen(bool smallScreen)
        {
            m_isSmallScreen = smallScreen;
        }

        /// <summary>
        /// Font manager needs to know where we're zoomed out to decide on best font to pick
        /// </summary>
        protected float m_zoomLevel = 500.0f;

        /// <summary>
        /// Font manager needs to know if we are full screen or not
        /// </summary>
        protected bool m_fullScreen = false;

        /// <summary>
        /// Set the current screen state 
        /// </summary>
        /// <param name="zoomLevel"></param>
        /// <param name="fullScreen"></param>
        public void setScreenState(float zoomLevel, bool fullScreen)
        {
            m_zoomLevel = zoomLevel;
            m_fullScreen = fullScreen;

            if (!m_fullScreen)
            {
                if (m_zoomLevel == 500.0f)
                {
                    m_state = FontType.Window;
                    Logger.logMsg("FontManager::setScreenState() - setting FontType.Window");

                }
                else 
                {
                    m_state = FontType.Small;
                    Logger.logMsg("FontManager::setScreenState() - setting FontType.Small");
                }
            }
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
        /// Get the Line spacing of the selected font - gap plus font height
        /// </summary>
        /// <returns></returns>
        public float getLineSpacing()
        {
            if (m_processor.ToLower() == "nuclex")
            {
                return getTextScale() * getFont().LineSpacing * 1.12f; // Kludge for Nuclex issue 
            }

            return getTextScale() * getFont().LineSpacing;
            //return getTextScale() * getFont().MeasureString("X").Y;
        }

        /// <summary>
        /// Character height
        /// </summary>
        /// <returns></returns>
        public float getCharHeight()
        {
            return getTextScale() * getFont().MeasureString("X").Y;
        }

        /// <summary>
        /// Get the line height of a specific font type - gap plus font height
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public float getLineSpacing(FontType type)
        {
            switch (type)
            {
                case FontType.Small:
                    return getTextScale() * m_smallWindowFont.LineSpacing;
                    //return getTextScale() * m_smallWindowFont.MeasureString("X").Y;

                case FontType.Window:
                    return getTextScale() * m_windowFont.LineSpacing;
                    //return getTextScale() * m_windowFont.MeasureString("X").Y;

                case FontType.Full:
                    return getTextScale() * m_fullScreenFont.LineSpacing;
                    //return getTextScale() * m_fullScreenFont.MeasureString("X").Y;

                case FontType.Overlay:
                    return  getOverlayFont().LineSpacing;
                    //return m_overlayFont.MeasureString("X").Y;

                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Get the width of a given font character
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
                    return getOverlayFont().MeasureString("X").X;

                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Line spacing - not the same as Line Height of course
        /// </summary>
        /// <returns></returns>
        public float getDefaultLineSpacing()
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
                case FontType.Window:
                case FontType.Full:
                    return 8.0f / getDefaultLineSpacing() * m_aspectRatio;

                default:
                    return 0.0f;
            }
        }
    }
}
