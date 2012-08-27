#region File Description
//-----------------------------------------------------------------------------
// FontManager.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework.Graphics;

namespace Xyglo
{
    /// <summary>
    /// Manage our fonts - load them, ensure we're providing the correct one for
    /// the resolution of the screen we're looking at - provider helper functions
    /// to access the fonts and their properties.
    /// </summary>
    public class FontManager
    {
        // Font type enumerator
        //
        public enum FontType
        {
            Micro,
            Tiny,
            Small,
            Medium,
            Large,
            VeryLarge,
            Huge,
            Enormous,
            Overlay
        }


        // --------------------------------- CONSTRUCTORS --------------------------------------
        //
        public FontManager()
        {
        }

        // ------------------------------- MEMBER VARIABLES ------------------------------------
        //

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
        /// Sprite Font micro size
        /// </summary>
        protected SpriteFont m_microFont;

        /// <summary>
        /// Sprite Font tiny size
        /// </summary>
        protected SpriteFont m_tinyFont;

        /// <summary>
        /// SpriteFont
        /// </summary>
        protected SpriteFont m_smallFont;

        /// <summary>
        /// SpriteFont
        /// </summary>
        protected SpriteFont m_mediumFont;

        /// <summary>
        /// SpriteFont
        /// </summary>
        protected SpriteFont m_largeFont;

        /// <summary>
        /// SpriteFont - very large
        /// </summary>
        protected SpriteFont m_veryLargeFont;

        /// <summary>
        /// SpriteFont - huge
        /// </summary>
        protected SpriteFont m_hugeFont;

        /// <summary>
        /// SpriteFont - enormous
        /// </summary>
        protected SpriteFont m_enormousFont;


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

        // -------------------------------- MEMBERS -----------------------------------------
        //

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

                m_microFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_micro");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_micro");

                m_tinyFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_tiny");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_tiny");

                m_smallFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_small");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_small");

                m_mediumFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_medium");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_medium");

                m_largeFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_large");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_large");

                m_veryLargeFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_verylarge");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_verylarge");

                m_hugeFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_huge");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_huge");

                m_enormousFont = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_enormous");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_enormous");

                m_overlayFontSmallScreen = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_overlaySmallScreen");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_overlaySmallScreen");

                m_overlayFontBigScreen = m_contentManager.Load<SpriteFont>(processor + fontFamily + "_overlayBigScreen");
                Logger.logMsg("FontManager.initialise() - Loaded font " + processor + fontFamily + "_overlayBigScreen");

                // Set default characters
                //
                m_smallFont.DefaultCharacter = ' ';
                m_mediumFont.DefaultCharacter = ' ';
                m_largeFont.DefaultCharacter = ' ';
                m_overlayFontSmallScreen.DefaultCharacter = ' ';
                m_overlayFontBigScreen.DefaultCharacter = ' ';

            }
            catch (Exception e)
            {
                Console.WriteLine("FontManager::initialise() - failed to load font : " + e);
                throw (e);
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
                    m_state = FontType.Medium;
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
        /// Return the font we have selected in our font manager - default font for screen size
        /// </summary>
        /// <returns></returns>
        public SpriteFont getDefaultFont()
        {
            switch(m_state)
            {
                case FontType.Tiny:
                    return m_tinyFont;

                case FontType.Micro:
                    return m_microFont;

                case FontType.Small:
                    return m_smallFont;

                case FontType.Medium:
                    return m_mediumFont;

                case FontType.Large:
                    return m_largeFont;

                case FontType.VeryLarge:
                    return m_veryLargeFont;

                case FontType.Huge:
                    return m_hugeFont;

                case FontType.Enormous:
                    return m_enormousFont;

                default:
                case FontType.Overlay:
                    return null;
            }
        }

        /// <summary>
        /// Get a font for a view size
        /// </summary>
        /// <param name="viewSize"></param>
        /// <returns></returns>
        public SpriteFont getViewFont(XygloView.ViewSize viewSize)
        {
            switch (viewSize)
            {
                case XygloView.ViewSize.Micro:
                    return m_microFont;

                case XygloView.ViewSize.Tiny:
                    return m_tinyFont;

                case XygloView.ViewSize.Small:
                    return m_smallFont;

                default:
                case XygloView.ViewSize.Medium:
                    return m_mediumFont;

                case XygloView.ViewSize.Large:
                    return m_largeFont;

                case XygloView.ViewSize.VeryLarge:
                    return m_veryLargeFont;

                case XygloView.ViewSize.Huge:
                    return m_hugeFont;

                case XygloView.ViewSize.Enormous:
                    return m_enormousFont;
            }
        }

        /// <summary>
        /// Get the character width of the given font
        /// </summary>
        /// <returns></returns>
        public float getCharWidth(XygloView.ViewSize viewSize)
        {
            //if (getDefaultFont() == null)
            //{
                //throw new Exception("No font defined");
            //}

            return getTextScale() * getViewFont(viewSize).MeasureString("X").X;
        }


        /// <summary>
        /// Get the Line spacing of the selected font - gap plus font height
        /// </summary>
        /// <returns></returns>
        public float getLineSpacing(XygloView.ViewSize viewSize)
        {
            //if (getDefaultFont() == null)
            //{
                //throw new Exception("No font defined");
            //}

            if (m_processor.ToLower() == "nuclex")
            {
                return getTextScale() * getViewFont(viewSize).LineSpacing * 1.12f; // Kludge for Nuclex issue 
            }

            return getTextScale() * getViewFont(viewSize).LineSpacing;
            //return getTextScale() * getFont().MeasureString("X").Y;
        }

        /// <summary>
        /// Character height
        /// </summary>
        /// <returns></returns>
        public float getCharHeight(XygloView.ViewSize viewSize)
        {
            //if (getFont() == null)
            //{
                //throw new Exception("No font defined");
            //}

            return getTextScale() * getViewFont(viewSize).MeasureString("X").Y;
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
                case FontType.Micro:
                    return getTextScale() * m_microFont.LineSpacing;

                case FontType.Tiny:
                    return getTextScale() * m_tinyFont.LineSpacing;

                case FontType.Small:
                    return getTextScale() * m_smallFont.LineSpacing;
                    //return getTextScale() * m_smallWindowFont.MeasureString("X").Y;

                case FontType.Medium:
                    return getTextScale() * m_mediumFont.LineSpacing;
                    //return getTextScale() * m_windowFont.MeasureString("X").Y;

                case FontType.Large:
                    return getTextScale() * m_largeFont.LineSpacing;
                    //return getTextScale() * m_fullScreenFont.MeasureString("X").Y;

                case FontType.VeryLarge:
                    return getTextScale() * m_veryLargeFont.LineSpacing;

                case FontType.Huge:
                    return getTextScale() * m_hugeFont.LineSpacing;

                case FontType.Enormous:
                    return getTextScale() * m_enormousFont.LineSpacing;

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
                case FontType.Micro:
                case FontType.Tiny:
                case FontType.VeryLarge:
                case FontType.Huge:
                case FontType.Enormous:
                case FontType.Small:
                    return getTextScale() * m_smallFont.MeasureString("X").X;

                case FontType.Medium:
                    return getTextScale() * m_mediumFont.MeasureString("X").X;

                case FontType.Large:
                    return getTextScale() * m_largeFont.MeasureString("X").X;

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
            return (float)(getDefaultFont().LineSpacing);
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
                case FontType.Medium:
                case FontType.Large:
                    return 8.0f / getDefaultLineSpacing() * m_aspectRatio;

                default:
                    return 0.0f;
            }
        }

        /// <summary>
        /// Get the aspect ratio
        /// </summary>
        /// <returns></returns>
        public float getAspectRatio()
        {
            return m_aspectRatio;
        }
    }
}
