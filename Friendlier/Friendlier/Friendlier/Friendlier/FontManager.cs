using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Xyglo
{
    static class FontManager
    {
        public static Microsoft.Xna.Framework.Content.ContentManager m_contentManager;

        public static void initialise(Microsoft.Xna.Framework.Content.ContentManager contentManager, string fontFamily)
        {
            m_fontFamily = fontFamily;
            m_contentManager = contentManager;

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
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load font : " + e.ToString());
            }
        }

        public static string m_fontFamily;

        public static SpriteFont m_smallWindowFont;
        public static SpriteFont m_windowFont;
        public static SpriteFont m_fullScreenFont;
        public static SpriteFont m_overlayFont;

        public static SpriteFont getWindowFont()
        {
            return m_windowFont;
        }

        public static SpriteFont getFullScreenFont()
        {
            return m_fullScreenFont;
        }

        public static SpriteFont getOverlayFont()
        {
            return m_overlayFont;
        }

        public static SpriteFont getSmallWindowFont()
        {
            return m_smallWindowFont;
        }
    }
}
