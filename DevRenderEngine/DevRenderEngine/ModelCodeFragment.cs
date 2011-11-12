using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    /// <summary>
    /// Defines a CodeFragment 
    /// </summary>
    class ModelCodeFragment : ModelItem
    {
        public ModelCodeFragment(string name):base(name)
        {
            itemType = ItemType.CodeFragment;
            dimensions.X = 0.4f;
        }

        public ModelCodeFragment(string name, Vector3 pos)
            : base(name)
        {
            itemType = ItemType.CodeFragment;
            position = pos;
        }

    }



}
