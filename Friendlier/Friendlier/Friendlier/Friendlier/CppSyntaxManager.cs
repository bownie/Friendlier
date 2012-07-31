#region File Description
//-----------------------------------------------------------------------------
// CppSyntaxManager.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion


// Do we want lots of debug?
//
//#define CPP_SYNTAX_MANAGER_DEBUG

// We can define this if we want to remember previously matched keywords and use them
// to build a picture of where we are.
//
//#define KEYWORD_HISTORY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Xyglo
{
    /// <summary>
    /// Specialisation of the SyntaxManager base class specifically for C++.  At the momemnt the highlighting
    /// is very basic and only covers keywords and preprocessor directives.  However we have flexibility and 
    /// bear in mind that SyntaxManager runs in two halves - a quick foreground scan of the current screen
    /// when something has changed, followed by a deeper background full run to ensure that everything is
    /// properly up to date.  The highlights themselves are stored in the FileBuffer but they are created
    /// in this class.  The FileBuffer has two lists for highlights - foreground and background to enable
    /// the two modes of operations - and the lists are subsequently copied or swapped as required.
    /// </summary>
    /// 
    public class CppSyntaxManager : SyntaxManager
    {
        // ------------------------------------ MEMBER VARIABLES ------------------------------------------
        //

        /// <summary>
        /// Open comment Regex
        /// </summary>
        public static Regex m_openComment = new Regex(@"/\*");

        /// <summary>
        /// Close comment Regex 
        /// </summary>
        public static Regex m_closeComment = new Regex(@"\*\/");

        /// <summary>
        /// Line comment Regex
        /// </summary>
        public static Regex m_lineComment = new Regex(@"//");

        /// <summary>
        /// Hash line Regex
        /// </summary>
        public static Regex m_hashLineComment = new Regex(@"#");

        /// <summary>
        /// A definition of a token for our keyword definition
        /// </summary>
        public static Regex m_token = new Regex(@"\b([A-Za-z_][A-Za-z0-9_]+)\b");


#if KEYWORD_HISTORY
        /// <summary>
        /// List of previous keywords
        /// </summary>
        public List<string> m_keywordList = new List<string>();
#endif

        /// <summary>
        /// Mutex to protected highlighting when running in multiple threads - probably don't need this
        /// </summary>
        //protected Mutex m_highlightingMutex = new Mutex();

        // -------------------------------------- CONSTRUCTORS --------------------------------------------
        //
        public CppSyntaxManager(Project project) : base(project)
        {
        }

        // ---------------------------------------- METHODS -----------------------------------------------
        //

        /// <summary>
        /// Generate highlighting for every FileBuffer for example whenever we load a project - we might want
        /// to think about persisting the highlighting and restoring it if it's onerous to work this out..
        /// </summary>
        public override bool generateAllHighlighting(FileBuffer fileBuffer, bool backgroundThread)
        {
#if SMART_HELP_DEBUG
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - starting");
#endif

            return generateHighlighting(fileBuffer, new FilePosition(0, 0), fileBuffer.getEndPosition(), backgroundThread);

#if SMART_HELP_DEBUG
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - completed.");
#endif
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
        /// Generate the highlightList by parsing the entire file and throwing away all previous highlighting
        /// information.  This is done once per file load in an ideal world as it takes a while.
        /// </summary>
        public override bool generateHighlighting(FileBuffer fileBuffer, FilePosition fromPos, FilePosition toPos, bool backgroundThread = false)
        {
#if CPP_SYNTAX_MANAGER_DEBUG
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - updating " + fileBuffer.getFilepath(), true);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif
            //m_highlightingMutex.WaitOne();

            // Remove the range we're going to update - if we're doing a background process then clear all
            // the highlights.
            //
            if (backgroundThread)
            {
                fileBuffer.clearAllHighlights(backgroundThread);

                // Only redo braces in the background
                //
                m_bracePositions.Clear();

#if KEYWORD_HISTORY
                // Clear the keyword list
                //
                m_keywordList.Clear();
#endif

            }
            else
            {
                fileBuffer.clearHighlights(fromPos, toPos, backgroundThread);
            }

            int xPosition = 0;
            int lastXPosition = 0;
            int foundPosition = 0;
            bool inMLComment = false;
            int lineCommentPosition = -1; // position where a line comment starts

            // Reset this before a long running process
            //
            m_interruptProcessing = false;

#if TABS_DONT_WORK_HERE
            int startTab = 0;
#endif

            // Scan range that we have set
            //
            for (int i = fromPos.Y; i < toPos.Y + 1; i++)
            {
                if (m_interruptProcessing)
                {
                    Logger.logMsg("CppSyntaxManager::generateHighlighting() - processing highlights interrupted");

                    // Don't forget to release the mutex
                    //
                    //m_highlightingMutex.ReleaseMutex();

                    return false; // false means don't copy anything from background to foreground as we have crap in the buffer
                }

                // If we're displaying with expanded tabs then make sure we also expand them
                // here.  We might want to change this back if we have any issues with wanting
                // to work back from Highlights to meaningful words.
                //
                string line = fileBuffer.getLine(i).Replace("\t", m_project.getTab());

                // Reset xPosition
                //
                xPosition = 0;
                foundPosition = -1;
                lineCommentPosition = -1;

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

                        if (endOfComment != -1) // end comment and continue
                        {
                            Highlight newHighlight = new Highlight(i, xPosition, xPosition + endOfComment + 2, line.Substring(xPosition, endOfComment + 2), HighlightType.Comment);
                            fileBuffer.setHighlight(newHighlight, backgroundThread);
                            xPosition += endOfComment + 2;
                            inMLComment = false;
                        }
                        else
                        {
                            // Insert comment to end of line and don't unset inMLComment as we're still in it
                            //
                            Highlight newHighlight = new Highlight(i, xPosition, line.Length, line.Substring(xPosition, line.Length - xPosition), HighlightType.Comment);
                            fileBuffer.setHighlight(newHighlight, backgroundThread);
                            xPosition = line.Length;
                            continue; // skip to next line
                        }
                    }
                    else
                    {
                        // Now deal with starting comments
                        //
                        foundPosition = line.Substring(xPosition).IndexOf("/*");

                        if ((foundPosition = line.IndexOf("#")) == 0)
                        {
                            // Create a highlight for a #define
                            Highlight newHighlight = new Highlight(i, foundPosition, line.Length, line, HighlightType.Define);
                            fileBuffer.setHighlight(newHighlight, backgroundThread);

                            // And exit this loop
                            xPosition = line.Length;
                        }
                        else if ((foundPosition = line.IndexOf("//")) != -1)
                        //else if ((foundPosition = indexOf(CppSyntaxManager.m_lineComment, line)) != -1)
                        {
                            Highlight newHighlight = new Highlight(i, foundPosition, line.Length, line.Substring(foundPosition, line.Length - foundPosition), HighlightType.Comment);
                            fileBuffer.setHighlight(newHighlight, backgroundThread);
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

                                    // We only recalculate brace depths when looking at the whole file
                                    //
                                    if (backgroundThread)
                                    {
                                        BraceDepth bd = new BraceDepth(xPosition, i, newDepth);
                                        m_bracePositions.Add(bd, newDepth);
                                    }
                                }
                            }
                            else if (line[xPosition] == '}')
                            {
                                if (testBraceDepth(xPosition, i) == -1)
                                {
                                    int existingDepth = getIndentDepth(xPosition, i);
                                    int newDepth = Math.Max(existingDepth - 2, 0);

                                    // We only recalculate brace depths when looking at the whole file
                                    //
                                    if (backgroundThread)
                                    {
                                        BraceDepth bd = new BraceDepth(xPosition, i, newDepth);
                                        m_bracePositions.Add(bd, newDepth);
                                    }
                                }
                            }
                            else if (line[xPosition] == '/' && (xPosition + 1 < line.Length && line[xPosition + 1] == '*'))
                            {
                                // Insert highlight if this is only thing on line
                                //
                                Highlight newHighlight = new Highlight(i, xPosition, xPosition + 2, line.Substring(xPosition, 2), HighlightType.Comment);
                                fileBuffer.setHighlight(newHighlight, backgroundThread);

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

                                    // Has our pre-check passed?
                                    //
                                    if (startBoundary)
                                    {
                                        if (m_keywords.Contains(m.Value))
                                        {
                                            Highlight newHighlight = new Highlight(i, xPosition + m.Index, xPosition + m.Index + m.Value.Length, m.Value, HighlightType.Keyword);
                                            fileBuffer.setHighlight(newHighlight, backgroundThread);
                                            xPosition = newHighlight.m_endHighlight.X;


#if KEYWORD_HISTORY
                                            // Push the list
                                            //
                                            m_keywordList.Add(m.Value.ToLower());

                                            // keep list to n
                                            //
                                            int n = 10;
                                            if (m_keywordList.Count > n)
                                            {
                                                m_keywordList.RemoveRange(0, m_keywordList.Count - n);
                                            }
#endif
                                        }
                                        else
                                        {
#if KEYWORD_HISTORY
                                            // At this point we are not a keyword - yet we have a nice
                                            // juicy keyword list which we can use to intuit our purpose
                                            // as a non-keyword.
                                            //
                                            if (m_keywordList.Contains("public") ||
                                                m_keywordList.Contains("private") ||
                                                m_keywordList.Contains("protected"))
                                            {
                                                Logger.logMsg("WHAT IS THIS ---> " + m.Value);
                                            }
#endif
                                            // Step past token
                                            //
                                            xPosition += m.Value.Length;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (lastXPosition == xPosition)
                    {
                        xPosition++;
                    }
                }
            }

#if CPP_SYNTAX_MANAGER_DEBUG
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - removing dupes and sorting");
#endif
            fileBuffer.checkAndSort(backgroundThread);

#if CPP_SYNTAX_MANAGER_DEBUG

            sw.Stop();
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - completed " + fileBuffer.getFilepath() + " in " + sw.Elapsed.TotalMilliseconds + " ms", true);
#endif

            // Release the mutex for this
            //m_highlightingMutex.ReleaseMutex();

            return true;
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
