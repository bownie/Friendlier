using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    public enum LineAnnotation
    {
        Default,           // No annotation
        LineModified,      // Line Difference
        LineInserted,      // Line Inserted
        LineDeleted,       // Line Deleted
        LinePadding,       // Line of Padding
        RegionStart,       // Collapsable Region?
        RegionEnd          // Collapsable Region?
    };

    /// <summary>
    /// A BufferView with ability to add annotations and line colouring
    /// </summary>
    public class AnnotatedBufferView : BufferView
    {

        // ------------------------------------ CONSTRUCTORS --------------------------------------
        //
        public AnnotatedBufferView(FontManager fontManager) : base(fontManager)
        {
        }


        // --------------------------------- MEMBER VARIABLES -------------------------------------
        //
        /// <summary>
        /// Store Annotations - one per line only so use a Dictionary
        /// </summary>
        protected Dictionary<int, LineAnnotation> m_annotations = new Dictionary<int, LineAnnotation>();

        /// <summary>
        /// Access the annotations
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, LineAnnotation> getAnnotations()
        {
            return m_annotations;
        }

        /// <summary>
        /// Get a specific annotation
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public LineAnnotation getAnnotation(int line)
        {
            return m_annotations[line];
        }

        /// <summary>
        /// Set a line annotation
        /// </summary>
        /// <param name="line"></param>
        /// <param name="annotation"></param>
        public void setAnnotation(int line, LineAnnotation annotation)
        {
            m_annotations[line] = annotation;
        }

        /// <summary>
        /// Remove an annotation and 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public bool removeAnnotation(int line)
        {
            return m_annotations.Remove(line);
        }

        /// <summary>
        /// Do we have an annotation for the specified line?
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public bool hasAnnotation(int line)
        {
            return m_annotations.ContainsKey(line);
        }

        /// <summary>
        /// Turn an annotation into a Colour
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public Color getLineHighlightColour(int line)
        {
            switch (getAnnotation(line))
            {
                case LineAnnotation.LinePadding:
                    return Color.LightBlue;

                case LineAnnotation.LineDeleted:
                    return Color.Red;

                case LineAnnotation.LineInserted:
                    return Color.Green;

                case LineAnnotation.LineModified:
                    return Color.Orange;

                default:
                    return Color.White;
            }
        }
    }
}
