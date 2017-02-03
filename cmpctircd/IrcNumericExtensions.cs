using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmpctircd
{
    public static class IrcNumericExtensions
    {
        /// <summary>
        /// Formats an IRC numeric response as a string, complete with leading
        /// zeros.
        /// </summary>
        /// <param name="n">The IRC numeric to format.</param>
        /// <returns>The formatted numeric.</returns>
        public static string Printable(this IrcNumeric n)
        {
            return string.Format("{0:000}", (int)n);
        }
    }
}
