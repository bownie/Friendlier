using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    /// <summary>
    /// Extends SyntaxManager for C++
    /// </summary>
    /// 
    public class CppSyntaxManager : SyntaxManager
    {

        // ------------------------------------ MEMBER VARIABLES ------------------------------------------
        //

        // -------------------------------------- CONSTRUCTORS --------------------------------------------
        //
        public CppSyntaxManager(Project project) : base(project)
        {
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
        public override void generateHighlighting()
        {
            Logger.logMsg("CppSyntaxManager::generateHighlighting() - starting");

            foreach (FileBuffer fb in m_project.getFileBuffers())
            {
                updateHighlighting(fb /*, 0*/);
            }

            Logger.logMsg("CppSyntaxManager::generateHighlighting() - completed.");
        }




        /// <summary>
        /// Regenerate the highlightList by parsing the entire file
        /// </summary>
        public override void updateHighlighting(FileBuffer fileBuffer /* , int fromLine = 0 */)
        {
            Logger.logMsg("CppSyntaxManager::updateHighlighting() - updating " + fileBuffer.getFilepath(), true);

            // Start line for highlight update
            //
            int startLine = 0;

            // We have to recalculate these every time in case we're removing or adding lines
            //
            m_bracePositions.Clear();

            /*
            if (fromLine != 0 && fromLine > (fileBuffer.getLineCount() - 1))
            {
                throw new Exception("Line number greater than buffer length when updating highlighting");
            }

            for (startLine = 0; startLine < fileBuffer.m_highlightList.Count; startLine++)
            {
                if (fileBuffer.m_highlightList[startLine].m_startHighlight.Y >= fromLine)
                {
                    break;
                }
            }*/

            // Remove the range we're going to update
            //
            fileBuffer.clearHighlights();

            int xPosition = 0;
            int lastXPosition = 0;
            int foundPosition = 0;
            bool inMLComment = false;

            // Ok - we scan the whole file - this might have to be optimised at some point
            //
            for (int i = startLine; i < fileBuffer.getLineCount(); i++)
            {
                string line = fileBuffer.getLine(i);

                // Reset xPosition
                //
                xPosition = 0;
                foundPosition = -1;

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
                            Highlight newHighlight = new Highlight(i, xPosition, endOfComment + 1, line.Substring(xPosition, endOfComment), SyntaxManager.m_commentColour);
                            //fileBuffer.m_highlightList.Add(newHighlight);
                            fileBuffer.setHighlight(newHighlight);
                            xPosition = endOfComment + 1;
                            inMLComment = false;
                        }
                        else
                        {
                            // Insert comment to end of line and don't unset inMLComment as we're still in it
                            //
                            Highlight newHighlight = new Highlight(i, xPosition, line.Length - xPosition, line.Substring(xPosition, line.Length - xPosition), SyntaxManager.m_commentColour);
                            //fileBuffer.m_highlightList.Add(newHighlight);
                            fileBuffer.setHighlight(newHighlight);
                            xPosition = line.Length;
                        }
                    }
                    else
                    {
                        // Now deal with starting comments
                        //
                        foundPosition = line.Substring(xPosition).IndexOf("/*");

                        if (foundPosition != -1)
                        {
                            // Insert highlight if this is only thing on line
                            //
                            Highlight newHighlight = new Highlight(i, foundPosition, 2, line.Substring(xPosition, 2), SyntaxManager.m_commentColour);
                            //fileBuffer.m_highlightList.Add(newHighlight);
                            fileBuffer.setHighlight(newHighlight);

                            // Move past comment start
                            //
                            xPosition = foundPosition + 2; // might go over end of line so beware this

                            inMLComment = true;
                        }
                        else // other comments and everything that is not commented but requires highlighting
                        {
                            // #defines etc
                            if (line.IndexOf('#') == 0)
                            {
                                // Create a highlight for a #define
                                Highlight newHighlight = new Highlight(i, 0, line.Length, line, SyntaxManager.m_defineColour);
                                //fileBuffer.m_highlightList.Add(newHighlight);
                                fileBuffer.setHighlight(newHighlight);

                                // And exit this loop
                                xPosition = line.Length;
                            }
                            else if ((foundPosition = line.IndexOf("//")) != -1)
                            {
                                Highlight newHighlight = new Highlight(i, foundPosition, line.Length - foundPosition, line.Substring(foundPosition, line.Length - foundPosition), SyntaxManager.m_commentColour);
                                //fileBuffer.m_highlightList.Add(newHighlight);
                                fileBuffer.setHighlight(newHighlight);
                            }

                            // Now process any other characters ensuring that we're still within the string
                            //
                            if (xPosition < line.Length)
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

                                if (line[xPosition] == '}')
                                {
                                    if (testBraceDepth(xPosition, i) == -1)
                                    {
                                        int existingDepth = getIndentDepth(xPosition, i);
                                        int newDepth = Math.Max(existingDepth - 2, 0);

                                        BraceDepth bd = new BraceDepth(xPosition, i, newDepth);
                                        m_bracePositions.Add(bd, newDepth);
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

            Logger.logMsg("CppSyntaxManager::updateHighlighting() - completed " + fileBuffer.getFilepath(), true);
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
    }
}
