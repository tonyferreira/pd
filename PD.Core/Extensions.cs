
using System;

namespace PD.Core
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        public static double BytesToMB(this long bytes)
        {
            return Convert.ToDouble(bytes / 1048576D);
        }

        public static double BytesToGB(this long bytes)
        {
            return Convert.ToDouble(bytes / 1073741824D);
        }
    }
}
