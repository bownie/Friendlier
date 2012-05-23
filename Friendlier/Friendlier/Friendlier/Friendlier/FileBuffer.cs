using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;

namespace Xyglo
{

    /// <summary>
    /// Open and buffer a file and provide an interface for handling large files efficiently
    /// </summary>
    //[DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class FileBuffer
    {
        //////////// MEMBER VARIABLES ///////////////

        [DataMember]
        public string m_filename;

        [DataMember]
        public string m_shortName;

        /// <summary>
        /// The local buffering of our file lines - we don't want to persist this
        /// </summary>
        List<string> m_lines = new List<string>();
        
        /// <summary>
        /// Our list of commands for undo/redo - don't persist
        /// </summary>
        //[DataMember]
        List<Command> m_commands = new List<Command>();

        /// <summary>
        /// Position in the undo/redo stack - don't persist
        /// </summary>
        //[DataMember]
        protected int m_undoPosition = 0;

        /// <summary>
        /// Undo watermark is reset when we save a file - don't persist
        /// </summary>
        //[DataMember]
        int m_undoWatermark = 0;

        /// <summary>
        /// Number of lines we keep in memory
        /// </summary>
        [DataMember]
        int m_lineLimit = 100000;

        /// <summary>
        /// Is this FileBuffer read only?
        /// </summary>
        [DataMember]
        protected bool m_readOnly = false;

        /// <summary>
        /// Last GameTime that we fetched this file
        /// </summary>
        protected TimeSpan m_lastFetchTime = TimeSpan.Zero;

        /// <summary>
        /// The last System time we fetch this file
        /// </summary>
        protected DateTime m_lastFetchSystemTime = DateTime.MinValue;

        /// <summary>
        /// When this FileBuffer was created
        /// </summary>
        protected DateTime m_creationSystemTime = DateTime.Now;

        /// <summary>
        /// Last time we wrote this file
        /// </summary>
        protected DateTime m_lastWriteSystemTime = DateTime.MinValue;

        /// <summary>
        /// Fetch Window for a File - every second
        /// </summary>
        [IgnoreDataMember]
        protected TimeSpan m_fetchWindow;

        /// <summary>
        /// List of highlight information - this is updated via a SyntaxManager but is retrieved directly
        /// from this class.  We persist this information to avoid having to regenerate it every time we
        /// load the file.
        /// </summary>
        //[DataMember]
        //protected List<Highlight> m_highlightList = new List<Highlight>();

        /// <summary>
        /// Dictionary of highlight information, these are unique by FilePosition key
        /// </summary>
        [DataMember]
        protected Dictionary<FilePosition, Highlight> m_highlightDictionary = new Dictionary<FilePosition, Highlight>();

        /// <summary>
        /// Sortedlist of 
        /// </summary>
        [DataMember]
        protected SortedList<FilePosition, Highlight> m_highlightSortedList = new SortedList<FilePosition, Highlight>();

        /// <summary>
        /// List of highlights we're going to return to the drawFileBuffer in the main loop
        /// </summary>
        protected List<Highlight> m_returnLineList = new List<Highlight>();

        //////////// CONSTRUCTORS ////////////

        /// <summary>
        /// Default constructor
        /// </summary>
        public FileBuffer()
        {
            m_filename = "";
            m_shortName = "";

            // Define Timespan in the default constructor
            //
            m_fetchWindow =  new TimeSpan(0, 0, 0, 1, 0);
        }

        /// <summary>
        /// Construct a FileBuffer
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="readOnly"></param>
        public FileBuffer(string filename, bool readOnly = false)
        {
            m_filename = filename;
            m_readOnly = readOnly;

            // Define Timespan here
            //
            m_fetchWindow =  new TimeSpan(0, 0, 0, 1, 0);

            // Fix the paths properly
            //
            fixPaths();
        }


        /////////////// METHODS ///////////////////

        /// <summary>
        /// Get the full filepath
        /// </summary>
        /// <returns></returns>
        public string getFilepath()
        {
            return m_filename;
        }

        /// <summary>
        /// Set the full file path
        /// </summary>
        /// <param name="filepath"></param>
        public void setFilepath(string filepath)
        {
            m_filename = filepath;
            fixPaths();
        }

        /// <summary>
        /// Fetch the current undo position
        /// </summary>
        /// <returns></returns>
        public int getUndoPosition()
        {
            return m_undoPosition;
        }

        /// <summary>
        /// Fic any backslashes to forward slashes and create short filename
        /// </summary>
        protected void fixPaths()
        {
            // Convert all back slashes to forward ones just in case we're still using them
            //
            m_shortName = m_filename.Replace('\\', '/');

            int position = m_shortName.LastIndexOf('/') + 1;
            m_shortName = m_shortName.Substring(position, m_shortName.Length - position);
        }
        
        /// <summary>
        /// Return the short filename
        /// </summary>
        /// <returns></returns>
        public string getShortFileName()
        {
            return m_shortName;
        }

        /// <summary>
        /// Test to see if the current filename is writeable
        /// </summary>
        /// <returns></returns>
        public bool isWriteable()
        {
            if (File.Exists(m_filename))
            {
                if (File.GetAttributes(m_filename) != FileAttributes.ReadOnly)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Generate the highlighting for this FileBuffer
        /// </summary>
        /// <param name="syntaxmanager"></param>
        public void generateHighlighting(SyntaxManager syntaxManager)
        {
            // We don't want to do this every time we load a file - it should persist the highlight anyway
            //
            syntaxManager.updateHighlighting(this);
        }

        /// <summary>
        /// Load this file - passing a syntax manager to do the necessary syntax processing for it.
        /// </summary>
        public void loadFile(SyntaxManager syntaxManager)
        {
            // test to see if file exists
            if (!File.Exists(m_filename))
            {
                Logger.logMsg("FileBuffer::loadFile() - file \"" + m_filename + "\" does not exist so cannot load it");
                return;
            }

            // If we have recovered this FileBuffer from a persisted state then m_lines could
            // very well be null at this point - initialise it if it is.  Otherwise we clear
            // before we load.
            //
            if (m_lines == null)
            {
                m_lines = new List<string>();
            }
            else
            {
                m_lines.Clear();
            }

            // Open a file non-exclusively for reading
            //
            FileStream fs = new FileStream(m_filename, FileMode.Open, FileAccess.Read, System.IO.FileShare.ReadWrite);
            using (StreamReader sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null && m_lines.Count < m_lineLimit)
                {
                    m_lines.Add(line);
                }
            }
        }

        /// <summary>
        /// We call this when we want to refetch a file for example if we're tailing it
        /// and need a recent copy.   For the moment this is horribly inefficient.
        /// </summary>
        public bool refetchFile(GameTime gametime, SyntaxManager syntaxManager)
        {
            //m_fetchWindow = new TimeSpan(0, 0, 1);

            // The outer counter determines the test window - when we check for
            // the file modification.
            //
            if (gametime.TotalGameTime - m_lastFetchTime > m_fetchWindow)
            {
                //Logger.logMsg("FileBuffer::fetchFile() - testing file for fetch " + m_filename);

                // Now we see if it's been updated recently and if so we refetch
                // and reset our counters.
                //
                //FileInfo fileInfo = new FileInfo(m_filename);
                //fileInfo.Refresh();

                //DateTime fileModTime = File.GetLastWriteTime(m_filename); // fileInfo.LastWriteTime; 
                //DateTime fileCreTime = File.GetCreationTime(m_filename);


                //string name = m_filename;
                //DateTime lastModTime = File.GetLastWriteTime(bv.getFileBuffer().getFilepath());
                
                //if (fileModTime != m_lastFetchSystemTime)
                //{
                    m_lines.Clear();
                    loadFile(syntaxManager);
                    m_lastFetchTime = gametime.TotalGameTime;
                    m_lastFetchSystemTime = DateTime.Now;

                    //Logger.logMsg("FileBuffer::fetchFile() - refetching file " + m_filename);
                    return true;
                //}
            }

            return false;
        }

        /// <summary>
        /// Brute force refetch
        /// </summary>
        public void forceRefetchFile(SyntaxManager syntaxManager)
        {
            m_lines.Clear();
            loadFile(syntaxManager);
        }

        /// <summary>
        /// Gets the content of an existing line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string getLine(int line)
        {
            if (line < m_lines.Count())
            {
                return m_lines[line];
            }
            else
            {
                throw new Exception("FileBuffer():getLine - cannot fetch line " + line);
            }
        }

        /// <summary>
        /// Sets the text value of an existing line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        public void setLine(int line, string value)
        {
            if (m_readOnly)
            {
                Logger.logMsg("FileBuffer::setLine() - file is read only.  Cannot set.");
                return;
            }

            if (line >= m_lines.Count)
            {
                Logger.logMsg("FileBuffer::setLine() - line " + line + " is not available in the FileBuffer");
            }
            else
            {
                m_lines[line] = value;
            }
        }

        /// <summary>
        /// Appends to an existing line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        public void appendToLine(int line, string value)
        {
            if (m_readOnly)
            {
                Logger.logMsg("FileBuffer::appendLine() - file is read only.  Cannot append.");
                return;
            }

            if (line >= m_lines.Count)
            {
                Logger.logMsg("FileBuffer::appendLine() - line " + line + " is not available in the FileBuffer");
            }
            else
            {
                m_lines[line] += value;
            }
        }

        /// <summary>
        /// Append a line to the end of the file - we can do this with readonly/tail files
        /// </summary>
        /// <param name="value"></param>
        public void appendLine(string value)
        {
            m_lines.Add(value);
        }


        /// <summary>
        /// Inserts a line at a given position in the list
        /// </summary>
        /// <param name="line"></param>
        public void insertLine(int line, string value)
        {
            if (m_readOnly)
            {
                Logger.logMsg("FileBuffer::insertLine() - file is read only.  Cannot insert.");
                return;
            }

            try
            {
                m_lines.Insert(line, value);
            }
            catch (Exception e)
            {
                Logger.logMsg("FileBuffer::insertLine() - failed to insert line " + line + " with " + e.Message);
            }
        }

        /// <summary>
        /// Delete a range of lines in the FileBuffer
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public bool deleteLines(int startLine, int number)
        {
            if (startLine > m_lines.Count || startLine + number > m_lines.Count)
            {
                Logger.logMsg("FileBuffer::deleteLines() - line number starting " + startLine + " is not available in the FileBuffer");
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

        /// <summary>
        /// Get the line count
        /// </summary>
        /// <returns></returns>
        public int getLineCount()
        {
            int lC = 0;

            try
            {
                lC = m_lines.Count();
            }
            catch (Exception e)
            {
                Logger.logMsg("FileBuffer::getLineCount() - m_lines is null - " + e.Message);
            }

            return lC;
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
                Logger.logMsg("FileBuffer:tidyUndoStack() - clearing undo position from undo Position " + m_undoPosition + " to " + m_commands.Count);
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

#if UNDO_DEBUG
            Logger.logMsg("FileBuffer:tidyUndoStack() - added new command to undo stack - size is now " + m_commands.Count);
#endif

        }

        /// <summary>
        /// Deletes a selection from a FileBuffer - returns true if it actually deleted something.
        /// </summary>
        /// <param name="startSelection"></param>
        /// <param name="endSelection"></param>
        /// <returns></returns>
        public FilePosition deleteSelection(Project project, FilePosition startSelection, FilePosition endSelection)
        {
            if (m_readOnly)
            {
                return endSelection;
            }

            FilePosition fp = endSelection;

            try
            {
                DeleteTextCommand command = new DeleteTextCommand(project, "Delete Selection", this, startSelection, endSelection);
                fp = command.doCommand();

                // Ensure we are neat and tidy
                //
                tidyUndoStack(command);
            }
            catch (Exception e)
            {
                Logger.logMsg("FileBuffer::deleteSelection() - nothing to delete : " + e.Message);
            }


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
            if (m_readOnly)
            {
                return endSelection;
            }

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
        public FilePosition insertText(Project project, FilePosition insertPosition, string text)
        {
            if (m_readOnly)
            {
                return insertPosition;
            }

            InsertTextCommand command = new InsertTextCommand(project, "Insert Text", this, insertPosition, text);
            FilePosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            // If we have a closing brace then we need to reformat
            //
            if (text == "}")
            {
                ReformatTextCommand reformatCommand = new ReformatTextCommand(project, "Auto Reformat", this, insertPosition, insertPosition);
                fp = reformatCommand.doCommand();
                tidyUndoStack(reformatCommand);
            }


            return fp;
        }

        /// <summary>
        /// Insert a new line
        /// </summary>
        /// <param name="insertPosition"></param>
        public FilePosition insertNewLine(Project project, FilePosition insertPosition, string indent)
        {
            InsertTextCommand command = new InsertTextCommand(project, "Insert new line", this, insertPosition, true, indent);
            FilePosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return fp;
        }

        /// <summary>
        /// Redo a certain number of commands already on the commands list
        /// </summary>
        /// <param name="steps"></param>
        public FilePosition redo(int steps)
        {
            FilePosition fp = new FilePosition();

            Logger.logMsg("FileBuffer::redo() - redo " + steps + " commands from position " + m_undoPosition);

            // Redo commands if we have any pending on the stack
            //
            if (m_commands.Count >= steps + m_undoPosition)
            {
                // Rewind the commands in order
                //
                for (int i = m_undoPosition; i < m_undoPosition + steps; i++)
                {
                    fp = m_commands[i].doCommand();
                }

                // Incremenet the m_undoPosition accordingly
                //
                m_undoPosition += steps;
            }

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

            Logger.logMsg("FileBuffer::undo() - undo " + steps + " commands from position " + m_undoPosition);

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
            }

#if UNDO_DEBUG
            Logger.logMsg("FileBuffer::undo() - undo stack size is now " + m_commands.Count);
            Logger.logMsg("FileBuffer::undo() - undo stack position is now " + m_undoPosition);
#endif

            return fp;
        }

        // Number of commands in stack
        //
        public int getCommandStackLength()
        {
            return m_commands.Count();
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
            if (m_readOnly)
            {
                Logger.logMsg("FileBuffer::save() - this file is marked read only and cannot be saved.");
                return;
            }

            Logger.logMsg("FileBuffer::save() - starting file write " + m_filename);
            using (StreamWriter sw = new StreamWriter(m_filename))
            {
                foreach (string line in m_lines)
                {
                    sw.WriteLine(line);
                }
            }

            Logger.logMsg("FileBuffer::save() - completed file write");

            // Reset this to make the file of unmodified status but keep the undo stack as is
            //
            m_undoWatermark = m_undoPosition;
        }

        /// <summary>
        /// If we have deserialised this object then we need to initialise some stuff
        /// </summary>
        public void initialiseAfterDeseralising()
        {
            // Initialise commands
            //
            m_commands = new List<Command>();

            // Initialise lines
            //
            m_lines = new List<string>();

            // Set up a couple of things
            //
            m_undoPosition = 0;
            m_undoWatermark = 0;
        }

        /// <summary>
        /// Amend ReadOnly get
        /// </summary>
        /// <param name="state"></param>
        public void setReadOnly(bool state)
        {
            m_readOnly = state;
        }

        /// <summary>
        /// Return read only status
        /// </summary>
        /// <returns></returns>
        public bool getReadOnly()
        {
            return m_readOnly;
        }

        /// <summary>
        /// Get some highlighting suggestions from the indicated line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<Highlight> getHighlighting(int line)
        {

            m_returnLineList.Clear();

            List<KeyValuePair<FilePosition, Highlight>> subList = m_highlightSortedList.Where(item => item.Key.Y == line).ToList();
            foreach (KeyValuePair<FilePosition, Highlight> item in subList)
            {
                    m_returnLineList.Add(item.Value);
            }

            return m_returnLineList;
        }

        /// <summary>
        /// Get some highlighting suggestions from the indicated line range
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<Highlight> getHighlighting(int startLine, int endLine)
        {
            m_returnLineList.Clear();

            /*
            // Find all highlighting for this line
            //
            m_returnLineList = m_highlightList.FindAll(
            delegate(Highlight h)
            {
                return (h.m_startHighlight.Y >= startLine && h.m_startHighlight.Y <= endLine);
            }
            );

            // Ensure it's sorted
            //
            m_returnLineList.Sort();
            */

            // Return 
            //
            return m_returnLineList;
        }

        /// <summary>
        /// Public accessor for creation time
        /// </summary>
        /// <returns></returns>
        public DateTime getCreationSystemTime()
        {
            return m_creationSystemTime;
        }

        /// <summary>
        /// Last write system time
        /// </summary>
        /// <returns></returns>
        public DateTime getLastWriteSystemTime()
        {
            return m_lastWriteSystemTime;
        }

        /// <summary>
        /// Last fetch system time
        /// </summary>
        /// <returns></returns>
        public DateTime getLastFetchSystemTime()
        {
            return m_lastFetchSystemTime;
        }

        /// <summary>
        /// Clear the highlight dictionary
        /// </summary>
        public void clearHighlights()
        {
            //m_highlightDictionary.Clear();
            m_highlightSortedList.Clear();
        }

        /// <summary>
        /// Set a highlight at a line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="highlight"></param>
        public void setHighlight(Highlight highlight)
        {
            /*
            if (m_highlightDictionary.ContainsKey(highlight.m_startHighlight))
            {
                m_highlightDictionary[highlight.m_startHighlight] = highlight;
            }
            else
            {
                m_highlightDictionary.Add(highlight.m_startHighlight, highlight);
            }
             * */
            if (m_highlightSortedList.ContainsKey(highlight.m_startHighlight))
            {
                m_highlightSortedList[highlight.m_startHighlight] = highlight;
            }
            else
            {
                m_highlightSortedList.Add(highlight.m_startHighlight, highlight);
            }
        }
    }
}


