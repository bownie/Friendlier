using System;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace Xyglo
{
    /// <summary>
    /// A DiffView is a type of XygloView which provides information about the differences
    /// between two or more BufferViews.
    /// 
    /// The premise of the DiffView is;
    /// 
    /// - accepts two BufferViews
    /// - generate a new Window with Read Only view of these views linked together
    /// - allows cutting and pasting from this view to the editable BufferViews
    /// 
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public class DiffView : XygloView
    {
        // ------------------------------- MEMBER VARIABLES ----------------------------------
        //
        /// <summary>
        /// First BufferView we are comparing
        /// </summary>
        protected BufferView m_sourceBufferView1 = null;

        /// <summary>
        /// Second BufferView we are comparing
        /// </summary>
        protected BufferView m_sourceBufferView2 = null;

        /// <summary>
        /// First Target BufferView
        /// </summary>
        protected BufferView m_targetBufferView1 = null;

        /// <summary>
        /// Second Target BufferView
        /// </summary>
        protected BufferView m_targetBufferView2 = null;

        /// <summary>
        /// FileBuffer 1 for target
        /// </summary>
        protected FileBuffer m_fileBuffer1 = null;

        /// <summary>
        /// FileBuffer 2 for target
        /// </summary>
        protected FileBuffer m_fileBuffer2 = null;

        // -------------------------------- CONSTRUCTORS --------------------------------------
        //
        public DiffView(Project project, BufferView bv1, BufferView bv2)
        {
            // Set our members
            //
            m_fontManager = project.getFontManager();
            m_sourceBufferView1 = bv1;
            m_sourceBufferView2 = bv2;

            // And initialise
            //
            initialise(project);
        }


        // ----------------------------------- METHODS ------------------------------------------
        //
        /// <summary>
        /// Initialise this object with a project
        /// </summary>
        protected void initialise(Project project)
        {
            if (m_fileBuffer1 == null)
                m_fileBuffer1 = new FileBuffer();

            if (m_fileBuffer2 == null)
                m_fileBuffer2 = new FileBuffer();

            // Create two BufferViews with these new FileBuffers - these are internal to this
            // DiffView and therefore don't need any positional information - they will be 
            // drawn by the DiffView specific code.
            //
            if (m_targetBufferView1 == null)
            {
                m_targetBufferView1 = new BufferView(m_fontManager);
                m_targetBufferView1.setFileBuffer(m_fileBuffer1);
            }

            if (m_targetBufferView2 == null)
            {
                m_targetBufferView2 = new BufferView(m_fontManager);
                m_targetBufferView2.setFileBuffer(m_fileBuffer2);
            }

            // At this point we can work out our width and height and try to find a 
            // suitable position for this DiffBuffer
            //
            m_position = project.getFreePosition(m_sourceBufferView1, getWidth(), getHeight());
        }

        /// <summary>
        /// Get first destination BufferView
        /// </summary>
        /// <returns></returns>
        public BufferView getSourceBufferView1()
        {
            return m_sourceBufferView1;
        }

        /// <summary>
        /// Get second destination BufferView
        /// </summary>
        /// <returns></returns>
        public BufferView getSourceBufferView2()
        {
            return m_sourceBufferView2;
        }

        /// <summary>
        /// Accepts two source BufferViews and processes them through to two destination
        /// BufferViews which are spaced and coloured accordingly.
        /// </summary>
        /// <param name="bv1"></param>
        /// <param name="bv2"></param>
        /// <returns></returns>
        public bool process()
        {
            DiffMatchPatch.diff_match_patch dm = new DiffMatchPatch.diff_match_patch();

            List<DiffMatchPatch.Diff> rL = dm.diff_lineMode(m_sourceBufferView1.getFileBuffer().getTextString(), m_sourceBufferView2.getFileBuffer().getTextString());

            if (rL.Count == 0)
                return false;

            // Make the diffs human readable
            //
            //dm.diff_cleanupSemantic(rL);

            // We have to do some state management here to make some sense
            // of our diff.
            //
            DiffMatchPatch.Diff lastDiff;

            m_fileBuffer1.clear();
            m_fileBuffer2.clear();

            // Leftline and rightline store the line we're currently on in the
            // relevant diff output file.
            //
            //int leftLine = 0;
            //int rightLine = 0;

            foreach (DiffMatchPatch.Diff diff in rL)
            {
                // A DELETE means that something is removed from the left hand file
                // but if this is followed by an INSERT then this is a change.  In 
                // this was we have to reconstruct the line meanings from the Diff
                // context.
                //
                if (diff.operation == DiffMatchPatch.Operation.DELETE)
                {
                    //foreach (string thing in diff.text.Split('\n'))
                    //{
                        //newBV.getFileBuffer().appendLine("DELETE : " + diff);
                    //}
                }
                else if (diff.operation == DiffMatchPatch.Operation.INSERT)
                {
                    // Explains the logic here if we split first and compare
                    //
                    string[] splitString = diff.text.Split('\n');

                    //if (splitString[0] == diff.text)
                    //{
                        //m_fileBuffer1.appendLineWithMarker(m_fileBuffer1.getLineCount() - 1, LineMarker.Inserted, diff.text);
                    //}
                    //else
                    //{
                        //foreach (string thing in splitString)
                        //{
                            //m_fileBuffer1.appendLineWithMarker(m_fileBuffer1.getLineCount() - 1, LineMarker.Inserted, thing);
                        //}
                    //}
                }
                else // EQUAL
                {
                    foreach (string subString in diff.text.Split('\n'))
                    {
                        m_fileBuffer1.appendLine(diff.text);
                        m_fileBuffer2.appendLine(diff.text);
                    }
                    // Always write equals out.
                    // Pretend we're writing the left hand side (SOURCE)
                    //
                    //foreach (string subString in diff.text.Split('\n'))
                    //{
                        //newBV.getFileBuffer().appendLine(diff.text);
                        //leftLine++;
                    //}
                    //newBV.getFileBuffer().appendLine(diff.text);
                }

                lastDiff = diff;
            }

            return true;
        }

        /// <summary>
        /// BufferView width defined by font size and we add a gap between the two
        /// </summary>
        /// <returns></returns>
        public override float getWidth()
        {
            return m_fontManager.getCharWidth() * ( m_targetBufferView1.getBufferShowWidth() + m_targetBufferView2.getBufferShowWidth() + 4);
        }

        /// <summary>
        /// BufferView height defined by font size and we add a gap between the two
        /// </summary>
        /// <returns></returns>
        public override float getHeight()
        {
            return m_fontManager.getCharWidth() * (m_targetBufferView1.getBufferShowLength() + m_targetBufferView2.getBufferShowLength() + 4);
        }

        /// <summary>
        /// We have no depth as a BufferView
        /// </summary>
        /// <returns></returns>
        public override float getDepth()
        {
            return 0.0f;
        }

        /// <summary>
        /// Return the number of differences
        /// </summary>
        /// <returns></returns>
        public int getDifferences()
        {
            return 0;
        }
    }
}
