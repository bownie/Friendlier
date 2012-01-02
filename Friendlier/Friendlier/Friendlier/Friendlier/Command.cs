using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    public abstract class Command : IDisposable
    {
        public Command() 
        {
        }

        public Command(string name)
        {
            m_name = name;
        }

        protected string m_name;

        /// <summary>
        /// Define an abstract command for (re)do
        /// </summary>
        public abstract void doCommand();

        /// <summary>
        /// Define an asbtract command for undo
        /// </summary>
        public abstract void undoCommand();

        public abstract void Dispose();

        public void positionOrder()
        {
            FilePosition swap;

            // Swap the end points if start is greater than end - gives us a predictable way of
            // handling text editing.
            //
            if (m_startPos.Y > m_endPos.Y || (m_startPos.Y == m_endPos.Y && (m_startPos.X > m_endPos.X)))
            {
                swap = m_startPos;
                m_startPos = m_endPos;
                m_endPos = swap;
            }
        }

        protected FilePosition m_startPos;
        protected FilePosition m_endPos;
    }
}
