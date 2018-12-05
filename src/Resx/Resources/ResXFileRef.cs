using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;

namespace Resx.Resources
{
    /// <summary>
    ///     ResX File Reference class. This allows the developer to represent
    ///     a link to an external resource. When the resource manager asks
    ///     for the value of the resource item, the external resource is loaded.
    /// </summary>
    [TypeConverter(typeof(Converter))]
    [Serializable]
    public class ResXFileRef
    {
        private string fileName;
        private string typeName;
        [OptionalField(VersionAdded = 2)]
        private Encoding textFileEncoding;

        /// <summary>
        ///     Creates a new ResXFileRef that points to the specified file.
        ///     The type refered to by typeName must support a constructor
        ///     that accepts a System.IO.Stream as a parameter.
        /// </summary>
        public ResXFileRef(string fileName, string typeName)
        {
            if (fileName == null)
            {
                throw (new ArgumentNullException("fileName"));
            }
            if (typeName == null)
            {
                throw (new ArgumentNullException("typeName"));
            }
            this.fileName = fileName;
            this.typeName = typeName;
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
        public ResXFileRef(string fileName, string typeName, Encoding textFileEncoding) : this(fileName, typeName)
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
        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public string TypeName
        {
            get
            {
                return typeName;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public Encoding TextFileEncoding
        {
            get
            {
                return textFileEncoding;
            }
        }

        /// <summary>
        ///    path1+result = path2
        ///   A string which is the relative path difference between path1 and
        ///  path2 such that if path1 and the calculated difference are used
        ///  as arguments to Combine(), path2 is returned
        /// </summary>
        private static string PathDifference(string path1, string path2, bool compareCase)
        {
            int i;
            int si = -1;

            for (i = 0; (i < path1.Length) && (i < path2.Length); ++i)
            {
                if ((path1[i] != path2[i]) && (compareCase || (Char.ToLower(path1[i], CultureInfo.InvariantCulture) != Char.ToLower(path2[i], CultureInfo.InvariantCulture))))
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
                return String.Empty;
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

            if (basePath == null || basePath.Length == 0)
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

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public class Converter : TypeConverter
        {
            /// <summary>
            ///    <para>[To be supplied.]</para>
            /// </summary>
            public override bool CanConvertFrom(ITypeDescriptorContext context,
                Type sourceType)
            {
                if (sourceType == typeof(string))
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            ///    <para>[To be supplied.]</para>
            /// </summary>
            public override bool CanConvertTo(ITypeDescriptorContext context,
                Type destinationType)
            {
                if (destinationType == typeof(string))
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            ///    <para>[To be supplied.]</para>
            /// </summary>
            public override Object ConvertTo(ITypeDescriptorContext context,
                CultureInfo culture,
                Object value,
                Type destinationType)
            {
                Object created = null;
                if (destinationType == typeof(string))
                {
                    created = ((ResXFileRef)value).ToString();
                }
                return created;
            }

            // "value" is the parameter name of ConvertFrom, which calls this method.
            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")]
            internal static string[] ParseResxFileRefString(string stringValue)
            {
                string[] result = null;
                if (stringValue != null)
                {
                    stringValue = stringValue.Trim();
                    string fileName;
                    string remainingString;
                    if (stringValue.StartsWith("\""))
                    {
                        int lastIndexOfQuote = stringValue.LastIndexOf("\"");
                        if (lastIndexOfQuote - 1 < 0)
                            throw new ArgumentException("value");
                        fileName = stringValue.Substring(1, lastIndexOfQuote - 1); // remove the quotes in" ..... " 
                        if (lastIndexOfQuote + 2 > stringValue.Length)
                            throw new ArgumentException("value");
                        remainingString = stringValue.Substring(lastIndexOfQuote + 2);
                    }
                    else
                    {
                        int nextSemiColumn = stringValue.IndexOf(";");
                        if (nextSemiColumn == -1)
                            throw new ArgumentException("value");
                        fileName = stringValue.Substring(0, nextSemiColumn);
                        if (nextSemiColumn + 1 > stringValue.Length)
                            throw new ArgumentException("value");
                        remainingString = stringValue.Substring(nextSemiColumn + 1);
                    }
                    string[] parts = remainingString.Split(new char[] { ';' });
                    if (parts.Length > 1)
                    {
                        result = new string[] { fileName, parts[0], parts[1] };
                    }
                    else if (parts.Length > 0)
                    {
                        result = new string[] { fileName, parts[0] };
                    }
                    else
                    {
                        result = new string[] { fileName };
                    }
                }
                return result;
            }

            /// <summary>
            ///    <para>[To be supplied.]</para>
            /// </summary>
            [ResourceExposure(ResourceScope.Machine)]
            [ResourceConsumption(ResourceScope.Machine)]
            public override Object ConvertFrom(ITypeDescriptorContext context,
                CultureInfo culture,
                Object value)
            {
                Object created = null;
                string stringValue = value as string;
                if (stringValue != null)
                {
                    string[] parts = ParseResxFileRefString(stringValue);
                    string fileName = parts[0];
                    Type toCreate = Type.GetType(parts[1], true);

                    // special case string and byte[]
                    if (toCreate.Equals(typeof(string)))
                    {
                        // we have a string, now we need to check the encoding
                        Encoding textFileEncoding = Encoding.Default;
                        if (parts.Length > 2)
                        {
                            textFileEncoding = Encoding.GetEncoding(parts[2]);
                        }
                        using (StreamReader sr = new StreamReader(fileName, textFileEncoding))
                        {
                            created = sr.ReadToEnd();
                        }
                    }
                    else
                    {

                        // this is a regular file, we call it's constructor with a stream as a parameter
                        // or if it's a byte array we just return that
                        byte[] temp = null;

                        using (FileStream s = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            Debug.Assert(s != null, "Couldn't open " + fileName);
                            temp = new byte[s.Length];
                            s.Read(temp, 0, (int)s.Length);
                        }

                        if (toCreate.Equals(typeof(byte[])))
                        {
                            created = temp;
                        }
                        else
                        {
                            MemoryStream memStream = new MemoryStream(temp);
                            if (toCreate.Equals(typeof(MemoryStream)))
                            {
                                return memStream;
                            }
                            else if (toCreate.Equals(typeof(Bitmap)) && fileName.EndsWith(".ico"))
                            {
                                // we special case the .ico bitmaps because GDI+ destroy the alpha channel component and
                                // we don't want that to happen
                                Icon ico = new Icon(memStream);
                                created = ico.ToBitmap();
                            }
                            else
                            {
                                created = Activator.CreateInstance(toCreate, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, new Object[] { memStream }, null);
                            }
                        }
                    }
                }
                return created;
            }

        }
    }
}