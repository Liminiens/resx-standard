using Resx.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Versioning;
using System.Text;
using System.Xml;

namespace Resx.Resources
{
    [Serializable]
    public sealed class ResXDataNode : ISerializable
    {
        private static readonly char[] SpecialChars = new[] {' ', '\r', '\n'};

        private DataNodeInfo nodeInfo;

        private string name;
        private string comment;

        /// is only used when we create a resxdatanode manually with an object and contains the FQN
        private string typeName;

        private string fileRefFullPath;
        private string fileRefType;
        private string fileRefTextEncoding;

        private object value;
        private ResXFileRef fileRef;

        private IFormatter binaryFormatter = null;

        private static ITypeResolutionService internalTypeResolver =
            new AssemblyNamesTypeResolutionService(new AssemblyName[] { });

        // call back function to get type name for multitargeting.
        // No public property to force using constructors for the following reasons:
        // 1. one of the constructors needs this field (if used) to initialize the object, make it consistent with the other ctrs to avoid errors.
        // 2. once the object is constructed the delegate should not be changed to avoid getting inconsistent results.
        private Func<Type, string> typeNameConverter;

        // constructors

        private ResXDataNode()
        {
        }

        // <summary>
        // this is a deep clone
        //</summary>
        internal ResXDataNode DeepClone()
        {
            ResXDataNode result = new ResXDataNode
            {
                // nodeinfo is just made up of immutable objects, we don't need to clone it
                nodeInfo = nodeInfo?.Clone(),
                name = name,
                comment = comment,
                typeName = typeName,
                fileRefFullPath = fileRefFullPath,
                fileRefType = fileRefType,
                fileRefTextEncoding = fileRefTextEncoding,
                value = value,
                fileRef = fileRef?.Clone(),
                typeNameConverter = typeNameConverter
            };

            // we don't clone the value, because we don't know how
            return result;
        }


        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        [
            SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")
            // "name" is the name of the param passed in. So we don't have to localize it.
        ]
        public ResXDataNode(string name, object value, Func<Type, string> typeNameConverter)
        {
            if (name == null)
            {
                throw (new ArgumentNullException(nameof(name)));
            }

            if (name.Length == 0)
            {
                throw (new ArgumentException("name"));
            }

            this.typeNameConverter = typeNameConverter;

            Type valueType = (value == null) ? typeof(object) : value.GetType();
            if (value != null && !valueType.IsSerializable)
            {
                throw new InvalidOperationException(string.Format(SR.NotSerializableType, name, valueType.FullName));
            }
            else if (value != null)
            {
                typeName = MultitargetUtil.GetAssemblyQualifiedName(valueType, this.typeNameConverter);
            }

            this.name = name;
            this.value = value;
        }

        public ResXDataNode(string name, ResXFileRef fileRef)
            : this(name, fileRef, null)
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        [
            SuppressMessage("Microsoft.Globalization",
                "CA1303:DoNotPassLiteralsAsLocalizedParameters") // "name" is the name of the param passed in.
            // So we don't have to localize it.
        ]
        public ResXDataNode(string name, ResXFileRef fileRef, Func<Type, string> typeNameConverter)
        {
            if (name == null)
            {
                throw (new ArgumentNullException(nameof(name)));
            }

            if (name.Length == 0)
            {
                throw (new ArgumentException(nameof(name)));
            }

            this.name = name;
            this.fileRef = fileRef ?? throw (new ArgumentNullException(nameof(fileRef)));
            this.typeNameConverter = typeNameConverter;
        }

        internal ResXDataNode(DataNodeInfo nodeInfo, string basePath)
        {
            this.nodeInfo = nodeInfo;
            InitializeDataNode(basePath);
        }

        private void InitializeDataNode(string basePath)
        {
            // we can only use our internal type resolver here
            // because we only want to check if this is a ResXFileRef node
            // and we can't be sure that we have a typeResolutionService that can 
            // recognize this. It's not very clean but this should work.
            Type nodeType = null;
            if (nodeInfo.TypeName != null)
            {
                if (nodeInfo.TypeName.Contains("ResXFileRef"))
                {
                    nodeType = typeof(ResXFileRef);
                }
                else if (nodeInfo.TypeName.Contains("ResXNullRef"))
                {
                    nodeType = typeof(ResXNullRef);
                }
                else
                {
                    nodeType = internalTypeResolver.GetType(nodeInfo.TypeName, false, true);
                }
            }
            else
            {
                nodeType = typeof(string);
            }

            if (nodeType != null && nodeType == typeof(ResXFileRef))
            {
                // we have a fileref, split the value data and populate the fields
                string[] fileRefDetails = ResxFileRefStringConverter.ParseResxFileRefString(nodeInfo.ValueData);
                if (fileRefDetails != null && fileRefDetails.Length > 1)
                {
                    if (!Path.IsPathRooted(fileRefDetails[0]) && basePath != null)
                    {
                        fileRefFullPath = Path.Combine(basePath, fileRefDetails[0]);
                    }
                    else
                    {
                        fileRefFullPath = fileRefDetails[0];
                    }

                    fileRefType = Type.GetType(string.Join(",", fileRefDetails[1].Split(',').Take(2)), true)
                        .AssemblyQualifiedName;
                    if (fileRefDetails.Length > 2)
                    {
                        fileRefTextEncoding = fileRefDetails[2];
                    }
                }
            }
        }

        public string Comment
        {
            get
            {
                string result = comment;
                if (result == null && nodeInfo != null)
                {
                    result = nodeInfo.Comment;
                }

                return result ?? "";
            }
            set { comment = value; }
        }

        public string Name
        {
            get
            {
                string result = name;
                if (result == null && nodeInfo != null)
                {
                    result = nodeInfo.Name;
                }

                return result;
            }
            [
                SuppressMessage("Microsoft.Globalization",
                    "CA1303:DoNotPassLiteralsAsLocalizedParameters") // "Name" is the name of the property.
                // So we don't have to localize it.
            ]
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(Name));
                }

                if (value.Length == 0)
                {
                    throw new ArgumentException(nameof(Name));
                }

                name = value;
            }
        }

        public ResXFileRef FileRef
        {
            get
            {
                if (FileRefFullPath == null)
                {
                    return null;
                }

                if (fileRef == null)
                {
                    if (string.IsNullOrEmpty(fileRefTextEncoding))
                    {
                        fileRef = new ResXFileRef(FileRefFullPath, FileRefType);
                    }
                    else
                    {
                        fileRef = new ResXFileRef(FileRefFullPath, FileRefType,
                            Encoding.GetEncoding(FileRefTextEncoding));
                    }
                }

                return fileRef;
            }
        }

        private string FileRefFullPath
        {
            get
            {
                string result = fileRef?.FileName;
                if (result == null)
                {
                    result = fileRefFullPath;
                }

                return result;
            }
        }

        private string FileRefType
        {
            get
            {
                string result = fileRef?.TypeName;
                if (result == null)
                {
                    result = fileRefType;
                }

                return result;
            }
        }

        private string FileRefTextEncoding
        {
            get
            {
                string result = fileRef?.TextFileEncoding?.BodyName;
                if (result == null)
                {
                    result = fileRefTextEncoding;
                }

                return result;
            }
        }

        private static string ToBase64WrappedString(byte[] data)
        {
            const int lineWrap = 80;
            const string crlf = "\r\n";
            const string prefix = "        ";
            string raw = Convert.ToBase64String(data);
            if (raw.Length > lineWrap)
            {
                // word wrap on lineWrap chars, \r\n
                StringBuilder output = new StringBuilder(raw.Length + (raw.Length / lineWrap) * 3);
                int current = 0;
                for (; current < raw.Length - lineWrap; current += lineWrap)
                {
                    output.Append(crlf);
                    output.Append(prefix);
                    output.Append(raw, current, lineWrap);
                }

                output.Append(crlf);
                output.Append(prefix);
                output.Append(raw, current, raw.Length - current);
                output.Append(crlf);
                return output.ToString();
            }
            else
            {
                return raw;
            }
        }

        private void FillDataNodeInfoFromObject(DataNodeInfo nodeInfo, object value)
        {
            if (value is CultureInfo ci)
            {
                // special-case CultureInfo, cannot use CultureInfoConverter for serialization
                nodeInfo.ValueData = ci.Name;
                nodeInfo.TypeName =
                    MultitargetUtil.GetAssemblyQualifiedName(typeof(CultureInfo), typeNameConverter);
            }
            else if (value is string str)
            {
                nodeInfo.ValueData = str;
            }
            else if (value is byte[] bytes)
            {
                nodeInfo.ValueData = ToBase64WrappedString(bytes);
                nodeInfo.TypeName = MultitargetUtil.GetAssemblyQualifiedName(typeof(byte[]), typeNameConverter);
            }
            else
            {
                Type valueType = (value == null) ? typeof(object) : value.GetType();
                if (value != null && !valueType.IsSerializable)
                {
                    throw new InvalidOperationException(string.Format(SR.NotSerializableType, name,
                        valueType.FullName));
                }

                TypeConverter tc = TypeDescriptor.GetConverter(valueType);
                bool toString = tc.CanConvertTo(typeof(string));
                bool fromString = tc.CanConvertFrom(typeof(string));
                try
                {
                    if (toString && fromString)
                    {
                        nodeInfo.ValueData = tc.ConvertToInvariantString(value);
                        nodeInfo.TypeName = MultitargetUtil.GetAssemblyQualifiedName(valueType, typeNameConverter);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Some custom type converters will throw in ConvertTo(string)
                    // to indicate that this object should be serialized through ISeriazable
                    // instead of as a string. This is semi-wrong, but something we will have to
                    // live with to allow user created Cursors to be serializable.
                    if (ClientUtils.IsSecurityOrCriticalException(ex))
                    {
                        throw;
                    }
                }

                bool toByteArray = tc.CanConvertTo(typeof(byte[]));
                bool fromByteArray = tc.CanConvertFrom(typeof(byte[]));
                if (toByteArray && fromByteArray)
                {
                    byte[] data = (byte[]) tc.ConvertTo(value, typeof(byte[]));
                    string text = ToBase64WrappedString(data);
                    nodeInfo.ValueData = text;
                    nodeInfo.MimeType = ResXResourceWriter.ByteArraySerializedObjectMimeType;
                    nodeInfo.TypeName = MultitargetUtil.GetAssemblyQualifiedName(valueType, typeNameConverter);
                    return;
                }

                if (value == null)
                {
                    nodeInfo.ValueData = string.Empty;
                    nodeInfo.TypeName =
                        MultitargetUtil.GetAssemblyQualifiedName(typeof(ResXNullRef), typeNameConverter);
                }
                else
                {
                    if (binaryFormatter == null)
                    {
                        binaryFormatter = new BinaryFormatter();
                        binaryFormatter.Binder = new ResXSerializationBinder(typeNameConverter);
                    }

                    MemoryStream ms = new MemoryStream();
                    binaryFormatter.Serialize(ms, value);
                    string text = ToBase64WrappedString(ms.ToArray());
                    nodeInfo.ValueData = text;
                    nodeInfo.MimeType = ResXResourceWriter.DefaultSerializedObjectMimeType;
                }
            }
        }

        private object GenerateObjectFromDataNodeInfo(DataNodeInfo dataNodeInfo, ITypeResolutionService typeResolver)
        {
            object result = null;
            string mimeTypeName = dataNodeInfo.MimeType;
            // default behavior: if we dont have a type name, it's a string
            string typeName =
                string.IsNullOrEmpty(dataNodeInfo.TypeName)
                    ? MultitargetUtil.GetAssemblyQualifiedName(typeof(string), typeNameConverter)
                    : dataNodeInfo.TypeName;

            if (!string.IsNullOrEmpty(mimeTypeName))
            {
                if (string.Equals(mimeTypeName, ResXResourceWriter.BinSerializedObjectMimeType)
                    || string.Equals(mimeTypeName, ResXResourceWriter.Beta2CompatSerializedObjectMimeType)
                    || string.Equals(mimeTypeName, ResXResourceWriter.CompatBinSerializedObjectMimeType))
                {
                    string text = dataNodeInfo.ValueData;
                    byte[] serializedData;
                    serializedData = FromBase64WrappedString(text);

                    if (binaryFormatter == null)
                    {
                        binaryFormatter = new BinaryFormatter();
                        binaryFormatter.Binder = new ResXSerializationBinder(typeResolver);
                    }

                    IFormatter formatter = binaryFormatter;
                    if (serializedData != null && serializedData.Length > 0)
                    {
                        result = formatter.Deserialize(new MemoryStream(serializedData));
                        if (result is ResXNullRef)
                        {
                            result = null;
                        }
                    }
                }

                else if (string.Equals(mimeTypeName, ResXResourceWriter.ByteArraySerializedObjectMimeType))
                {
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        Type type = ResolveType(typeName, typeResolver);
                        if (type != null)
                        {
                            TypeConverter tc = TypeDescriptor.GetConverter(type);
                            string text = dataNodeInfo.ValueData;
                            byte[] serializedData = FromBase64WrappedString(text);
                            if (tc.CanConvertFrom(typeof(byte[])))
                            {
                                if (serializedData != null)
                                {
                                    result = tc.ConvertFrom(serializedData);
                                }
                            }
                            else if (serializedData != null)
                            {
                                var bitmap = BitmapUtility.CreateFromArray(serializedData);
                                if (bitmap != null)
                                {
                                    result = bitmap;
                                }
                            }
                        }
                        else
                        {
                            string newMessage = string.Format(SR.TypeLoadException, typeName,
                                dataNodeInfo.ReaderPosition.Y, dataNodeInfo.ReaderPosition.X);
                            XmlException xml = new XmlException(newMessage, null, dataNodeInfo.ReaderPosition.Y,
                                dataNodeInfo.ReaderPosition.X);
                            TypeLoadException newTle = new TypeLoadException(newMessage, xml);

                            throw newTle;
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(typeName))
            {
                Type type = ResolveType(typeName, typeResolver);
                if (type != null)
                {
                    if (type == typeof(ResXNullRef))
                    {
                        result = null;
                    }
                    else if (typeName.IndexOf("System.Byte[]") != -1 && typeName.IndexOf("mscorlib") != -1)
                    {
                        // Handle byte[]'s, which are stored as base-64 encoded strings.
                        // We can't hard-code byte[] type name due to version number
                        // updates & potential whitespace issues with ResX files.
                        result = FromBase64WrappedString(dataNodeInfo.ValueData);
                    }
                    else
                    {
                        TypeConverter tc = TypeDescriptor.GetConverter(type);
                        if (tc.CanConvertFrom(typeof(string)))
                        {
                            string text = dataNodeInfo.ValueData;
                            try
                            {
                                result = tc.ConvertFromInvariantString(text);
                            }
                            catch (NotSupportedException nse)
                            {
                                string newMessage = string.Format(SR.NotSupported, typeName,
                                    dataNodeInfo.ReaderPosition.Y, dataNodeInfo.ReaderPosition.X, nse.Message);
                                XmlException xml = new XmlException(newMessage, nse, dataNodeInfo.ReaderPosition.Y,
                                    dataNodeInfo.ReaderPosition.X);
                                NotSupportedException newNse = new NotSupportedException(newMessage, xml);
                                throw newNse;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("ResxFileRefStringConverter for " + type.FullName +
                                            " doesn't support string conversion");
                        }
                    }
                }
                else
                {
                    string newMessage = string.Format(SR.TypeLoadException, typeName, dataNodeInfo.ReaderPosition.Y,
                        dataNodeInfo.ReaderPosition.X);
                    XmlException xml = new XmlException(newMessage, null, dataNodeInfo.ReaderPosition.Y,
                        dataNodeInfo.ReaderPosition.X);
                    TypeLoadException newTle = new TypeLoadException(newMessage, xml);

                    throw newTle;
                }
            }
            else
            {
                // if mimeTypeName and typeName are not filled in, the value must be a string
                Debug.Assert(value is string, "Resource entries with no Type or MimeType must be encoded as strings");
            }

            return result;
        }

        internal DataNodeInfo GetDataNodeInfo()
        {
            bool shouldSerialize = true;
            if (nodeInfo != null)
            {
                shouldSerialize = false;
            }
            else
            {
                nodeInfo = new DataNodeInfo();
            }

            nodeInfo.Name = Name;
            nodeInfo.Comment = Comment;

            // We always serialize if this node represents a FileRef. This is because FileRef is a public property,
            // so someone could have modified it.
            if (shouldSerialize || FileRefFullPath != null)
            {
                // if we dont have a datanodeinfo it could be either
                // a direct object OR a fileref
                if (FileRefFullPath != null)
                {
                    nodeInfo.ValueData = FileRef.ToString();
                    nodeInfo.MimeType = null;
                    nodeInfo.TypeName =
                        MultitargetUtil.GetAssemblyQualifiedName(typeof(ResXFileRef), typeNameConverter);
                }
                else
                {
                    // serialize to string inside the nodeInfo
                    FillDataNodeInfoFromObject(nodeInfo, value);
                }
            }

            return nodeInfo;
        }

        /// <summary>
        ///    Might return the position in the resx file of the current node, if known
        ///    otherwise, will return Point(0,0) since point is a struct 
        /// </summary>
        public Point GetNodePosition()
        {
            if (nodeInfo == null)
            {
                return new Point();
            }
            else
            {
                return nodeInfo.ReaderPosition;
            }
        }

        /// <summary>
        ///    Get the FQ type name for this datanode.
        ///    We return typeof(object) for ResXNullRef
        /// </summary>
        public string GetValueTypeName(ITypeResolutionService typeResolver)
        {
            // the type name here is always a FQN
            if (!string.IsNullOrEmpty(typeName))
            {
                if (typeName.Equals(
                    MultitargetUtil.GetAssemblyQualifiedName(typeof(ResXNullRef), typeNameConverter)))
                {
                    return MultitargetUtil.GetAssemblyQualifiedName(typeof(object), typeNameConverter);
                }
                else
                {
                    return typeName;
                }
            }

            string result = FileRefType;
            Type objectType = null;
            // do we have a fileref?
            if (result != null)
            {
                // try to resolve this type
                objectType = ResolveType(FileRefType, typeResolver);
            }
            else if (nodeInfo != null)
            {
                // we dont have a fileref, try to resolve the type of the datanode
                result = nodeInfo.TypeName;
                // if typename is null, the default is just a string
                if (string.IsNullOrEmpty(result))
                {
                    // we still dont know... do we have a mimetype? if yes, our only option is to 
                    // deserialize to know what we're dealing with... very inefficient...
                    if (!string.IsNullOrEmpty(nodeInfo.MimeType))
                    {
                        object insideObject = null;

                        try
                        {
                            insideObject = GenerateObjectFromDataNodeInfo(nodeInfo, typeResolver);
                        }
                        catch (Exception ex)
                        {
                            // it'd be better to catch SerializationException but the underlying type resolver
                            // can throw things like FileNotFoundException which is kinda confusing, so I am catching all here..
                            if (ClientUtils.IsCriticalException(ex))
                            {
                                throw;
                            }

                            // something went wrong, type is not specified at all or stream is corrupted
                            // return system.object
                            result = MultitargetUtil.GetAssemblyQualifiedName(typeof(object), typeNameConverter);
                        }

                        if (insideObject != null)
                        {
                            result = MultitargetUtil.GetAssemblyQualifiedName(insideObject.GetType(),
                                typeNameConverter);
                        }
                    }
                    else
                    {
                        // no typename, no mimetype, we have a string...
                        result = MultitargetUtil.GetAssemblyQualifiedName(typeof(string), typeNameConverter);
                    }
                }
                else
                {
                    objectType = ResolveType(nodeInfo.TypeName, typeResolver);
                }
            }

            if (objectType != null)
            {
                if (objectType == typeof(ResXNullRef))
                {
                    result = MultitargetUtil.GetAssemblyQualifiedName(typeof(object), typeNameConverter);
                }
                else
                {
                    result = MultitargetUtil.GetAssemblyQualifiedName(objectType, typeNameConverter);
                }
            }

            return result;
        }

        /// <summary>
        ///    Get the FQ type name for this datanode
        /// </summary>
        public string GetValueTypeName(AssemblyName[] names)
        {
            return GetValueTypeName(new AssemblyNamesTypeResolutionService(names));
        }

        /// <summary>
        ///    Get the value contained in this datanode
        /// </summary>
        public object GetValue(ITypeResolutionService typeResolver)
        {
            if (value != null)
            {
                return value;
            }

            object result = null;
            if (FileRefFullPath != null)
            {
                Type objectType = ResolveType(FileRefType, typeResolver);
                if (objectType != null)
                {
                    // we have the FQN for this type
                    if (FileRefTextEncoding != null)
                    {
                        fileRef = new ResXFileRef(FileRefFullPath, objectType.AssemblyQualifiedName,
                            Encoding.GetEncoding(FileRefTextEncoding));
                    }
                    else
                    {
                        fileRef = new ResXFileRef(FileRefFullPath, objectType.AssemblyQualifiedName);
                    }

                    TypeConverter tc = TypeDescriptor.GetConverter(typeof(ResXFileRef));
                    result = tc.ConvertFrom(fileRef.ToString());
                }
                else
                {
                    string newMessage = string.Format(SR.TypeLoadExceptionShort, FileRefType);
                    TypeLoadException newTle = new TypeLoadException(newMessage);
                    throw (newTle);
                }
            }
            else if (nodeInfo.ValueData != null)
            {
                // it's embedded, we deserialize it
                result = GenerateObjectFromDataNodeInfo(nodeInfo, typeResolver);
            }
            else
            {
                // schema is wrong and say minOccur for Value is 0,
                // but it's too late to change it...
                // we need to return null here
                return null;
            }

            return result;
        }

        /// <summary>
        ///    Get the value contained in this datanode
        /// </summary>
        public object GetValue(AssemblyName[] names)
        {
            return GetValue(new AssemblyNamesTypeResolutionService(names));
        }

        private static byte[] FromBase64WrappedString(string text)
        {
            if (text.IndexOfAny(SpecialChars) != -1)
            {
                StringBuilder sb = new StringBuilder(text.Length);
                for (int i = 0; i < text.Length; i++)
                {
                    switch (text[i])
                    {
                        case ' ':
                        case '\r':
                        case '\n':
                            break;
                        default:
                            sb.Append(text[i]);
                            break;
                    }
                }

                return Convert.FromBase64String(sb.ToString());
            }
            else
            {
                return Convert.FromBase64String(text);
            }
        }

        private Type ResolveType(string typeName, ITypeResolutionService typeResolver)
        {
            if (typeName.Contains(", System.Drawing"))
            {
                typeName = typeName.Replace(", System.Drawing", ", System.Drawing.Common");
            }

            Type t = null;
            if (typeResolver != null)
            {
                // If we cannot find the strong-named type, then try to see
                // if the TypeResolver can bind to partial names. For this, 
                // we will strip out the partial names and keep the rest of the
                // strong-name information to try again.

                t = typeResolver.GetType(typeName, false);
                if (t == null)
                {
                    string[] typeParts = typeName.Split(',');

                    // Break up the type name from the rest of the assembly strong name.
                    //
                    if (typeParts.Length >= 2)
                    {
                        string partialName = typeParts[0].Trim();
                        string assemblyName = typeParts[1].Trim();
                        partialName = partialName + ", " + assemblyName;
                        t = typeResolver.GetType(partialName, false);
                    }
                }
            }

            if (t == null)
            {
                t = Type.GetType(typeName, false);

                if (t == null)
                {
                    var testTypeName = string.Join(",", typeName.Split(',').Take(2));
                    if (testTypeName.Length > 0)
                    {
                        t = Type.GetType(testTypeName, false);
                    }
                }
            }

            return t;
        }

        /// <summary>
        ///    Get the value contained in this datanode
        /// </summary>
        // NOTE: No LinkDemand for SerializationFormatter necessary here, since this class already
        // has a FullTrust LinkDemand.
        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            DataNodeInfo nodeInfo = GetDataNodeInfo();
            si.AddValue("Name", nodeInfo.Name, typeof(string));
            si.AddValue("Comment", nodeInfo.Comment, typeof(string));
            si.AddValue("TypeName", nodeInfo.TypeName, typeof(string));
            si.AddValue("MimeType", nodeInfo.MimeType, typeof(string));
            si.AddValue("ValueData", nodeInfo.ValueData, typeof(string));
        }

        private ResXDataNode(SerializationInfo info, StreamingContext context)
        {
            DataNodeInfo nodeInfo = new DataNodeInfo();
            nodeInfo.Name = (string) info.GetValue("Name", typeof(string));
            nodeInfo.Comment = (string) info.GetValue("Comment", typeof(string));
            nodeInfo.TypeName = (string) info.GetValue("TypeName", typeof(string));
            nodeInfo.MimeType = (string) info.GetValue("MimeType", typeof(string));
            nodeInfo.ValueData = (string) info.GetValue("ValueData", typeof(string));
            this.nodeInfo = nodeInfo;
            InitializeDataNode(null);
        }
    }
}