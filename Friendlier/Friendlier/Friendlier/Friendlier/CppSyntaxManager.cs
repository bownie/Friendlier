#region File Description
//-----------------------------------------------------------------------------
// CppSyntaxManager.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Xyglo
{
    /// <summary>
    /// Specialisation of the SyntaxManager base class specifically for C++
    /// </summary>
    /// 
    public class CppSyntaxManager : SyntaxManager
    {
        // ------------------------------------ MEMBER VARIABLES ------------------------------------------
        //
        public static Regex m_openComment = new Regex(@"/\*");

        public static Regex m_closeComment = new Regex(@"\*\/");

        public static Regex m_lineComment = new Regex(@"//");

        public static Regex m_hashLineComment = new Regex(@"#");

        public static Regex m_token = new Regex(@"\b([A-Za-z_][A-Za-z0-9_]+)\b");

        // -------------------------------------- CONSTRUCTORS --------------------------------------------
        //
        public CppSyntaxManager(Project project) : base(project)
        {
            initialiseKeywords();
        }


        // ---------------------------------------- METHODS -----------------------------------------------
        //

        /*
        protected List<Highlight> m_returnLineList = new List<Highlight>();

        /// <summary>
        /// Get some highlighting suggestions from the indicated line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public override List<Highlight> getHighlighting(int line)
        {
            m_returnLineList.Clear();

            // Find all highlighting for this line
            //
            m_returnLineList = m_highlightList.FindAll(
            delegate(Highlight h)
            {
                return h.m_startHighlight.Y == line;
            }
            );

            // Ensure it's sorted
            //
            m_returnLineList.Sort();

            // Return 
            //
            return m_returnLineList;
        }

        /// <summary>
        /// Get some highlighting suggestions from the indicated line range
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public override List<Highlight> getHighlighting(int startLine, int endLine)
        {
            m_returnLineList.Clear();

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

            // Return 
            //
            return m_returnLineList;
        }
         */

        /// <summary>
        /// Generate highlighting for every FileBuffer for example whenever we load a project - we might want
        /// to think about persisting the highlighting and restoring it if it's onerous to work this out..
        /// </summary>
        public override void generateAllHighlighting(FileBuffer fileBuffer)
        {
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - starting");

            generateHighlighting(fileBuffer, new FilePosition(0, 0), fileBuffer.getEndPosition());

            Logger.logMsg("CppSyntaxManager::generateHighlighting() - completed.");
        }

        /// <summary>
        /// A faster indexOf implementation using regexs
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        protected int indexOf(Regex reg, string input)
        {
            Match m = reg.Match(input);

            if (m.Success)
            {
                return m.Index; // step past whitespace character
            }

            return -1;
        }

        /// <summary>
        /// Update the highlighting for this FileBuffer - eventually we'll do this correctly, for the
        /// moment we redo everything for each command which is incredibly wasteful.
        /// </summary>
        /// <param name="fileBuffer"></param>
        /// <param name="line"></param>
        public override void updateHighlighting(FileBuffer fileBuffer, int line)
        {
            // Examine the last command and modify the highlighting accordingly
            //
            Command command = fileBuffer.getLastCommand();

            if (command == null)
            {
                return;
            }

            Logger.logMsg("CppSyntaxManager::updateHighlighting() - updating " + fileBuffer.getFilepath(), true);

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            if (command.GetType() == typeof(Xyglo.DeleteTextCommand))
            {
                Logger.logMsg("CppSyntaxManager::updateHighlighting - update following DeleteTextCommand");

                // Fetch the command and remove the highlighting for affected range
                //
                DeleteTextCommand dtc = (DeleteTextCommand)command;
                //command.getSnippet().getLinesDeleted();

                fileBuffer.removeHighlightingRange(dtc.getStartPos(), dtc.getEndPos(), command.getSnippet());
                generateHighlighting(fileBuffer, dtc.getStartPos(), dtc.getEndPos());
                //generateAllHighlighting(fileBuffer);
            }
            else if (command.GetType() == typeof(Xyglo.InsertTextCommand))
            {
                Logger.logMsg("CppSyntaxManager::updateHighlighting - update following InsertTextCommand");
                //generateAllHighlighting(fileBuffer);
            }
            else if (command.GetType() == typeof(Xyglo.ReplaceTextCommand))
            {
                Logger.logMsg("CppSyntaxManager::updateHighlighting - update following ReplaceTextCommand");
                //generateAllHighlighting(fileBuffer);
            }

            sw.Stop();
            Logger.logMsg("CppSyntaxManager::updateHighlighting() - completed " + fileBuffer.getFilepath() + " in " + sw.Elapsed.TotalMilliseconds + " ms", true);
        }

        /// <summary>
        /// Generate the highlightList by parsing the entire file and throwing away all previous highlighting
        /// information.  This is done once per file load in an ideal world as it takes a while.
        /// </summary>
        public override void generateHighlighting(FileBuffer fileBuffer, FilePosition fromPos, FilePosition toPos)
        {
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - updating " + fileBuffer.getFilepath(), true);

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // We have to recalculate these every time in case we're removing or adding lines
            //
            m_bracePositions.Clear();

            // Remove the range we're going to update
            //
            fileBuffer.clearHighlights(fromPos, toPos);
            //fileBuffer.clearAllHighlights();

            int xPosition = 0;
            int lastXPosition = 0;
            int foundPosition = 0;
            bool inMLComment = false;
            int lineCommentPosition = -1; // position where a line comment starts


#if TABS_DONT_WORK_HERE
            int startTab = 0;
#endif

            // Scan range that we have set
            //
            for (int i = fromPos.Y; i < toPos.Y; i++)
            {
                // If we're displaying with expanded tabs then make sure we also expand them
                // here.  We might want to change this back if we have any issues with wanting
                // to work back from Highlights to meaningful words.
                //
                string preLine = fileBuffer.getLine(i);
                string line = preLine.Replace("\t", m_project.getTab());

                // Reset xPosition
                //
                xPosition = 0;
                foundPosition = -1;
                lineCommentPosition = -1;

#if TABS_DONT_WORK_HERE
                // Initialise startTab so we can handle multiple blocks of tabs efficiently
                //
                startTab = -1;
#endif

                // Scan whole line potentially many times for embedded comments etc
                //
                while (xPosition < line.Length)
                {
                    lastXPosition = xPosition;

                    // Deal with ending multi-line comments first as they are the hardest
                    //
                    if (inMLComment)
                    {
                        int endOfComment = line.Substring(xPosition).IndexOf("*/");
                        //int endOfComment = indexOf(CppSyntaxManager.m_closeComment, line.Substring(xPosition));

                        if (endOfComment != -1) // end comment and continue
                        {
                            Highlight newHighlight = new Highlight(i, xPosition, xPosition + endOfComment + 2, line.Substring(xPosition, endOfComment + 2), SyntaxManager.m_commentColour);
                            fileBuffer.setHighlight(newHighlight);
                            xPosition += endOfComment + 2;
                            inMLComment = false;
                        }
                        else
                        {
                            // Insert comment to end of line and don't unset inMLComment as we're still in it
                            //
                            Highlight newHighlight = new Highlight(i, xPosition, line.Length, line.Substring(xPosition, line.Length - xPosition), SyntaxManager.m_commentColour);
                            fileBuffer.setHighlight(newHighlight);
                            xPosition = line.Length;
                        }
                    }
                    else
                    {
                        // Now deal with starting comments
                        //
                        foundPosition = line.Substring(xPosition).IndexOf("/*");
                        //foundPosition = indexOf(CppSyntaxManager.m_openComment, line.Substring(xPosition));

                        //if (foundPosition < xPosition)
                        //{
                        //}
                        //else
                        //{

                            // #defines etc
                            if ((foundPosition = line.IndexOf("#")) == 0)
                            {
                                // Create a highlight for a #define
                                Highlight newHighlight = new Highlight(i, foundPosition, line.Length, line, SyntaxManager.m_defineColour);
                                fileBuffer.setHighlight(newHighlight);

                                // And exit this loop
                                xPosition = line.Length;
                            }
                            else if ((foundPosition = line.IndexOf("//")) != -1)
                            //else if ((foundPosition = indexOf(CppSyntaxManager.m_lineComment, line)) != -1)
                            {
                                Highlight newHighlight = new Highlight(i, foundPosition, line.Length, line.Substring(foundPosition, line.Length - foundPosition), SyntaxManager.m_commentColour);
                                fileBuffer.setHighlight(newHighlight);
                                lineCommentPosition = foundPosition;
                                xPosition = line.Length;
                            }

                            // Now process any other characters ensuring that we're still within the string
                            // and we're not beyond the start of a line comment boundary.
                            //
                            if (xPosition < line.Length && ( xPosition < lineCommentPosition || lineCommentPosition == -1))
                            {
                                if (line[xPosition] == '{') 
                                {
                                    // Check to see if there is an existing BraceDepth entry here
                                    //
                                    if (testBraceDepth(xPosition, i) == -1)
                                    {
                                        // Test the indent depth on the next line
                                        //
                                        int newDepth = testIndentDepth(fileBuffer, i + 1);

                                        // If it's not set then we just estimate it based on previous values plus '2'
                                        if (newDepth == -1)
                                        {
                                            newDepth = getIndentDepth(xPosition, i) + 2;
                                        }

                                        BraceDepth bd = new BraceDepth(xPosition, i, newDepth);
                                        m_bracePositions.Add(bd, newDepth);
                                    }
                                }
                                else if (line[xPosition] == '}')
                                {
                                    if (testBraceDepth(xPosition, i) == -1)
                                    {
                                        int existingDepth = getIndentDepth(xPosition, i);
                                        int newDepth = Math.Max(existingDepth - 2, 0);

                                        BraceDepth bd = new BraceDepth(xPosition, i, newDepth);
                                        m_bracePositions.Add(bd, newDepth);
                                    }
                                }
                                else if (line[xPosition] == '/' && (xPosition + 1 < line.Length && line[xPosition + 1] == '*'))
                                {
                                    // Insert highlight if this is only thing on line
                                    //
                                    Highlight newHighlight = new Highlight(i, xPosition, xPosition + 2, line.Substring(xPosition, 2), SyntaxManager.m_commentColour);
                                    fileBuffer.setHighlight(newHighlight);

                                    // Move past comment start
                                    //
                                    xPosition += 2; // might go over end of line so beware this

                                    inMLComment = true;
                                }

                                else
                                {
                                    // Fetch a token
                                    //
                                    Match m = m_token.Match(line.Substring(xPosition));

                                    // If we have a token inspect it
                                    //
                                    if (m.Success)
                                    {

                                        // Note that our pattern might match on a substring but not be valid in the
                                        // whole string - so check previous character to see if it's a word boundary
                                        //
                                        bool startBoundary = true;
                                        if (xPosition > 0)
                                        {
                                            Match m2 = m_token.Match(line.Substring(xPosition - 1));

                                            if (m2.Success && line[xPosition - 1] != ' ' && line[xPosition - 1] != '\t' )
                                            {
                                                startBoundary = false;
                                            }
                                        }

                                        if (startBoundary)
                                        {
                                            // Not sure we need this
                                            //string stripWhitespace = m.Value.Replace(" ", "");
                                            //int adjustLength = m.Value.Length - stripWhitespace.Length;
                                            //GroupCollection coll = m.Groups;

                                            if (m_keywords.Contains(m.Value))
                                            {
                                                Highlight newHighlight = new Highlight(i, xPosition + m.Index, xPosition + m.Index + m.Value.Length, m.Value, SyntaxManager.m_keywordColour);
                                                fileBuffer.setHighlight(newHighlight);
                                                xPosition = newHighlight.m_endHighlight.X;
                                            }
                                        }
                                    }
                                }
#if TABS_DONT_WORK_HERE
                                // End of tab run - insert highlight
                                //
                                if (line[xPosition] != '\t' && startTab != -1)
                                {
                                    Highlight newHighlight = new Highlight(i, startTab, xPosition, "TB", SyntaxManager.m_commentColour);
                                    fileBuffer.setHighlight(newHighlight);
                                    startTab = -1;
                                }
                                else if (line[xPosition] == '\t' && startTab == -1) // Handle tabs - we might want to highlight there
                                {
                                    startTab = xPosition;
                                }
#endif
                            }
                        //}
                    }

                    if (lastXPosition == xPosition)
                    {
                        xPosition++;
                    }
                }
            }

            Logger.logMsg("CppSyntaxManager::generateHighlighting() - removing dupes and sorting");
            fileBuffer.checkAndSort();

            sw.Stop();
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - completed " + fileBuffer.getFilepath() + " in " + sw.Elapsed.TotalMilliseconds + " ms", true);
        }

        /// <summary>
        /// Get some suggestions from the current text we're entering
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public override List<string> getAutoCompletes(string text)
        {
            List<string> rS = new List<string>();
            return rS;
        }

        /// <summary>
        /// Get the indent level at a certain line in the FileBuffer
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public override string getIndent(FilePosition fp)
        {
            string rS = "";

            int depth = getIndentDepth(fp);

            // Build up the indent - char by char
            //
            for (int i = 0; i < depth; i++)
            {
                rS += " ";
            }

            return rS;
        }

        /// <summary>
        /// Initialise our C++ keywords
        /// </summary>
        public override void initialiseKeywords()
        {
            // from http://en.cppreference.com/w/cpp/keyword

            // Ignoring these for the minute
            //
            //alignas (C++11)
            //alignof (C++11)
            //char16_t(since C++11)
            //char32_t(since C++11)
            //constexpr(since C++11)
            //decltype(since C++11)
            //noexcept(since C++11)
            //nullptr (since C++11)
            //static_assert(since C++11)
            //thread_local(since C++11)

            m_keywords = new string[] { "and",
                                        "and_eq",
                                        "asm",
                                        "auto",
                                        "bitand",
                                        "bitor",
                                        "bool",
                                        "break",
                                        "case",
                                        "catch",
                                        "char",
                                        "class",
                                        "compl",
                                        "const",
                                        "const_cast",
                                        "continue",
                                        "default",
                                        "delete",
                                        "do",
                                        "double",
                                        "dynamic_cast",
                                        "else",
                                        "enum",
                                        "explicit",
                                        "export",
                                        "extern",
                                        "false",
                                        "float",
                                        "for",
                                        "friend",
                                        "goto",
                                        "if",
                                        "inline",
                                        "int",
                                        "long",
                                        "mutable",
                                        "namespace",
                                        "new",
                                        "not",
                                        "not_eq",
                                        "operator",
                                        "or",
                                        "or_eq",
                                        "private",
                                        "protected",
                                        "public",
                                        "register",
                                        "reinterpret_cast",
                                        "return",
                                        "short",
                                        "signed",
                                        "sizeof",
                                        "static",
                                        "static_cast",
                                        "struct",
                                        "switch",
                                        "template",
                                        "this",
                                        "throw",
                                        "true",
                                        "try",
                                        "typedef",
                                        "typeid",
                                        "typename",
                                        "union",
                                        "unsigned",
                                        "using",
                                        "virtual",
                                        "void",
                                        "volatile",
                                        "wchar_t",
                                        "while",
                                        "xor",
                                        "xor_eq" };
        }
    }
}
