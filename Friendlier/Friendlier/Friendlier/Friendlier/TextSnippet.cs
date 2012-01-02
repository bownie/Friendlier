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
        public int m_snippetFactoryPosition = 0;

        public TextSnippet()
        {
            m_snippetFactoryPosition = -1;
        }

        public TextSnippet(int position)
        {
            m_snippetFactoryPosition = position;
        }
    }
}
