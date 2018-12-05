using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Resources;

namespace Resx.Resources
{
    /// <devdoc>
    ///     ResX resource set.
    /// </devdoc>
    public class ResXResourceSet : ResourceSet
    {
        /// <devdoc>
        ///     Creates a resource set for the specified file.
        /// </devdoc>
        [
            SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")  // Shipped like this in Everett.
        ]
        public ResXResourceSet(String fileName) : base(new ResXResourceReader(fileName))
        {
            ReadResources();
        }

        /// <devdoc>
        ///     Creates a resource set for the specified stream.
        /// </devdoc>
        [
            SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")  // Shipped like this in Everett.
        ]
        public ResXResourceSet(Stream stream) : base(new ResXResourceReader(stream))
        {
            ReadResources();
        }

        /// <devdoc>
        ///     Gets the default reader type associated with this set.
        /// </devdoc>
        public override Type GetDefaultReader()
        {
            return typeof(ResXResourceReader);
        }

        /// <devdoc>
        ///     Gets the default writer type associated with this set.
        /// </devdoc>
        public override Type GetDefaultWriter()
        {
            return typeof(ResXResourceWriter);
        }
    }
}
