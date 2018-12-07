using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

namespace Resx.Resources
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public class ResxFileRefStringConverter : TypeConverter
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context,
            Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context,
            Type destinationType)
        {
            return destinationType == typeof(string);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture,
            object value,
            Type destinationType)
        {
            object created = null;
            if (destinationType == typeof(string))
            {
                created = ((ResXFileRef) value).ToString();
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

                string[] parts = remainingString.Split(';');
                if (parts.Length > 1)
                {
                    result = new[] {fileName, parts[0], parts[1]};
                }
                else if (parts.Length > 0)
                {
                    result = new[] {fileName, parts[0]};
                }
                else
                {
                    result = new[] {fileName};
                }
            }

            return result;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public override object ConvertFrom(
            ITypeDescriptorContext context,
            CultureInfo culture,
            object value)
        {
            if (value is string stringValue)
            {
                string[] parts = ParseResxFileRefString(stringValue);
                string fileName = parts[0];
                string typeName = string.Join(",", parts[1].Split(',').Take(2));
                Type toCreate = Type.GetType(typeName, true);

                // special case string and byte[]
                if (toCreate == typeof(string))
                {
                    // we have a string, now we need to check the encoding
                    Encoding textFileEncoding = Encoding.Default;
                    if (parts.Length > 2)
                    {
                        textFileEncoding = Encoding.GetEncoding(parts[2]);
                    }

                    using (StreamReader sr = new StreamReader(fileName, textFileEncoding))
                    {
                        return sr.ReadToEnd();
                    }
                }

                // this is a regular file, we call it's constructor with a stream as a parameter
                // or if it's a byte array we just return that
                byte[] temp = null;

                using (FileStream s = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Debug.Assert(s != null, "Couldn't open " + fileName);
                    temp = new byte[s.Length];
                    s.Read(temp, 0, (int) s.Length);
                }

                if (toCreate == typeof(byte[]))
                {
                    return temp;
                }

                MemoryStream memStream = new MemoryStream(temp);
                if (toCreate == typeof(MemoryStream))
                {
                    return memStream;
                }

                if (toCreate == typeof(Bitmap) && fileName.EndsWith(".ico"))
                {
                    // we special case the .ico bitmaps because GDI+ destroy the alpha channel component and
                    // we don't want that to happen
                    Icon ico = new Icon(memStream);
                    return ico.ToBitmap();
                }

                return Activator.CreateInstance(
                    toCreate,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance,
                    null,
                    new object[] {memStream}, null);
            }

            return null;
        }
    }
}