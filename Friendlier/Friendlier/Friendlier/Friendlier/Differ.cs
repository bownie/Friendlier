#region File Description
//-----------------------------------------------------------------------------
// Differ.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Xyglo
{
    public enum DiffResult
    {
        Unchanged,
        Inserted,
        Deleted,
        Padding
    }

    public class DiffPreview
    {
        public DiffPreview(Vector2 start, Vector2 end, Color colour)
        {
            m_startPos = start;
            m_endPos = end;
            m_colour = colour;
        }
        public Vector2 m_startPos;
        public Vector2 m_endPos;
        public Color m_colour;
    }


    /// <summary>
    /// Differ is a wrapper class that turns a diff algorithm into a useful class for
    /// our representation - able to find out the number of differences and where they
    /// occur.
    /// </summary>
    public class Differ
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
        /// Store our Diff
        /// </summary>
        protected DiffMatchPatch.diff_match_patch m_diff = null;

        /// <summary>
        /// Diff List
        /// </summary>
        protected List<DiffMatchPatch.Diff> m_diffList = null;

        /// <summary>
        /// The list that we'll populate with LHS diff information for drawing
        /// </summary>
        List<DiffPreview> m_lhsDiffPreview = new List<DiffPreview>();

        /// <summary>
        /// The list that we'll populate with RHS diff information for drawing
        /// </summary>
        List<DiffPreview> m_rhsDiffPreview = new List<DiffPreview>();

        // List on which to push highlights and colours
        //
        List<Pair<Pair<Vector3, Vector3>, Color>> m_highlightList = new List<Pair<Pair<Vector3, Vector3>, Color>>();

        /// <summary>
        /// Position of the right hand box preview
        /// </summary>
        Vector2 m_rightBox;

        /// <summary>
        /// Bottom right of right hand box preview
        /// </summary>
        Vector2 m_rightBoxEnd;

        /// <summary>
        /// Top left of left hand box preview
        /// </summary>
        Vector2 m_leftBox;

        /// <summary>
        /// Bottom right of left hand box preview
        /// </summary>
        Vector2 m_leftBoxEnd;

        /// <summary>
        /// Colour of unchanged lines
        /// </summary>
        public Color m_unchangedColour = Color.White;

        /// <summary>
        /// Colour of deleted lines
        /// </summary>
        public Color m_deletedColour = Color.Red;

        /// <summary>
        /// Colour of padding lines
        /// </summary>
        public Color m_paddingColour = Color.Yellow;

        /// <summary>
        /// Colour of inserted lines
        /// </summary>
        public Color m_insertedColour = Color.Green;

        /// <summary>
        /// Y margin for the diff preview 
        /// </summary>
        protected float m_yMargin = 10.0f;

        /// <summary>
        /// X margin for the diff preview
        /// </summary>
        protected float m_xMargin = 5.0f;

        // -------------------------------- CONSTRUCTORS --------------------------------------
        //


        /// <summary>
        /// Construct a Differ object.  A Differ contains a Google DiffMatchPatch object and this is
        /// used to generate the raw diff.  Then on top of this the Differ class provides an interface
        /// which gives it a line-level usefulness for our display purposes.
        /// </summary>
        public Differ()
        {
            if (m_diff == null)
            {
                m_diff = new DiffMatchPatch.diff_match_patch();
            }

            if (m_diffList == null)
            {
                m_diffList = new List<DiffMatchPatch.Diff>();
            }
        }

        /// <summary>
        /// Source buffer view for RHS
        /// </summary>
        /// <returns></returns>
        public BufferView getSourceBufferViewLhs()
        {
            return m_sourceBufferView1;
        }

        /// <summary>
        /// Source buffer view for RHS
        /// </summary>
        /// <returns></returns>
        public BufferView getSourceBufferViewRhs()
        {
            return m_sourceBufferView2;
        }

        /// <summary>
        /// Set the BufferViews for the diff
        /// </summary>
        /// <param name="bv1"></param>
        /// <param name="bv2"></param>
        public void setBufferViews(BufferView bv1, BufferView bv2)
        {
            m_sourceBufferView1 = bv1;
            m_sourceBufferView2 = bv2;
        }

        /// <summary>
        /// X margin for text preview
        /// </summary>
        /// <returns></returns>
        public float getXMargin()
        {
            return m_xMargin;
        }

        /// <summary>
        /// Y margin for text preview
        /// </summary>
        /// <returns></returns>
        public float getYMargin()
        {
            return m_yMargin;
        }

        /// <summary>
        /// Get the diff list
        /// </summary>
        /// <returns></returns>
        public List<DiffMatchPatch.Diff> getDiffList()
        {
            return m_diffList;
        }

        /// <summary>
        /// This is our locally stored snapshots of differences - result and line length which
        /// is enough to generate the visible diff from but we also want the source line number
        /// associated with any value here.
        /// </summary>
        protected List<Pair<DiffResult, int>> m_lhsDiff = new List<Pair<DiffResult, int>>();

        /// <summary>
        /// The locally stored snapshort for the RH file for results of diff.
        /// </summary>
        protected List<Pair<DiffResult, int>> m_rhsDiff = new List<Pair<DiffResult, int>>();

        /// <summary>
        /// Return the locally cached diff results for the LHS
        /// </summary>
        /// <returns></returns>
        public List<Pair<DiffResult, int>> getLhsDiff()
        {
            return m_lhsDiff;
        }

        /// <summary>
        /// Return the locally cached diff results for the RHS
        /// </summary>
        /// <returns></returns>
        public List<Pair<DiffResult, int>> getRhsDiff()
        {
            return m_rhsDiff;
        }

        /// <summary>
        /// Turns the original lhs file position into a diff file position so that
        /// we can keep a consistent view of file position.  For the LHS we count
        /// Unchanged and Deleted lines as part of the original document.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public int originalLhsFileToDiffPosition(int position)
        {
            int matchValue = 0;

            for (int i = 0; i < m_lhsDiff.Count; i++)
            {
                DiffResult dR = (DiffResult)m_lhsDiff[i].First;
                if (dR == DiffResult.Unchanged || dR == DiffResult.Deleted)
                {
                    if (matchValue == position)
                    {
                        return i;
                    }

                    // For each Unchanged line we increment the match value
                    matchValue++;
                }
            }

            return -1;
        }

        /// <summary>
        /// Turns the original rhs file position into a diff file position so that
        /// we can keep a consistent view of file position.  For the RHS we count
        /// Unchanged and Inserted lines as part of the original document.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public int originalRhsFileToDiffPosition(int position)
        {
            int matchValue = 0;

            for (int i = 0; i < m_rhsDiff.Count; i++)
            {
                DiffResult dR = (DiffResult)m_rhsDiff[i].First;
                if (dR == DiffResult.Unchanged || dR == DiffResult.Inserted)
                {
                    if (matchValue == position)
                    {
                        return i;
                    }

                    // For each Unchanged line we increment the match value
                    matchValue++;
                }
            }

            return -1;
        }

        /// <summary>
        /// Get Bufferview file position from diff position for LHS
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public int diffPositionLhsToOriginalPosition(int position)
        {
            int rV = position;

            for (int i = 0; i < position; i++)
            {
                if (m_lhsDiff[i].First == DiffResult.Padding)
                {
                    rV--;
                }
            }

            return rV;
        }

        /// <summary>
        /// Get Bufferview file position from diff position for RHS
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public int diffPositionRhsToOriginalPosition(int position)
        {
            int rV = position;

            for (int i = 0; i < position; i++)
            {
                if (m_rhsDiff[i].First == DiffResult.Padding)
                {
                    rV--;
                }
            }

            return rV;
        }


        /// <summary>
        /// The maximum diff length
        /// </summary>
        /// <returns></returns>
        public int getMaxDiffLength()
        {
            return Math.Max(m_lhsDiff.Count(), m_rhsDiff.Count());
        }

        /// <summary>
        /// Work out the maxiumum width of the rows
        /// </summary>
        /// <returns></returns>
        public int getMaxDiffWidth()
        {
            int leftMaxWidth = 0; 
            int rightMaxWidth = 0;

            // Won't work unless we have some content
            //
            if (m_lhsDiff.Count > 0)
            {
                leftMaxWidth = m_lhsDiff.Max(item => item.Second);
            }

            if (m_rhsDiff.Count > 0)
            {
                rightMaxWidth = m_rhsDiff.Max(item => item.Second);
            }

            return Math.Max(leftMaxWidth, rightMaxWidth);
        }

        /// <summary>
        /// Left box coordinations
        /// </summary>
        /// <returns></returns>
        public Vector2 getLeftBox()
        {
            return m_leftBox;
        }

        /// <summary>
        /// Left box bottom right hand corner
        /// </summary>
        /// <returns></returns>
        public Vector2 getLeftBoxEnd()
        {
            return m_leftBoxEnd;
        }

        /// <summary>
        /// Right box coordinates
        /// </summary>
        /// <returns></returns>
        public Vector2 getRightBox()
        {
            return m_rightBox;
        }

        /// <summary>
        /// Right box bottom right hand corner
        /// </summary>
        /// <returns></returns>
        public Vector2 getRightBoxEnd()
        {
            return m_rightBoxEnd;
        }

        public bool hasDiffs()
        {
            return (m_rhsDiff.Count() > 0 || m_lhsDiff.Count() > 0 || m_rhsDiffPreview.Count() > 0 || m_lhsDiffPreview.Count() > 0);
        }


        /// <summary>
        /// Clear the diff previews etc
        /// </summary>
        public void clear()
        {
            m_rhsDiff.Clear();
            m_lhsDiff.Clear();
            m_rhsDiffPreview.Clear();
            m_lhsDiffPreview.Clear();

            m_sourceBufferView1 = null;
            m_sourceBufferView2 = null;
        }


        /// <summary>
        /// Generate the previews so that they are ready for rendering - turns the diff results into a 
        /// cached copy of the previews.  We only do this once per diffing request.
        /// </summary>
        /// <param name="leftBox"></param>
        /// <param name="leftBoxEnd"></param>
        /// <param name="rightBox"></param>
        /// <param name="rightBoxEnd"></param>
        public void generateDiffPreviews(Vector2 leftBox, Vector2 leftBoxEnd, Vector2 rightBox, Vector2 rightBoxEnd)
        {

            m_leftBox = leftBox;
            m_leftBoxEnd = leftBoxEnd;
            m_rightBox = rightBox;
            m_rightBoxEnd = rightBoxEnd;

            // Set our working width and height
            //
            float workingHeight = Math.Min(leftBoxEnd.Y - leftBox.Y, rightBoxEnd.Y - rightBox.Y) - (2 * m_yMargin);
            float workingWidth = Math.Min(leftBoxEnd.X - leftBox.X, rightBoxEnd.X - rightBox.X) - (2 * m_xMargin);

            float lineHeight = workingHeight / (float)getMaxDiffLength();

            float charWidth = workingWidth / (float)getMaxDiffWidth();

            //Logger.logMsg("LINE HEIGHT = " + lineHeight);
            //Logger.logMsg("CHAR WIDTH  = " + charWidth);

            Logger.logMsg("Differ::generateDiffPreviews() - generating the left hand side diff preview");

            // Populate the preview lists
            //
            for (int i = 0; i < m_lhsDiff.Count(); i++)
            {
                Color lineColour;

                switch(m_lhsDiff[i].First)
                {
                    case  DiffResult.Deleted:
                        lineColour = m_deletedColour;
                        break;

                    case DiffResult.Inserted:
                        lineColour = m_insertedColour;
                        break;

                    case DiffResult.Padding:
                        lineColour = m_paddingColour;
                        break;

                    case DiffResult.Unchanged:
                    default:
                        lineColour = m_unchangedColour;
                        break;
                }

                Vector2 start = new Vector2(leftBox.X + m_xMargin, leftBox.Y + m_yMargin + (i * lineHeight));
                Vector2 end = new Vector2(leftBox.X + m_xMargin + (m_lhsDiff[i].Second * charWidth), leftBox.Y + m_yMargin + (i * lineHeight));

                // Generate preview object and push onto list
                //
                DiffPreview preview = new DiffPreview(start, end, lineColour);
                m_lhsDiffPreview.Add(preview);
            }

            Logger.logMsg("generateDiffPreviews() - generating the right hand side diff preview");
            for (int i = 0; i < m_rhsDiff.Count(); i++)
            {
                Color lineColour;

                switch (m_rhsDiff[i].First)
                {
                    case DiffResult.Deleted:
                        lineColour = m_deletedColour;
                        break;

                    case DiffResult.Inserted:
                        lineColour = m_insertedColour;
                        break;

                    case DiffResult.Padding:
                        lineColour = m_paddingColour;
                        break;

                    case DiffResult.Unchanged:
                    default:
                        lineColour = m_unchangedColour;
                        break;
                }

                Vector2 start = new Vector2(rightBox.X + m_xMargin, rightBox.Y + m_yMargin + (i * lineHeight));
                Vector2 end = new Vector2(rightBox.X + m_xMargin + (m_rhsDiff[i].Second * charWidth), rightBox.Y + m_yMargin + (i * lineHeight));

                // Generate preview object and push onto list
                //
                DiffPreview preview = new DiffPreview(start, end, lineColour);
                m_rhsDiffPreview.Add(preview);
            }
        }

        /// <summary>
        /// Get the diff preview for the RHS
        /// </summary>
        /// <returns></returns>
        public List<DiffPreview> getRhsDiffPreview()
        {
            return m_rhsDiffPreview;
        }

        /// <summary>
        /// Get the diff preview for the LHS
        /// </summary>
        /// <returns></returns>
        public List<DiffPreview> getLhsDiffPreview()
        {
            return m_lhsDiffPreview;
        }

        /// <summary>
        /// Process the files to diff
        /// </summary>
        /// <returns></returns>
        public bool process()
        {
            Logger.logMsg("Differ::process - starting diff process");


            // We use the diff publicLineMode - which is a hack to get line style diffing working
            //
            m_diffList = m_diff.diff_publicLineMode(m_sourceBufferView1.getFileBuffer().getTextString(), m_sourceBufferView2.getFileBuffer().getTextString());

            // For no diffs or one diff the same then we've got nothing to do here
            //
            if (m_diffList.Count == 0 || (m_diffList.Count == 1 && m_diffList[0].operation == DiffMatchPatch.Operation.EQUAL))
                return false;

            // Make the diffs human readable
            //
            //m_diff.diff_cleanupSemantic(m_diffList);

            // We have to do some state management here to make some sense
            // of our diff.
            //
            //DiffMatchPatch.Diff lastDiff;

            Logger.logMsg("Differ::process() - got " + m_diffList.Count + " diffs to process");

            // 
            int lastDeleteLines = 0;


#if PREALLOC
            int totalPairs = 0;

            // Preallocate some memory to speed things up a bit
            //
            foreach (DiffMatchPatch.Diff diff in m_diffList)
            {
                totalPairs += diff.text.Count(item => item == '\n');
            }

            // Preallocate lis
            //
            List<Pair<DiffResult, int>> tempList = new List<Pair<DiffResult, int>>(totalPairs);

            int currentPair = 0;
#endif

            // Process all the diffs
            //
            foreach (DiffMatchPatch.Diff diff in m_diffList)
            {
                // How many lines in this diff?
                //
                int linesAffected = diff.text.Count(item => item == '\n');

                // Now allow for any padding
                //
                switch(diff.operation)
                {
                    case DiffMatchPatch.Operation.DELETE:
                        for (int i = 0; i < linesAffected; i++)
                        {
#if PREALLOC
                            tempList[currentPair].First = DiffResult.Deleted;
                            tempList[currentPair].Second = diff.text.Split('\n')[i].Length;
                            m_lhsDiff.Add(tempList[currentPair++]);
#else
                            m_lhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Deleted, diff.text.Split('\n')[i].Length));
#endif
                        }

                        // Set this so we know how many lines were deleted from the lhs
                        //
                        lastDeleteLines = linesAffected;
                    break;

                    case DiffMatchPatch.Operation.INSERT:
                        for(int i = 0; i < linesAffected; i++)
                        {
#if PREALLOC
                            tempList[currentPair].First = DiffResult.Inserted;
                            tempList[currentPair].Second = diff.text.Split('\n')[i].Length;
                            m_rhsDiff.Add(tempList[currentPair++]);
#else
                            m_rhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Inserted, diff.text.Split('\n')[i].Length));
#endif

                            // If we didn't delete anything last then we need to pad the left hand side
                            // by the total lines affected.
                            //
                            if (lastDeleteLines == 0)
                            {
#if PREALLOC
                                tempList[currentPair].First = DiffResult.Padding;
                                tempList[currentPair].Second = 0;
                                m_lhsDiff.Add(tempList[currentPair++]);
#else
                                m_lhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Padding, 0));
#endif
                            }
                        }

                        // Pad out the rest of the rhs as necessary if we've deleted more lines
                        // than we've inserted.
                        //
                        if (linesAffected < lastDeleteLines)
                        {
                            for (int i = linesAffected; i < lastDeleteLines; i++)
                            {
#if PREALLOC
                                tempList[currentPair].First = DiffResult.Padding;
                                tempList[currentPair].Second = 0;
                                m_rhsDiff.Add(tempList[currentPair++]);
#else
                                m_rhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Padding, 0));
#endif
                            }
                        }
                        else if (lastDeleteLines > 0 && lastDeleteLines < linesAffected)
                        {
                            for (int i = lastDeleteLines; i < linesAffected; i++)
                            {
#if PREALLOC
                                tempList[currentPair].First = DiffResult.Padding;
                                tempList[currentPair].Second = 0;
                                m_lhsDiff.Add(tempList[currentPair++]);
#else
                                m_lhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Padding, 0));
#endif
                            }
                        }

                        lastDeleteLines = 0;
                    break;
                
                    case DiffMatchPatch.Operation.EQUAL:

                        // Insert padding at this point if we're deleted some prior lines
                        for (int i = 0; i < lastDeleteLines; i++ )
                        {
#if PREALLOC
                            tempList[currentPair].First = DiffResult.Padding;
                            tempList[currentPair].Second = 0;
                            m_rhsDiff.Add(tempList[currentPair++]);
#else
                            m_rhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Padding, 0));
#endif
                        }

                        for (int i = 0; i < linesAffected; i++)
                        {
#if PREALLOC
                            tempList[currentPair].First = DiffResult.Unchanged;
                            tempList[currentPair].Second = diff.text.Split('\n')[i].Length;
                            m_rhsDiff.Add(tempList[currentPair++]);

                            tempList[currentPair].First = DiffResult.Unchanged;
                            tempList[currentPair].Second = diff.text.Split('\n')[i].Length;
                            m_lhsDiff.Add(tempList[currentPair++]);

#else
                            m_rhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Unchanged, diff.text.Split('\n')[i].Length));
                            m_lhsDiff.Add(new Pair<DiffResult, int>(DiffResult.Unchanged, diff.text.Split('\n')[i].Length));
#endif
                        }
                        lastDeleteLines = 0;
                        break;
                }
            }

#if PREALLOC
            // Deallocate explicitly anything not yet used
            //
            for (int i = currentPair; i < tempList.Count; i++)
            {
                tempList[i] = null;
            }
#endif
            return true;
        }
    }
}
