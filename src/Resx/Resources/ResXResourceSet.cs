using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Resources;

namespace Resx.Resources
{
    /// <summary>
    ///     ResX resource set.
    /// </summary>
    public class ResXResourceSet : ResourceSet
    {
        /// <summary>
        ///     Creates a resource set for the specified file.
        /// </summary>
        [
            SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")  // Shipped like this in Everett.
        ]
        public ResXResourceSet(string fileName) : base(new ResXResourceReader(fileName))
        {
            ReadResources();
        }

        /// <summary>
        ///     Creates a resource set for the specified stream.
        /// </summary>
        [
            SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")  // Shipped like this in Everett.
        ]
        public ResXResourceSet(Stream stream) : base(new ResXResourceReader(stream))
        {
            ReadResources();
        }

        /// <summary>
        ///     Gets the default reader type associated with this set.
        /// </summary>
        public override Type GetDefaultReader()
        {
            return typeof(ResXResourceReader);
        }

        /// <summary>
        ///     Gets the default writer type associated with this set.
        /// </summary>
        public override Type GetDefaultWriter()
        {
            return typeof(ResXResourceWriter);
        }
    }
}
