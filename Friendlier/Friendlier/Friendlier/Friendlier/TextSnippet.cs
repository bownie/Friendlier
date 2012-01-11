using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    public class TextSnippet
    {
        public List<string> m_lines = new List<string>();
        public FilePosition m_startPoint;
        public int m_linesDeleted = 0;
        protected int m_snippetFactoryPosition = 0;

        public TextSnippet()
        {
            m_snippetFactoryPosition = -1;
        }

        public TextSnippet(int position)
        {
            m_snippetFactoryPosition = position;
        }

        public int getSnippetFactoryPosition()
        {
            return m_snippetFactoryPosition;
        }

        public void setSnippetFactoryPosition(int position)
        {
            m_snippetFactoryPosition = position;
        }


        /*
        /// <summary>
        /// Clear down this TextSnippet ready for re-use
        /// </summary>
        public void clear()
        {
            m_lines.Clear();
            m_snippetFactoryPosition = -1;
            m_linesDeleted = 0;
            m_startPoint.X = 0;
            m_startPoint.Y = 0;
        }
         * */


        /// <summary>
        /// Set this snippet to a single line - remove any extra lines we've already stored in it
        /// </summary>
        /// <param name="line"></param>
        public void setSnippetSingle(string line)
        {
            switch (m_lines.Count())
            {
                case 0:
                    m_lines.Add(line);
                    break;

                case 1:
                    m_lines[0] = line;
                    break;

                default:
                    m_lines[0] = line;
                    m_lines.RemoveRange(1, m_lines.Count - 1);
                    break;
            }
        }

    }
}
