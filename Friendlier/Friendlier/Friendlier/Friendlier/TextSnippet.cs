using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    public class TextSnippet
    {
        // Make this public to avoid 
        public List<string> m_lines = new List<string>();

        //protected FilePosition m_startPoint;

        protected int m_linesDeleted = 0;

        public int getLinesDeleted()
        {
            return m_linesDeleted;
        }

        public void setLinesDeleted(int lines)
        {
            m_linesDeleted = lines;
        }

        public void incrementLinesDeleted(int increment)
        {
            m_linesDeleted += increment;
        }

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

        /// <summary>
        /// Clear down this TextSnippet ready for re-use
        /// </summary>
        public void clear()
        {
            m_lines.Clear();
            //m_snippetFactoryPosition = -1;
            m_linesDeleted = 0;
            //m_startPoint.X = 0;
            //m_startPoint.Y = 0;
        }

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

        /// <summary>
        /// Return a line break formatted clipboard string
        /// </summary>
        /// <returns></returns>
        public string getClipboardString()
        {
            string rS = "";
            int i = 0;

            foreach (string line in m_lines)
            {
                // Only append a line feed if we aren't the last line of the snippet
                if (++i != m_lines.Count())
                {
                    rS += line + "\r\n";
                }
                else
                {
                    rS += line;
                }
            }

            return rS;
        }

    }
}
