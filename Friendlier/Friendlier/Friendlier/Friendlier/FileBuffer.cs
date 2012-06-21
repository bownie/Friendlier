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
    /// An enum for marking specific lines in a FileBuffer dictionary
    /// </summary>
    public enum LineMarker
    {
        Normal,
        Deleted,
        Inserted,
        Modified
    }

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
        /// Dictionary object for LineMarkers used when diffing FileBuffers
        /// </summary>
        Dictionary<int, LineMarker> m_lineMarkers = new Dictionary<int, LineMarker>();

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
        [NonSerialized]
        protected TimeSpan m_lastFetchTime = TimeSpan.Zero;

        /// <summary>
        /// The last System time we fetch this file
        /// </summary>
        [NonSerialized]
        protected DateTime m_lastFetchSystemTime = DateTime.MinValue;

        /// <summary>
        /// When this FileBuffer was created
        /// </summary>
        [DataMember]
        protected DateTime m_creationSystemTime = DateTime.MinValue;

        /// <summary>
        /// Last time we wrote this file
        /// </summary>
        [DataMember]
        protected DateTime m_lastWriteSystemTime = DateTime.MinValue;

        /// <summary>
        /// Fetch Window for a File - every second
        /// </summary>
        [IgnoreDataMember]
        protected TimeSpan m_fetchWindow;

        /// <summary>
        /// Sortedlist of highlights
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

            // Initialise things
            //
            initialise();
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

            // Fix the paths properly
            //
            fixPaths();

            // Initialise things
            //
            initialise();
        }

        /// <summary>
        /// Copy constructor - we only copy the lines though making a facsimile 
        /// of a FileBuffer that we can discard.
        /// </summary>
        /// <param name="fb"></param>
        public FileBuffer(FileBuffer fb)
        {
            for (int i = 0; i < fb.getLineCount(); i++)
            {
                m_lines.Add(fb.getLine(i));
            }
        }


        /////////////// METHODS ///////////////////

        /// <summary>
        /// Initialise some stuff
        /// </summary>
        protected void initialise()
        {
            // Define Timespan in the default constructor
            //
            m_fetchWindow = new TimeSpan(0, 0, 0, 1, 0);
        }

        /// <summary>
        /// Get the full filepath
        /// </summary>
        /// <returns></returns>
        public string getFilepath()
        {
            return m_filename;
        }

        /// <summary>
        /// Clear the FileBuffer
        /// </summary>
        public void clear()
        {
            m_filename = "";
            m_shortName = "";
            m_readOnly = false;

            // Clear collections
            //
            m_lines.Clear();
            m_lineMarkers.Clear();
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
        /// Add some text and a LineMarker to a specific line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="marker"></param>
        /// <param name="text"></param>
        public void addLineWithMarker(LineMarker marker, string text)
        {
            m_lines.Add(text);
            int line = m_lines.Count() - 1;
            m_lineMarkers[line] = marker;
        }

        /// <summary>
        /// Append a piece of text and check LineMarker to a specific line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="marker"></param>
        /// <param name="text"></param>
        public void appendLineWithMarker(LineMarker marker, string text)
        {
//            if (m_lineMarkers.ContainsKey(line) && m_lineMarkers[line] != marker)
            //{
                //throw new Exception("Attempting to repurpose an existing LineMarker");
            //}

            if (m_lines.Count() == 0)
            {
                m_lines.Add(text);

            }
            // Set line to append the new text
            //
            //m_lines[line] = m_lines[line] + text;
        }

        /// <summary>
        /// Get a LineMarker for a given line if it exists
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public LineMarker getLineMarker(int line)
        {
            if (!m_lineMarkers.ContainsKey(line))
            {
                throw new Exception("LineMarker for line " + line + " does not exist");
            }

            return m_lineMarkers[line];
        }

        /// <summary>
        /// Fetch the whole file as a single string
        /// </summary>
        /// <returns></returns>
        public string getTextString()
        {
            string rs = "";

            foreach (string addString in m_lines)
            {
                rs += addString;

                //if (addString.IndexOf('\n') != Math.Min(addString.Length - 1, 1))
                if (!addString.Contains('\n'))
                {
                    rs += "\n";
                }
            }
            return rs;
            //return String.Join(String.Empty, m_lines.ToArray());
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

            // Set up some file information here
            try
            {
                m_creationSystemTime = File.GetCreationTime(m_filename);
                m_lastWriteSystemTime = File.GetLastWriteTime(m_filename);
            }
            catch (Exception)
            {
                Logger.logMsg("FileBuffer::FileBuffer() - can't get the file creation time");
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

            m_lastFetchSystemTime = DateTime.Now;
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

                    // Calling this also updates the m_lastFetchSystemTime
                    //
                    loadFile(syntaxManager);
                    m_lastFetchTime = gametime.TotalGameTime;

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
        /// Set a line even if there is no entry for it yet
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        public void appendLineIfNotExist(int line, string value)
        {
            if (m_readOnly)
            {
                Logger.logMsg("FileBuffer::setLine() - file is read only.  Cannot set.");
                return;
            }

            if (line >= m_lines.Count)
            {
                // Add enough lines to ensure we can reference it
                //
                for (int i = 0; i <= line - m_lines.Count; i++)
                {
                    m_lines.Add("");
                }
            }

            // Now set the line
            //
            m_lines[line] += value;
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

            if (line > m_lines.Count)
            {
                Logger.logMsg("FileBuffer::appendLine() - line " + line + " is not available in the FileBuffer");
            }
            else if (line == m_lines.Count)
            {
                m_lines.Add(value);
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

        /// <summary>
        /// Is this FileBuffer too big for Friendlier to process currently?
        /// </summary>
        /// <returns></returns>
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
        /// Get the last command executed on this FileBuffer - m_undoPosition points to the next available
        /// undo place so make sure we return the previous command.
        /// </summary>
        /// <returns></returns>
        public Command getLastCommand()
        {
            if (m_commands != null && m_undoPosition > 0 && m_undoPosition <= m_commands.Count)
            {
                return m_commands[m_undoPosition - 1];
            }

            return null;
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
        /// Remove a range from the highlighting.  If we specify a range and no lines deleted then just the highlighting
        /// is removed.   If linesDeleted is specified then we also remove that number of lines from subsequent
        /// highlighting entries.  This allows us to deal with inline deletions and well as complete deletions
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        public void removeHighlightingRange(FilePosition startPosition, FilePosition endPosition, int linesDeleted)
        {
            if (linesDeleted == 0)
            {
                // Have to be deleting on one line.  Get all the highlighting for this line beyond the start point.
                //
                List<KeyValuePair<FilePosition, Highlight>> deleteSelection = m_highlightSortedList.Where(item => item.Key.X >= startPosition.X && item.Key.Y == endPosition.Y).ToList();
                //List<KeyValuePair<FilePosition, Highlight>> reInsertList = new List<KeyValuePair<FilePosition, Highlight>>();

                // Iterate through deleting and re-inserting
                //
                foreach (var item in deleteSelection)
                {
                    if (m_highlightSortedList.Remove(item.Key))
                    {
                        Logger.logMsg("FilerBuffer::removeHighlightingRange() - removed highlight item - adding replacement");

                        // Modify these entries
                        //
                        FilePosition fp = item.Key;
                        Highlight h = item.Value;

                        // We will always at least delete one character - perhaps a range
                        //
                        int xAdjust = Math.Max(1, endPosition.X - startPosition.X);

                        // Adjust X by removed range
                        //
                        if (fp.X > 0)
                        {
                            fp.X -= xAdjust;
                        }

                        if (h.m_startHighlight.X > 0)
                        {
                            h.m_startHighlight.X -= xAdjust;
                            h.m_endHighlight.X -= xAdjust;

                            // Only re-add if this is true otherwise we've broken the token anyway
                            m_highlightSortedList.Add(fp, h);
                        }
                    }
                    else
                    {
                        Logger.logMsg("FilerBuffer::removeHighlightingRange() - failed to remove highlight");
                    }
                }

                // Now return
                //
                return;
            }


            // First fetch all the highlights that start after our startPosition
            //
            List<KeyValuePair<FilePosition, Highlight>> removeList = m_highlightSortedList.Where(item => item.Key >= startPosition).ToList();

            // Check for multi-line spanning highlights from before our deletion
            //
            List<KeyValuePair<FilePosition, Highlight>> modifyList = m_highlightSortedList.Where(item => item.Key < startPosition && item.Value.m_endHighlight > startPosition).ToList();

            // Modified elements still need removing
            //
            removeList.AddRange(modifyList);

            Logger.logMsg("FileBuffer::removeHighlightingRange - removing " + removeList.Count() + " highlights");

            // Remove all of these
            //
            foreach (KeyValuePair<FilePosition, Highlight> item in removeList)
            {
                m_highlightSortedList.Remove(item.Key);
            }

            // The list we want to restore is everything not in the selection - do this 
            // by line and regenerate the first and last lines afterwards.
            //
            List<KeyValuePair<FilePosition, Highlight>> restList = removeList.Where(item => item.Key.Y > endPosition.Y).ToList();

            // And re-insert
            //
            foreach (KeyValuePair<FilePosition, Highlight> item in restList)
            {
                // Adjust FilePosition by number of lines removed
                //
                FilePosition fp = item.Key;
                Highlight hl = item.Value;

                fp.Y -= (endPosition.Y - startPosition.Y + 1);
                hl.m_startHighlight.Y -= (endPosition.Y - startPosition.Y + 1);
                hl.m_endHighlight.Y -= (endPosition.Y - startPosition.Y + 1);

                m_highlightSortedList.Add(fp, item.Value);
            }
        }


        /// <summary>
        /// Deletes a selection from a FileBuffer - returns true if it actually deleted something.
        /// </summary>
        /// <param name="startSelection"></param>
        /// <param name="endSelection"></param>
        /// <returns></returns>
        public ScreenPosition deleteSelection(Project project, FilePosition startSelection, FilePosition endSelection, ScreenPosition startHighlight, ScreenPosition endHighlight)
        {
            ScreenPosition fp = new ScreenPosition(endSelection);
            if (m_readOnly)
            {
                return fp;
            }

            try
            {
                DeleteTextCommand command = new DeleteTextCommand(project, "Delete Selection", this, startSelection, endSelection, startHighlight, endHighlight);
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
        public ScreenPosition replaceText(Project project, FilePosition startSelection, FilePosition endSelection, string text, ScreenPosition highlightStart, ScreenPosition highlightEnd)
        {
            if (m_readOnly)
            {
                return new ScreenPosition(endSelection);
            }

            ReplaceTextCommand command = new ReplaceTextCommand(project, "Replace Text", this, startSelection, endSelection, text, highlightStart, highlightEnd);
            ScreenPosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return fp;
        }

        /*
        /// <summary>
        /// Need this conversion at a file level to a ScreenPosition - this is used by commands to
        /// provide screen level feedback
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public FilePosition getFilePosition(Project project, ScreenPosition sP)
        {
            // Only fetch the line if we have one to fetch
            //
            if (sP.Y == 0 || sP.Y >= getLineCount())
            {
                return new FilePosition(sP);
            }

            string line = getLine(sP.Y);
            string subLine = line.Substring(0, Math.Min(sP.X, line.Length));

            // Number of tabs
            //
            int numTabs = subLine.Where(item => item == '\t').Count();

            // Remove the width of the tabs from the position
            //
            return m_cursorPosition.X - (numTabs * (project.getTab().Length - 1));
        }
        */


        /// <summary>
        /// Insert some text into our current FileBuffer at a particular position
        /// </summary>
        /// <param name="insertPosition"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public ScreenPosition insertText(Project project, FilePosition insertPosition, ScreenPosition highlightStart, ScreenPosition highlightEnd, string text)
        {
            if (m_readOnly)
            {
                return new ScreenPosition(insertPosition);
            }

            InsertTextCommand command = new InsertTextCommand(project, "Insert Text", this, insertPosition, text, highlightStart, highlightEnd);
            ScreenPosition fp = command.doCommand();

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
        public ScreenPosition insertNewLine(Project project, FilePosition insertPosition, ScreenPosition highlightStart, ScreenPosition highlightEnd, string indent)
        {
            InsertTextCommand command = new InsertTextCommand(project, "Insert new line", this, insertPosition, highlightStart, highlightEnd, true, indent);
            ScreenPosition fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            return fp;
        }

        /// <summary>
        /// Redo a certain number of commands already on the commands list
        /// </summary>
        /// <param name="steps"></param>
        public Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>> redo(int steps)
        {
            Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>> rP = new Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>>();
            ScreenPosition fp = new ScreenPosition();

            ScreenPosition startHighlight = new ScreenPosition();
            ScreenPosition endHighlight = new ScreenPosition();

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
                    startHighlight = m_commands[i].getHighlightStart();
                    endHighlight = m_commands[i].getHighlightEnd();
                }

                // Increment the m_undoPosition accordingly
                //
                m_undoPosition += steps;
            }

            // Set return value of the cursor position and highlight information
            //
            rP.First = fp;

            // Assign the sub-pair
            //
            rP.Second = new Pair<ScreenPosition, ScreenPosition>();
            rP.Second.First = startHighlight;
            rP.Second.Second = endHighlight;

            return rP;

        }

        /// <summary>
        /// Undo a given number of steps in the life of a FileBuffer - returns a complicated package
        /// of position cursor and highlighting (if any was saved)
        /// </summary>
        /// <param name="steps"></param>
        /// <returns></returns>
        public Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>> undo(int steps)
        {
            Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>> rP = new Pair<ScreenPosition, Pair<ScreenPosition, ScreenPosition>>();
            ScreenPosition fp = new ScreenPosition();

            Logger.logMsg("FileBuffer::undo() - undo " + steps + " commands from position " + m_undoPosition);

            ScreenPosition startHighlight = new ScreenPosition();
            ScreenPosition endHighlight = new ScreenPosition();

            if (m_commands.Count >= steps && m_undoPosition >= 0)
            {
                // Unwind the commands in order
                //
                for (int i = m_undoPosition - 1; i > m_undoPosition - 1 - steps; i--)
                {
                    fp = m_commands[i].undoCommand();

                    startHighlight = m_commands[i].getHighlightStart();
                    endHighlight = m_commands[i].getHighlightEnd();
                }

                // Reduce the m_undoPosition accordingly
                //
                m_undoPosition -= steps;
            }

#if UNDO_DEBUG
            Logger.logMsg("FileBuffer::undo() - undo stack size is now " + m_commands.Count);
            Logger.logMsg("FileBuffer::undo() - undo stack position is now " + m_undoPosition);
#endif
            // Set return value of the cursor position and highlight information
            //
            rP.First = fp;

            // Assign the sub-pair
            //
            rP.Second = new Pair<ScreenPosition, ScreenPosition>();
            rP.Second.First = startHighlight;
            rP.Second.Second = endHighlight;

            return rP;
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
        /// Save this FileBuffer to a File
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
        /// End of file position
        /// </summary>
        /// <returns></returns>
        public FilePosition getEndPosition()
        {
            if (m_lines.Count > 0)
            {
                int y = m_lines.Count - 1;
                int x = m_lines[y].Length;

                return new FilePosition(x, y);
            }

            return new FilePosition(0, 0);
        }

        public void clearAllHighlights()
        {
            m_highlightSortedList.Clear();
        }
        /// <summary>
        /// Clear the highlight dictionary
        /// </summary>
        public void clearHighlights(FilePosition fromPos, FilePosition toPos)
        {
            List<KeyValuePair<FilePosition, Highlight>> removeList = m_highlightSortedList.Where(item => item.Key >= fromPos && item.Key <= toPos).ToList();

            foreach (KeyValuePair<FilePosition, Highlight> item in removeList)
            {
                m_highlightSortedList.Remove(item.Key);
            }
        }

        /// <summary>
        /// Set a highlight at a line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="highlight"></param>
        public void setHighlight(Highlight highlight)
        {
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


