using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    /// <summary>
    /// Extends SyntaxManager for C++
    /// </summary>
    public class CppSyntaxManager : SyntaxManager
    {
        public CppSyntaxManager(Project project) : base(project)
        {
        }

        /// <summary>
        /// Get some highlighting suggestions from the indicated line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public override List<Highlight> getHighlighting(int line)
        {
            List<Highlight> rL = new List<Highlight>();
            return rL;
        }

        /// <summary>
        /// Get some suggestions from the current text we're entering
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public override List<string> getSuggestions(string text)
        {
            List<string> rS = new List<string>();
            return rS;
        }

        /// <summary>
        /// Get the indent level at a certain line in the FileBuffer
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public override string getIndent(int line)
        {
            string rs = "";
            return rs;
        }
    }
}
