using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    /// <summary>
    /// Take a FilePosition and some text and insert it into a FileBuffer at that point.
    /// </summary>
    class InsertTextCommand : Command
    {
        public InsertTextCommand(string name, FileBuffer buffer, FilePosition insertPosition, string text)
        {
            m_name = name;
            m_fileBuffer = buffer;
            m_startPos = insertPosition;
            m_text = text;
        }

        /// <summary>
        /// Do this command
        /// </summary>
        public override void doCommand()
        {
            string fetchLine = m_fileBuffer.getLine(m_startPos.Y);
            m_fileBuffer.setLine(m_startPos.Y, fetchLine.Insert(m_startPos.X, m_text));

            //Console.WriteLine("Writing " + m_text + " (length " + m_text.Length + ")");
        }

        public override void undoCommand()
        {
        }

        public override void Dispose()
        {
            //throw new NotImplementedException();
        }

        string m_text;
        TextSnippet m_snippet = SnippetFactory.getSnippet();
        FileBuffer m_fileBuffer;

    }
}
