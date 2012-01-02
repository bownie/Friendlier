using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    internal interface IUndoable<T>
    {
        T State { get; }
    }
}
