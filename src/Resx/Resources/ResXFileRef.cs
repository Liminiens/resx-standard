using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Resx.Resources
{
    /// <summary>
    ///     ResX File Reference class. This allows the developer to represent
    ///     a link to an external resource. When the resource manager asks
    ///     for the value of the resource item, the external resource is loaded.
    /// </summary>
    [TypeConverter(typeof(ResxFileRefStringConverter))]
    [Serializable]
    public class ResXFileRef
    {
        private string fileName;
        private string typeName;
        [OptionalField(VersionAdded = 2)] private Encoding textFileEncoding;

        /// <summary>
        ///     Creates a new ResXFileRef that points to the specified file.
        ///     The type refered to by typeName must support a constructor
        ///     that accepts a System.IO.Stream as a parameter.
        /// </summary>
        public ResXFileRef(string fileName, string typeName)
        {
            this.fileName = fileName ?? throw (new ArgumentNullException(nameof(fileName)));
            this.typeName = typeName ?? throw (new ArgumentNullException(nameof(typeName)));
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
            textFileEncoding = null;
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMethodsAsStatic")]
        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
        }

        /// <summary>
        ///     Creates a new ResXFileRef that points to the specified file.
        ///     The type refered to by typeName must support a constructor
        ///     that accepts a System.IO.Stream as a parameter.
        /// </summary>
        public ResXFileRef(string fileName, string typeName, Encoding textFileEncoding)
            : this(fileName, typeName)
        {
            this.textFileEncoding = textFileEncoding;
        }

        internal ResXFileRef Clone()
        {
            return new ResXFileRef(fileName, typeName, textFileEncoding);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public string FileName => fileName;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public string TypeName => typeName;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public Encoding TextFileEncoding => textFileEncoding;

        /// <summary>
        ///  path1+result = path2
        ///  A string which is the relative path difference between path1 and
        ///  path2 such that if path1 and the calculated difference are used
        ///  as arguments to Combine(), path2 is returned
        /// </summary>
        private static string PathDifference(string path1, string path2, bool compareCase)
        {
            int i;
            int si = -1;

            for (i = 0; (i < path1.Length) && (i < path2.Length); ++i)
            {
                if ((path1[i] != path2[i]) && (compareCase ||
                                               (char.ToLower(path1[i], CultureInfo.InvariantCulture) !=
                                                char.ToLower(path2[i], CultureInfo.InvariantCulture))))
                {
                    break;
                }
                else if (path1[i] == Path.DirectorySeparatorChar)
                {
                    si = i;
                }
            }

            if (i == 0)
            {
                return path2;
            }

            if ((i == path1.Length) && (i == path2.Length))
            {
                return string.Empty;
            }

            StringBuilder relPath = new StringBuilder();

            for (; i < path1.Length; ++i)
            {
                if (path1[i] == Path.DirectorySeparatorChar)
                {
                    relPath.Append(".." + Path.DirectorySeparatorChar);
                }
            }

            return relPath.ToString() + path2.Substring(si + 1);
        }

        internal void MakeFilePathRelative(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                return;
            }

            fileName = PathDifference(basePath, fileName, false);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override string ToString()
        {
            string result = "";

            if (fileName.IndexOf(";") != -1 || fileName.IndexOf("\"") != -1)
            {
                result += ("\"" + fileName + "\";");
            }
            else
            {
                result += (fileName + ";");
            }

            result += typeName;
            if (textFileEncoding != null)
            {
                result += (";" + textFileEncoding.WebName);
            }

            return result;
        }
    }
}