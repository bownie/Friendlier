using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Xyglo
{
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public abstract class XygloView
    {
        /// <summary>
        /// 3d position of the BufferView
        /// </summary>
        [DataMember]
        protected Vector3 m_position;

        /// <summary>
        /// Font manager reference passed in to provide font sizes for rendering
        /// </summary>
        [NonSerialized]
        protected FontManager m_fontManager = null;

        /// <summary>
        /// Ensure that XygloViews can describe their Width
        /// </summary>
        /// <returns></returns>
        public abstract float getWidth();

        /// <summary>
        /// Ensure that XygloViews can describe their height
        /// </summary>
        /// <returns></returns>
        public abstract float getHeight();

        /// <summary>
        /// Ensure that XygloViews can describe their depth
        /// </summary>
        /// <returns></returns>
        public abstract float getDepth();

    }
}
