using System;
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
            Y = y;
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

        public static bool operator ==(FilePosition a, FilePosition b)
        {
            return ((a.X == b.X) && (a.Y == b.Y));
        }

        public static bool operator !=(FilePosition a, FilePosition b)
        {
            return ((a.X != b.X) || (a.Y != b.Y));
        }

        // Needed to avoid warning
        //
        public override int GetHashCode()
        {
            return X ^ Y;
        }

        // Needed to avoid warning
        //
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
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

        public string getFilepath()
        {
            return m_filename;
        }


        public string m_shortName;

        List<string> m_lines = new List<string>();
        List<Command> m_commands = new List<Command>();

        /// <summary>
        /// Position in the undo/redo stack
        /// </summary>
        int m_undoPosition = 0;

        /// <summary>
        /// Undo watermark is reset when we save a file
        /// </summary>
        int m_undoWatermark = 0;

        /// <summary>
        /// Number of lines we keep in memory
        /// </summary>
        int m_lineLimit = 5000;

        /// <summary>
        /// Default constructor
        /// </summary>
        public FileBuffer()
        {
            m_filename = "";
            m_shortName = "";
        }

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
                Logger.logMsg("File " + m_filename + " too big for editing");
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
                Logger.logMsg("Clearing undo position");
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
        public FilePosition deleteSelection(FilePosition startSelection, FilePosition endSelection)
        {
            DeleteTextCommand command = new DeleteTextCommand("Delete Selection", this, startSelection, endSelection);
            FilePosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return fp;
        }

        /// <summary>
        /// Replace a selection with some text
        /// </summary>
        /// <param name="replacePosition"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public FilePosition replaceText(FilePosition startSelection, FilePosition endSelection, string text)
        {
            ReplaceTextCommand command = new ReplaceTextCommand("Replace Text", this, startSelection, endSelection, text);
            FilePosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return fp;
        }

        /// <summary>
        /// Insert some text into our current FileBuffer at a particular position
        /// </summary>
        /// <param name="insertPosition"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public FilePosition insertText(FilePosition insertPosition, string text)
        {
            InsertTextCommand command = new InsertTextCommand("Insert Text", this, insertPosition, text);
            FilePosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return fp;
        }

        /// <summary>
        /// Insert a new line
        /// </summary>
        /// <param name="insertPosition"></param>
        public FilePosition insertNewLine(FilePosition insertPosition)
        {
            InsertTextCommand command = new InsertTextCommand("Insert new line", this, insertPosition);
            FilePosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return fp;
        }

        /// <summary>
        /// Undo a given number of steps in the life of a FileBuffer
        /// </summary>
        /// <param name="steps"></param>
        /// <returns></returns>
        public FilePosition undo(int steps)
        {
            FilePosition fp = new FilePosition();

            if (m_commands.Count >= steps && m_undoPosition >= 0)
            {
                // Unwind the commands in order
                //
                for (int i = m_undoPosition - 1; i > m_undoPosition - 1 - steps; i--)
                {
                    fp = m_commands[i].undoCommand();
                }

                // Reduce the m_undoPosition accordingly
                //
                m_undoPosition -= steps;

                Logger.logMsg("FileBuffer::undo() - undo position = " + m_undoPosition);
                return fp;
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
            return (m_undoPosition != m_undoWatermark);
        }

        /// <summary>
        /// Save this FileBuffer
        /// </summary>
        /// <returns></returns>
        public void save()
        {
            Logger.logMsg("Starting write file " + m_filename);
            using (StreamWriter sw = new StreamWriter(m_filename))
            {
                foreach (string line in m_lines)
                {
                    sw.WriteLine(line);
                }
            }

            Logger.logMsg("Completed file write");

            // Reset this to make the file of unmodified status but keep the undo stack as is
            //
            m_undoWatermark = m_undoPosition;
        }
    }
}


