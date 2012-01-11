﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    /// <summary>
    /// Define a line and character position in a file
    /// </summary>
    public struct FilePosition : ICloneable
    {
        public FilePosition(int x, int y)
        {
            X = x;
            Y = x;
        }

        public FilePosition(FilePosition p)
        {
            X = p.X;
            Y = p.Y;
        }

        public object Clone()
        {
            FilePosition copy = new FilePosition(this.X, this.Y);
            return copy;
        }

        public FilePosition(Vector2 vector)
        {
            X = Convert.ToInt16(vector.X);
            Y = Convert.ToInt16(vector.Y);
        }


        public int X;
        public int Y;
    }

    /// <summary>
    /// Open and buffer a file and provide an interface for handling large files efficiently
    /// </summary>
    public class FileBuffer
    {
        public string m_filename;
        public string m_shortName;

        List<string> m_lines = new List<string>();
        List<Command> m_commands = new List<Command>();

        /// <summary>
        /// Position in the undo/redo stack
        /// </summary>
        int m_undoPosition = 0;

        /// <summary>
        /// Number of lines we keep in memory
        /// </summary>
        int m_lineLimit = 500;

        public FileBuffer(string filename)
        {
            m_filename = filename;

            // Convert all back slashes to forward ones just in case we're still using them
            //
            m_shortName = filename.Replace('\\', '/');

            int position = m_shortName.LastIndexOf('/') + 1;
            m_shortName = m_shortName.Substring(position, m_shortName.Length - position);

            // Load and buffer the file
            //
            loadFile();
        }
        
        public string getShortFileName()
        {
            return m_shortName;
        }

        protected void loadFile()
        {
            // Check for file existing
            //
            using (StreamReader sr = new StreamReader(m_filename))
            {
                string line;
                while ((line = sr.ReadLine()) != null && m_lines.Count < m_lineLimit)
                {
                    // Do some clipping here if we need it
                    //line = line.Substring(0, 80);

                    m_lines.Add(line);
                }
            }
        }

        /// <summary>
        /// Gets the content of an existing line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string getLine(int line)
        {
            return m_lines[line];
        }

        /// <summary>
        /// Sets the text value of an existing line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        public void setLine(int line, string value)
        {
            m_lines[line] = value;
        }

        /// <summary>
        /// Appends to an existing line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        public void appendLine(int line, string value)
        {
            m_lines[line] += value;
        }

        /// <summary>
        /// Inserts a line at a given position in the list
        /// </summary>
        /// <param name="line"></param>
        public void insertLine(int line, string value)
        {
            m_lines.Insert(line, value);
        }

        public bool deleteLines(int startLine, int number)
        {
            if (startLine > m_lines.Count || startLine + number > m_lines.Count)
            {
                return false;
            }

            if (number > 0)
            {
                m_lines.RemoveRange(startLine, number);
            }
            else
            {
                m_lines.RemoveAt(startLine);
            }

            return true;
        }

        public int getLineCount()
        {
            return m_lines.Count();
        }

        public bool isTooBig()
        {
            FileInfo fI = new FileInfo(m_filename);
            if ( fI.Length > 5000 )
            {
                Console.WriteLine("File " + m_filename + " too big for editing");
                return true;
            }
            return false;
        }


        /// <summary>
        /// If we have done a new command and the undo stack extends above the current undo position then remove
        /// everything on the undo stack above current position and push new command on at that level.
        /// </summary>
        protected void tidyUndoStack(Command command)
        {
            if (m_undoPosition < m_commands.Count)
            {
                Console.WriteLine("Clearing undo position");
                for (int i = m_undoPosition; i < m_commands.Count; i++)
                {
                    m_commands[i].Dispose();
                }

                m_commands.RemoveRange(m_undoPosition, m_commands.Count - m_undoPosition);
            }

            // Push onto the command stack and ensure that undo position is current
            //
            m_commands.Add(command);
            m_undoPosition = m_commands.Count;

        }

        /// <summary>
        /// Deletes a selection from a FileBuffer - returns true if it actually deleted something.
        /// </summary>
        /// <param name="startSelection"></param>
        /// <param name="endSelection"></param>
        /// <returns></returns>
        public bool deleteSelection(FilePosition startSelection, FilePosition endSelection)
        {
            //Console.WriteLine("DELETE SELECTION");
            DeleteTextCommand command = new DeleteTextCommand("Delete Selection", this, startSelection, endSelection);
            command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return true;
        }

        /// <summary>
        /// Insert some text into our current FileBuffer at a particular position
        /// </summary>
        /// <param name="insertPosition"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool insertText(FilePosition insertPosition, string text)
        {
            InsertTextCommand command = new InsertTextCommand("Insert Text", this, insertPosition, text);
            command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return true;
        }

        public bool undo(int steps)
        {
            if (m_commands.Count >= steps && m_undoPosition >= 0)
            {
                // Unwind the commands in order
                //
                for (int i = m_undoPosition - 1; i > m_undoPosition - 1 - steps; i--)
                {
                    m_commands[i].undoCommand();
                }

                // Reduce the m_undoPosition accordingly
                //
                m_undoPosition -= steps;

                Console.WriteLine("UNDO POSITION = " + m_undoPosition);
                return true;
            }
            else
            {
                throw new Exception("FileBuffer::undo() - not enough steps to undo");
            }
        }

        /// <summary>
        /// Has this buffer been modified?
        /// </summary>
        /// <returns></returns>
        public bool isModified()
        {
            return (m_undoPosition != 0);
        }

    }
}

