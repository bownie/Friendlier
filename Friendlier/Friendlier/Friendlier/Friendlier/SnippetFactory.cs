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
            return m_snippetList[m_currentSnippet++];
        }

        /*
        private static bool Equals(TextSnippet snippet)
        {
            return (i < 5);
        }

        */

        /// <summary>
        /// Take the returned snippet and put it back on the pile to be used
        /// </summary>
        /// <param name="snippet"></param>
        public static void returnSnippet(TextSnippet snippet)
        {
            Logger.logMsg("Returning snippet to the heap : " + m_snippetList[snippet.getSnippetFactoryPosition()].getSnippetFactoryPosition());
            Logger.logMsg("Current snippet position = " + m_currentSnippet);

            // Remove from from
            //m_snippetList.RemoveAt(snippet.getSnippetFactoryPosition());

            // Reposition index and reinsert at end
            //snippet.setSnippetFactoryPosition(-1);
            //m_snippetList.Add(snippet);

            // Decrement current snippet number
            //
            m_currentSnippet--;
        }
    }
}
