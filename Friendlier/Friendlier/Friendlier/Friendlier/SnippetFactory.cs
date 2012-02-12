using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    /// <summary>
    /// Generate and maintain a heap of Snippets which we use to store text for our undo stack
    /// </summary>
    public static class SnippetFactory
    {
        public static List<TextSnippet> m_snippetList = new List<TextSnippet>();
        public static int m_currentSnippet = 0;
        public static int m_growSnippets = 256;
        public static int m_maxSnippets = 0;

        public static void initialise()
        {
#if SNIPPET_FACTORY_DEBUG
            Logger.logMsg("SnippetFactory():initialise() - growing snippet factory by " + m_growSnippets);
#endif
            // (Re)initialise 
            //
            for (int i = 0; i < m_growSnippets; i++)
            {
                m_snippetList.Add(new TextSnippet()); // Default m_snippetFactoryPosition is -1 (unused)
            }

            // Set m_maxSnippets
            //
            m_maxSnippets = m_snippetList.Count();
        }

        public static TextSnippet getSnippet()
        {
#if SNIPPET_FACTORY_DEBUG
            Logger.logMsg("SnippetFactory():getSnippet() - getting snippet from factory");
#endif

            // Grow if we need to
            //
            if (m_currentSnippet == 0 || m_currentSnippet >= m_maxSnippets)
            {
                initialise();
            }

            // Reset the internal index of this before sending it out for use - this ensures that
            // when it comes back to the heap we know where to re-use it.
            //
            m_snippetList[m_currentSnippet].setSnippetFactoryPosition(m_currentSnippet);

#if SNIPPET_FACTORY_DEBUG
            Logger.logMsg("SnippetFactory():getSnippet() - setting current position of snippet to " + m_currentSnippet);
#endif

            return m_snippetList[m_currentSnippet++];
        }

        /*
        private static bool Equals(TextSnippet snippet)
        {
            return (i < 5);
        }

        */

        /// <summary>
        /// Take the returned snippet and put it back on the pile to be reused
        /// </summary>
        /// <param name="snippet"></param>
        public static void returnSnippet(TextSnippet snippet)
        {
#if SNIPPET_FACTORY_DEBUG
            Logger.logMsg("SnippetFactory():returnSnippet - returning snippet to the heap - position = " + m_snippetList[snippet.getSnippetFactoryPosition()].getSnippetFactoryPosition());
#endif
            
            // Clear the snippet and ensure all the counters are zeroed
            //
            snippet.clear();

            // Remove from from
            m_snippetList.RemoveAt(snippet.getSnippetFactoryPosition());

            // Reposition index and reinsert at end
            snippet.setSnippetFactoryPosition(-1);
            m_snippetList.Add(snippet);

            // Decrement current snippet number
            //
            m_currentSnippet--;

#if SNIPPET_FACTORY_DEBUG
            Logger.logMsg("SnippetFactory():returnSnippet - decremented snippet position = " + m_currentSnippet);
#endif
        }
    }
}
