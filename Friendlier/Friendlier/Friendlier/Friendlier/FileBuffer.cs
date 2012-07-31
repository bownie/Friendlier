#region File Description
//-----------------------------------------------------------------------------
// FileBuffer.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using System.Threading;

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
    /// Open and buffer a file and provide an interface for handling large files efficiently.
    /// At the moment this isn't efficient and it's not pretty.  Needs some work.
    /// 
    /// Does implement mutex locking for file access to ensure that this is thread safe.
    /// </summary>
    //[DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class FileBuffer : IDisposable
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
        /// Was the last command an undo?
        /// </summary>
        protected bool m_lastCommandUndo = false;

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
        /// A normal List for holding Highlights.  These are markers overlaying text elements which
        /// hold colouration information for text highlighting purposes.
        /// </summary>
        [NonSerialized]
        protected List<Highlight> m_highlightList = new List<Highlight>();

        /// <summary>
        /// List of highlights we're going to return to the drawFileBuffer in the main loop
        /// </summary>
        protected List<Highlight> m_returnLineList = new List<Highlight>();

        /// <summary>
        /// This is the list that is updated in the background 
        /// </summary>
        [NonSerialized]
        protected List<Highlight> m_backgroundHighlightList = new List<Highlight>();

        /// <summary>
        /// List used only to swap between m_highlightList and m_backgroundHighlightList
        /// </summary>
        [NonSerialized]
        List<Highlight> m_swapList = new List<Highlight>();

        /// <summary>
        /// Use this to allow us to process highlighting in the background
        /// </summary>
        protected Mutex m_highlightMutex = new Mutex();

        /// <summary>
        /// Protected our m_lines during read/write
        /// </summary>
        protected Mutex m_lineMutex = new Mutex();

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
        /// Dispose this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposing object
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_lineMutex.Dispose();

                if (m_commands != null)
                {
                    foreach(Command command in m_commands)
                    {
                        command.Dispose();
                    }
                }
            }
        }


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

            // Wait for a lock
            //
            m_lineMutex.WaitOne();

            // Clear collections
            //
            m_lines.Clear();
            m_lineMarkers.Clear();

            // Release lock
            //
            m_lineMutex.ReleaseMutex();
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
            // Get lock;
            //
            m_lineMutex.WaitOne();

            m_lines.Add(text);
            int line = m_lines.Count() - 1;
            m_lineMarkers[line] = marker;

            // Release lock
            //
            m_lineMutex.ReleaseMutex();
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

            // Get lock;
            //
            m_lineMutex.WaitOne();

            if (m_lines.Count() == 0)
            {
                m_lines.Add(text);

            }

            // Release
            //
            m_lineMutex.ReleaseMutex();
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

            // Get lock
            //
            m_lineMutex.WaitOne();

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

            // Create FileStream
            //
            using (FileStream fs = new FileStream(m_filename, FileMode.Open, FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                StreamReader sr = new StreamReader(fs);
                // Open a file for reading
                //
                string line;
                while ((line = sr.ReadLine()) != null && m_lines.Count < m_lineLimit)
                {
                    m_lines.Add(line);
                }
            }
            // Release lock
            //
            m_lineMutex.ReleaseMutex();

            m_lastFetchSystemTime = DateTime.Now;
        }

        protected long m_lastFileSize = 0;

        /// <summary>
        /// We call this when we want to refetch a file for example if we're tailing it
        /// and need a recent copy.   For the moment this is horribly inefficient.
        /// </summary>
        public bool refetchFile(GameTime gametime, SyntaxManager syntaxManager)
        {
            // Check file size from the last fetch - don't necessarily reload if 
            // we don't need to.
            //
            FileInfo f = new FileInfo(m_filename);
            if (f.Length == m_lastFileSize)
                return false;

            m_lastFileSize = f.Length;

            // The outer counter determines the test window - when we check for
            // the file modification.
            //
            if (gametime.TotalGameTime - m_lastFetchTime > m_fetchWindow)
            {
                // Load file and set last fetch time
                //
                loadFile(syntaxManager);
                m_lastFetchTime = gametime.TotalGameTime;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Brute force refetch
        /// </summary>
        public void forceRefetchFile(SyntaxManager syntaxManager)
        {
            // Clear through mutex then load (implicit mutex)
            //
            m_lineMutex.WaitOne();
            m_lines.Clear();
            m_lineMutex.ReleaseMutex();

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
                throw new Exception("FileBuffer::getLine() - cannot fetch line " + line);
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

            // Get lock
            //
            m_lineMutex.WaitOne();

            if (line >= m_lines.Count)
            {
                Logger.logMsg("FileBuffer::setLine() - line " + line + " is not available in the FileBuffer");
            }
            else
            {
                m_lines[line] = value;
            }

            // Release
            //
            m_lineMutex.ReleaseMutex();
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

            // Get lock
            //
            m_lineMutex.WaitOne();

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

            // Release
            //
            m_lineMutex.ReleaseMutex();
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

            // Get lock
            //
            m_lineMutex.WaitOne();

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

            // Release
            //
            m_lineMutex.ReleaseMutex();
        }

        /// <summary>
        /// Append a line to the end of the file - we can do this with readonly/tail files
        /// </summary>
        /// <param name="value"></param>
        public void appendLine(string value)
        {
            // Get lock
            //
            m_lineMutex.WaitOne();

            m_lines.Add(value);

            // Release
            //
            m_lineMutex.ReleaseMutex();
        }

        /// <summary>
        /// Get the mutex on this FileBuffer
        /// </summary>
        /// <returns></returns>
        public bool getLock()
        {
            return m_lineMutex.WaitOne();
        }

        /// <summary>
        /// Release the mutex on this FileBuffer
        /// </summary>
        public void releaseLock()
        {
            m_lineMutex.ReleaseMutex();
            return;
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
                // Get lock
                //
                m_lineMutex.WaitOne();

                m_lines.Insert(line, value);

                // Release
                //
                m_lineMutex.ReleaseMutex();
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

            // Get lock
            //
            m_lineMutex.WaitOne();

            if (number > 0)
            {
                m_lines.RemoveRange(startLine, number);
            }
            else
            {
                m_lines.RemoveAt(startLine);
            }

            // Release
            //
            m_lineMutex.ReleaseMutex();

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
            if (m_commands == null)
                return null;

            if (m_lastCommandUndo) // was the last command an undo?
            {
                if (m_undoPosition >= 0 && m_undoPosition < m_commands.Count)
                {
                    return m_commands[m_undoPosition]; // should always works
                }
            }
            else
            {
                if (m_undoPosition > 0 && m_undoPosition <= m_commands.Count)
                {
                    return m_commands[m_undoPosition - 1];
                }
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

        /*
        /// <summary>
        /// Insert a range into the highlighting.  Often works in combination with a removed range and the action
        /// depends on the command calling it.  StartPosition, endPosition and snippet hold all the information
        /// required but there is an impliced command through their use.  So changes to commands could affect
        /// how this works.  Not a totally clean solution but reasonable in terms of the rest of the design 
        /// (whereby the SyntaxManager.updateHighlighting command is a central interface for doing and undoing
        /// all highlighting changes).
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <param name="snippet"></param>
        public void insertHighlightingRange(FilePosition startPosition, FilePosition endPosition, TextSnippet snippet)
        {
            List<Highlight> modifySelection;

            // Ensure the list is sorted - do we need to do this every time?
            //
            m_highlightList.Sort();

            // For new line inserts
            //
            //if (snippet.isNewLine())
            //{

            //}

            // Now fetch highlights to modify - if on one line only that line
            //
            if ((snippet.isSingleCharacter() && startPosition == endPosition)       // for delete command of single character
                || (snippet.m_lines.Count == 1 && snippet.m_lines[0].Length == 1))  // for insert of single character
            {
                modifySelection = m_highlightList.Where(item => item.m_startHighlight.Y == startPosition.Y).ToList();

                // Store a deep copy of this list in snippet
                //
                foreach (Highlight hl in modifySelection)
                {
                    snippet.m_highlights.Add(new Highlight(hl));
                }

                // Now modify the highlights
                //
                foreach (Highlight hl in modifySelection)
                {
                    if (hl.m_startHighlight.Y == startPosition.Y && hl.m_startHighlight.X >= startPosition.X)
                    {
                        // Deal with single line, single character case
                        if (snippet.isSingleCharacter()         // delete case
                            || snippet.m_lines[0].Length == 1)  // insert case
                        {
                            hl.m_startHighlight.X++;
                            hl.m_endHighlight.X++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove a range from the highlighting.  We use the startPosition and endPosition in combination with the
        /// information stored in the snippet to work out what we have to do, but bear in mind that we already now which
        /// command has issued this request so there is a certain amount of pre-logic assumed here.
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        public void removeHighlightingRange(FilePosition startPosition, FilePosition endPosition, TextSnippet snippet)
        {
            List<Highlight> modifySelection;

            // First handle lines deleted and remove those highlights and mvoe down everything else
            //
            if (snippet.getLinesDeleted() > 0)
            {
                // First get the selection to delete
                //
                modifySelection = m_highlightList.Where(item => item.m_startHighlight.Y >= startPosition.Y && item.m_startHighlight.Y < startPosition.Y + snippet.getLinesDeleted()).ToList();

                // And delete it
                //
                foreach(Highlight hl in modifySelection)
                {
                    m_highlightList.Remove(hl);
                }

                // Everything remaining after the start position needs some Y values taking off them
                //
                modifySelection = m_highlightList.Where(item => item.m_startHighlight.Y >= startPosition.Y).ToList();

                // Get the previous line length so we can add it to any highlights that are joined 
                // on to this line.
                //
                int previousLineLength = getLine(startPosition.Y).Length;

                foreach (Highlight hl in modifySelection)
                {
                    // If we're attaching to a previous line and the previous line has something on it 
                    // already adjust the X portion of the highlights)
                    //
                    if ((hl.m_startHighlight.Y - snippet.getLinesDeleted()) == startPosition.Y)
                    {
                        hl.m_startHighlight.X += previousLineLength;
                        hl.m_endHighlight.X += previousLineLength;
                    }

                    // Now take the line count away
                    //
                    hl.m_startHighlight.Y -= snippet.getLinesDeleted();
                    hl.m_endHighlight.Y -= snippet.getLinesDeleted();
                }
                return;
            }

            // Now fetch highlights to modify - if on one line only that line
            //
            if ((snippet.isSingleCharacter() && startPosition == endPosition)       // for delete command of single character
                || (snippet.m_lines.Count == 1 && snippet.m_lines[0].Length == 1))  // for insert of single character)
            {
                modifySelection = m_highlightList.Where(item => item.m_startHighlight.Y == startPosition.Y).ToList();

                // Store a deep copy of this list in snippet
                //
                foreach (Highlight hl in modifySelection)
                {
                    snippet.m_highlights.Add(new Highlight(hl));
                }

                // Now modify the highlights
                //
                foreach (Highlight hl in modifySelection)
                {
                    if (hl.m_startHighlight.Y == startPosition.Y && hl.m_startHighlight.X >= startPosition.X)
                    {
                        // Deal with single line, single character case
                        if (snippet.isSingleCharacter()         // delete case
                            || snippet.m_lines[0].Length == 1)  // insert case
                        {
                            hl.m_startHighlight.X--;
                            hl.m_endHighlight.X--;
                        }
                    }
                }
            }
            else
            {
                //  Get all highlights from this line onwards if lines are deleted
                //
                modifySelection = m_highlightList.Where(item => item.m_startHighlight.X >= startPosition.X).ToList();

                // 
                foreach (Highlight hl in modifySelection)
                {
                    if (hl.m_startHighlight.Y == startPosition.Y && hl.m_startHighlight.X >= startPosition.X)
                    {
                        // Deal with single line, single character case
                        if (snippet.isSingleCharacter())
                        {
                            hl.m_startHighlight.X--;
                            hl.m_endHighlight.X--;
                        }

                        if (snippet.getLinesDeleted() > 0)
                        {

                        }
                    }
                }
            }
        }*/

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

            // Last command wasn't an undo
            //
            m_lastCommandUndo = false;

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

            ScreenPosition fp = new ScreenPosition();

            ReplaceTextCommand command = new ReplaceTextCommand(project, "Replace Text", this, startSelection, endSelection, text, highlightStart, highlightEnd);
            fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            // Last command wasn't an undo
            //
            m_lastCommandUndo = false;
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

            ScreenPosition fp = new ScreenPosition();

            InsertTextCommand command = new InsertTextCommand(project, "Insert Text", this, insertPosition, text, highlightStart, highlightEnd);
            fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            // If we have a closing brace then we need to reformat
            //
            if (text == "}")
            {
                ReformatTextCommand reformatCommand = new ReformatTextCommand(project, "Auto Reformat", this, insertPosition, insertPosition);
                {
                    fp = reformatCommand.doCommand();
                    tidyUndoStack(reformatCommand);
                }
            }

            // Last command wasn't an undo
            //
            m_lastCommandUndo = false;

            //Logger.logMsg("NUMBER OF COMMNADS = " + m_commands.Count);

            return fp;
        }

        /// <summary>
        /// Insert a new line
        /// </summary>
        /// <param name="insertPosition"></param>
        public ScreenPosition insertNewLine(Project project, FilePosition insertPosition, ScreenPosition highlightStart, ScreenPosition highlightEnd, string indent)
        {
            ScreenPosition fp = new ScreenPosition();

            InsertTextCommand command = new InsertTextCommand(project, "Insert new line", this, insertPosition, highlightStart, highlightEnd, true, indent);
            fp = command.doCommand();

            // Ensure we are neat and tidy
            //
            tidyUndoStack(command);

            // Last command wasn't an undo
            //
            m_lastCommandUndo = false;

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

            // Last command wasn't an undo
            //
            m_lastCommandUndo = false;

            return rP;

        }

        /*
        /// <summary>
        /// Undo any highlighting changes using the highlights stored in the snippet
        /// </summary>
        protected void undoHighlighting(FilePosition startPos, TextSnippet snippet)
        {
            // Remove all highlights on this line
            //
            List<Highlight> removeList = m_highlightList.Where(item => item.m_startHighlight.Y == startPos.Y).ToList();
            foreach (Highlight hl in removeList)
            {
                m_highlightList.Remove(hl);
            }

            m_highlightList.AddRange(snippet.m_highlights);
            m_highlightList.Sort();
        }
         * */


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

                    // Fix the highlighting
                    //
                    //undoHighlighting(m_commands[i].getStartPos(), m_commands[i].getSnippet());
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

            // Set this indicator
            //
            m_lastCommandUndo = true;

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

            // Get lock
            //
            m_lineMutex.WaitOne();

            Logger.logMsg("FileBuffer::save() - starting file write " + m_filename);
            using (StreamWriter sw = new StreamWriter(m_filename))
            {
                foreach (string line in m_lines)
                {
                    sw.WriteLine(line);
                }
            }

            // Release
            //
            m_lineMutex.ReleaseMutex();

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
        /// Get some highlighting suggestions from the indicated line.   Compensate for xOffset and the 
        /// length of the line so we only return a subset that is already adjusted for our view.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<Highlight> getHighlighting(int line, int xOffset, int lineLength)
        {
            m_returnLineList.Clear();
            Highlight convertHL;

            // Get the mutex
            //
            m_highlightMutex.WaitOne();

            foreach (Highlight item in m_highlightList.Where(item => item.m_startHighlight.Y == line).ToList())
            {
                convertHL = new Highlight(item);

                if (xOffset > 0)
                {
                    if (item.m_startHighlight.X >= xOffset && item.m_startHighlight.X <= xOffset + lineLength)
                    {
                        convertHL.m_startHighlight.X -= xOffset;
                        convertHL.m_endHighlight.X -= xOffset;

                        if (convertHL.m_startHighlight.X < 0)
                        {
                            throw new Exception("Got inconsistency in keys offset = " + xOffset);
                        }
                    }
                }

                if (m_returnLineList.Contains(convertHL))
                {
                    throw new Exception("Already have this highlight in return list");
                }
                else
                {
                    m_returnLineList.Add(convertHL);
                }
            }

            // Release the mutex
            //
            m_highlightMutex.ReleaseMutex();

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

        /// <summary>
        /// Clear the list of all highlights
        /// </summary>
        public void clearAllHighlights(bool background)
        {
            if (background)
                m_backgroundHighlightList.Clear();
            else
            {
                m_highlightMutex.WaitOne();
                m_highlightList.Clear();
                m_highlightMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Clear the highlight dictionary
        /// </summary>
        public void clearHighlights(FilePosition fromPos, FilePosition toPos, bool background)
        {
            if (background)
            {
                List<Highlight> removeList = m_backgroundHighlightList.Where(item => item.m_startHighlight >= fromPos && item.m_endHighlight <= toPos).ToList();
                foreach (Highlight item in removeList)
                {
                    m_backgroundHighlightList.Remove(item);
                }
            }
            else
            {
                m_highlightMutex.WaitOne();

                List<Highlight> removeList = m_highlightList.Where(item => item.m_startHighlight >= fromPos && item.m_endHighlight <= toPos).ToList();
                foreach (Highlight item in removeList)
                {
                    m_highlightList.Remove(item);
                }

                m_highlightMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Set a highlight at a line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="highlight"></param>
        public void setHighlight(Highlight highlight, bool background)
        {
            if (background)
            {
                if (m_backgroundHighlightList.Contains(highlight))
                {
                    throw new Exception("FileBuffer::setHighlight() - attempting to set duplicate");
                }

                m_backgroundHighlightList.Add(highlight);
            }
            else
            {
                m_highlightMutex.WaitOne();

                if (m_highlightList.Contains(highlight))
                {
                    throw new Exception("FileBuffer::setHighlight() - attempting to set duplicate");
                }

                m_highlightList.Add(highlight);

                m_highlightMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Ensure that the highlight list is sorted, ordered and consistent
        /// </summary>
        public void checkAndSort(bool background)
        {
            if (background)
            {
                m_backgroundHighlightList = m_backgroundHighlightList.Distinct().ToList();
                m_backgroundHighlightList.Sort();
            }
            else
            {
                m_highlightMutex.WaitOne();

                // Remove duplicates
                //
                m_highlightList = m_highlightList.Distinct().ToList();

                // Sort
                //
                m_highlightList.Sort();

                // Release
                //
                m_highlightMutex.ReleaseMutex();
            }
        }


        /// <summary>
        /// Swap across the background highlighting to the live highlighting - this should be done from
        /// the syntax highlighting thread only so we can also spend some time here creating a new 
        /// List for the next set of background highlighting.   We can also do a deep copy if we want to.
        /// </summary>
        public void copyBackgroundHighlighting(bool slowDeepCopy = false)
        {
            m_highlightMutex.WaitOne();

            //Logger.logMsg("FileBuffer::copyBackgroundHighlighting() - m_highlightList count = " + m_highlightList.Count);
            //Logger.logMsg("FileBuffer::copyBackgroundHighlighting() - m_backgroundHighlightList count = " + m_backgroundHighlightList.Count);

            if (slowDeepCopy)
            {
                m_highlightList.Clear();

                // Deep copy the list into the highlight list
                //
                foreach (Highlight hl in m_backgroundHighlightList)
                {
                    m_highlightList.Add(new Highlight(hl));
                }
            }
            else
            {
                m_swapList = m_highlightList;
                m_highlightList = m_backgroundHighlightList;

                // Clear this list and swap it onto the background list
                m_swapList.Clear();
                m_backgroundHighlightList = m_swapList;
            }
  
            m_highlightMutex.ReleaseMutex();
        }
    }
}
