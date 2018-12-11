using System;
using System.Diagnostics.CodeAnalysis;

namespace Resx.Utility
{
    internal static class ClientUtils
    {
        public static bool IsSecurityOrCriticalException(Exception ex)
        {
            return (ex is System.Security.SecurityException) || IsCriticalException(ex);
        }
        // ExecutionEngineException is obsolete and shouldn't be used (to catch, throw or reference) anymore.
        // Pragma added to prevent converting the "type is obsolete" warning into build error.
        // File owner should fix this.
#pragma warning disable 618
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsCriticalException(Exception ex)
        {
            return ex is NullReferenceException
                   || ex is StackOverflowException
                   || ex is OutOfMemoryException
                   || ex is System.Threading.ThreadAbortException
                   || ex is ExecutionEngineException
                   || ex is IndexOutOfRangeException
                   || ex is AccessViolationException;
        }
#pragma warning restore 618
    }
}