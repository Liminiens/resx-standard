using Resx.Utility;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Xml;

namespace Resx.Resources
{
    /// <summary>
    ///     ResX resource reader.
    /// </summary>
    public class ResXResourceReader : IResourceReader
    {
        string fileName = null;
        TextReader reader = null;
        Stream stream = null;
        string fileContents = null;
        AssemblyName[] assemblyNames;
        string basePath;
        bool isReaderDirty = false;

        ITypeResolutionService typeResolver;
        IAliasResolver aliasResolver = null;

        ListDictionary resData = null;
        ListDictionary resMetadata = null;
        bool useResXDataNodes = false;


        private ResXResourceReader(ITypeResolutionService typeResolver)
        {
            this.typeResolver = typeResolver;
            aliasResolver = new ReaderAliasResolver();
        }

        private ResXResourceReader(AssemblyName[] assemblyNames)
        {
            this.assemblyNames = assemblyNames;
            aliasResolver = new ReaderAliasResolver();
        }

        public ResXResourceReader(string fileName)
            : this(fileName, (ITypeResolutionService) null, (IAliasResolver) null)
        {
        }

        public ResXResourceReader(string fileName, ITypeResolutionService typeResolver) : this(fileName, typeResolver,
            (IAliasResolver) null)
        {
        }

        internal ResXResourceReader(string fileName, ITypeResolutionService typeResolver, IAliasResolver aliasResolver)
        {
            this.fileName = fileName;
            this.typeResolver = typeResolver;
            this.aliasResolver = aliasResolver;
            if (this.aliasResolver == null)
            {
                this.aliasResolver = new ReaderAliasResolver();
            }
        }

        public ResXResourceReader(TextReader reader)
            : this(reader, (ITypeResolutionService) null, (IAliasResolver) null)
        {
        }

        public ResXResourceReader(TextReader reader, ITypeResolutionService typeResolver)
            : this(reader, typeResolver,
                (IAliasResolver) null)
        {
        }

        internal ResXResourceReader(
            TextReader reader,
            ITypeResolutionService typeResolver,
            IAliasResolver aliasResolver)
        {
            this.reader = reader;
            this.typeResolver = typeResolver;
            this.aliasResolver = aliasResolver;
            if (this.aliasResolver == null)
            {
                this.aliasResolver = new ReaderAliasResolver();
            }
        }


        public ResXResourceReader(Stream stream)
            : this(stream, (ITypeResolutionService) null, (IAliasResolver) null)
        {
        }

        public ResXResourceReader(Stream stream, ITypeResolutionService typeResolver)
            : this(stream, typeResolver,
                (IAliasResolver) null)
        {
        }

        internal ResXResourceReader(Stream stream, ITypeResolutionService typeResolver, IAliasResolver aliasResolver)
        {
            this.stream = stream;
            this.typeResolver = typeResolver;
            this.aliasResolver = aliasResolver;
            if (this.aliasResolver == null)
            {
                this.aliasResolver = new ReaderAliasResolver();
            }
        }

        public ResXResourceReader(Stream stream, AssemblyName[] assemblyNames) : this(stream, assemblyNames,
            (IAliasResolver) null)
        {
        }

        internal ResXResourceReader(Stream stream, AssemblyName[] assemblyNames, IAliasResolver aliasResolver)
        {
            this.stream = stream;
            this.assemblyNames = assemblyNames;
            this.aliasResolver = aliasResolver;
            if (this.aliasResolver == null)
            {
                this.aliasResolver = new ReaderAliasResolver();
            }
        }

        public ResXResourceReader(TextReader reader, AssemblyName[] assemblyNames) : this(reader, assemblyNames,
            (IAliasResolver) null)
        {
        }

        internal ResXResourceReader(TextReader reader, AssemblyName[] assemblyNames, IAliasResolver aliasResolver)
        {
            this.reader = reader;
            this.assemblyNames = assemblyNames;
            this.aliasResolver = aliasResolver;
            if (this.aliasResolver == null)
            {
                this.aliasResolver = new ReaderAliasResolver();
            }
        }

        public ResXResourceReader(string fileName, AssemblyName[] assemblyNames) : this(fileName, assemblyNames,
            (IAliasResolver) null)
        {
        }

        internal ResXResourceReader(string fileName, AssemblyName[] assemblyNames, IAliasResolver aliasResolver)
        {
            this.fileName = fileName;
            this.assemblyNames = assemblyNames;
            this.aliasResolver = aliasResolver;
            if (this.aliasResolver == null)
            {
                this.aliasResolver = new ReaderAliasResolver();
            }
        }

        ~ResXResourceReader()
        {
            Dispose(false);
        }

        /// <summary>
        ///     BasePath for relatives filepaths with ResXFileRefs.
        /// </summary>
        public string BasePath
        {
            get { return basePath; }
            set
            {
                if (isReaderDirty)
                {
                    throw new InvalidOperationException(SR.InvalidResXBasePathOperation);
                }

                basePath = value;
            }
        }

        /// <summary>
        ///     ResXFileRef's TypeConverter automatically unwraps it, creates the referenced
        ///     object and returns it. This property gives the user control over whether this unwrapping should
        ///     happen, or a ResXFileRef object should be returned. Default is true for backward compat and common case
        ///     scenario.
        /// </summary>
        public bool UseResXDataNodes
        {
            get { return useResXDataNodes; }
            set
            {
                if (isReaderDirty)
                {
                    throw new InvalidOperationException(SR.InvalidResXBasePathOperation);
                }

                useResXDataNodes = value;
            }
        }

        /// <summary>
        ///     Closes and files or streams being used by the reader.
        /// </summary>
        // NOTE: Part of IResourceReader - not protected by class level LinkDemand.
        public void Close()
        {
            ((IDisposable) this).Dispose();
        }

        /// <internalonly/>
        // NOTE: Part of IDisposable - not protected by class level LinkDemand.
        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (fileName != null && stream != null)
                {
                    stream.Close();
                    stream = null;
                }

                if (reader != null)
                {
                    reader.Close();
                    reader = null;
                }
            }
        }

        private void SetupNameTable(XmlReader xmlReader)
        {
            xmlReader.NameTable.Add(ResXResourceWriter.TypeStr);
            xmlReader.NameTable.Add(ResXResourceWriter.NameStr);
            xmlReader.NameTable.Add(ResXResourceWriter.DataStr);
            xmlReader.NameTable.Add(ResXResourceWriter.MetadataStr);
            xmlReader.NameTable.Add(ResXResourceWriter.MimeTypeStr);
            xmlReader.NameTable.Add(ResXResourceWriter.ValueStr);
            xmlReader.NameTable.Add(ResXResourceWriter.ResHeaderStr);
            xmlReader.NameTable.Add(ResXResourceWriter.VersionStr);
            xmlReader.NameTable.Add(ResXResourceWriter.ResMimeTypeStr);
            xmlReader.NameTable.Add(ResXResourceWriter.ReaderStr);
            xmlReader.NameTable.Add(ResXResourceWriter.WriterStr);
            xmlReader.NameTable.Add(ResXResourceWriter.BinSerializedObjectMimeType);
            xmlReader.NameTable.Add(ResXResourceWriter.SoapSerializedObjectMimeType);
            xmlReader.NameTable.Add(ResXResourceWriter.AssemblyStr);
            xmlReader.NameTable.Add(ResXResourceWriter.AliasStr);
        }

        /// <summary>
        ///     Demand loads the resource data.
        /// </summary>
        private void EnsureResData()
        {
            if (resData == null)
            {
                resData = new ListDictionary();
                resMetadata = new ListDictionary();

                XmlTextReader contentReader = null;

                try
                {
                    // Read data in any which way
                    if (fileContents != null)
                    {
                        contentReader = new XmlTextReader(new StringReader(fileContents));
                    }
                    else if (reader != null)
                    {
                        contentReader = new XmlTextReader(reader);
                    }
                    else if (fileName != null || stream != null)
                    {
                        if (stream == null)
                        {
                            stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        }

                        contentReader = new XmlTextReader(stream);
                    }

                    if (contentReader == null)
                        throw new InvalidOperationException("Content reader is null");

                    SetupNameTable(contentReader);
                    contentReader.WhitespaceHandling = WhitespaceHandling.None;
                    ParseXml(contentReader);
                }
                finally
                {
                    if (fileName != null && stream != null)
                    {
                        stream.Close();
                        stream = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Creates a reader with the specified file contents.
        /// </summary>
        public static ResXResourceReader FromFileContents(string fileContents)
        {
            return FromFileContents(fileContents, (ITypeResolutionService) null);
        }

        /// <internalonly/>
        /// <summary>
        ///     Creates a reader with the specified file contents.
        /// </summary>
        public static ResXResourceReader FromFileContents(string fileContents, ITypeResolutionService typeResolver)
        {
            ResXResourceReader result = new ResXResourceReader(typeResolver);
            result.fileContents = fileContents;
            return result;
        }

        /// <internalonly/>
        /// <summary>
        ///     Creates a reader with the specified file contents.
        /// </summary>
        public static ResXResourceReader FromFileContents(string fileContents, AssemblyName[] assemblyNames)
        {
            ResXResourceReader result = new ResXResourceReader(assemblyNames);
            result.fileContents = fileContents;
            return result;
        }

        // NOTE: Part of IEnumerable - not protected by class level LinkDemand.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        // NOTE: Part of IResourceReader - not protected by class level LinkDemand.
        public IDictionaryEnumerator GetEnumerator()
        {
            isReaderDirty = true;
            EnsureResData();
            return resData.GetEnumerator();
        }

        /// <summary>
        ///    Returns a dictionary enumerator that can be used to enumerate the <metadata> elements in the .resx file.
        /// </summary>
        public IDictionaryEnumerator GetMetadataEnumerator()
        {
            EnsureResData();
            return resMetadata.GetEnumerator();
        }

        /// <summary>
        ///    Attempts to return the line and column (Y, X) of the XML reader.
        /// </summary>
        private Point GetPosition(XmlReader reader)
        {
            Point pt = new Point(0, 0);
            IXmlLineInfo lineInfo = reader as IXmlLineInfo;

            if (lineInfo != null)
            {
                pt.Y = lineInfo.LineNumber;
                pt.X = lineInfo.LinePosition;
            }

            return pt;
        }

        private void ParseXml(XmlTextReader reader)
        {
            bool success = false;
            try
            {
                try
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            string s = reader.LocalName;

                            if (reader.LocalName.Equals(ResXResourceWriter.AssemblyStr))
                            {
                                ParseAssemblyNode(reader);
                            }
                            else if (reader.LocalName.Equals(ResXResourceWriter.DataStr))
                            {
                                ParseDataNode(reader, false);
                            }
                            else if (reader.LocalName.Equals(ResXResourceWriter.MetadataStr))
                            {
                                ParseDataNode(reader, true);
                            }
                        }
                    }

                    success = true;
                }
                catch (SerializationException se)
                {
                    Point pt = GetPosition(reader);
                    string newMessage = string.Format(
                        SR.SerializationException,
                        reader[ResXResourceWriter.TypeStr],
                        pt.Y, pt.X,
                        se.Message);
                    XmlException xml = new XmlException(newMessage, se, pt.Y, pt.X);
                    SerializationException newSe = new SerializationException(newMessage, xml);

                    throw newSe;
                }
                catch (TargetInvocationException tie)
                {
                    Point pt = GetPosition(reader);
                    string newMessage = string.Format(
                        SR.InvocationException,
                        reader[ResXResourceWriter.TypeStr],
                        pt.Y, pt.X,
                        tie.InnerException.Message);
                    XmlException xml = new XmlException(newMessage, tie.InnerException, pt.Y, pt.X);
                    TargetInvocationException newTie = new TargetInvocationException(newMessage, xml);

                    throw newTie;
                }
                catch (XmlException e)
                {
                    throw new ArgumentException(string.Format(SR.InvalidResXFile, e.Message), e);
                }
                catch (Exception e)
                {
                    if (ClientUtils.IsSecurityOrCriticalException(e))
                    {
                        throw;
                    }
                    else
                    {
                        Point pt = GetPosition(reader);
                        XmlException xmlEx = new XmlException(e.Message, e, pt.Y, pt.X);
                        throw new ArgumentException(string.Format(SR.InvalidResXFile, xmlEx.Message), xmlEx);
                    }
                }
            }
            finally
            {
                if (!success)
                {
                    resData = null;
                    resMetadata = null;
                }
            }
        }

        private void ParseAssemblyNode(XmlReader reader)
        {
            string alias = reader[ResXResourceWriter.AliasStr];
            string typeName = reader[ResXResourceWriter.NameStr];

            AssemblyName assemblyName = new AssemblyName(typeName);

            if (string.IsNullOrEmpty(alias))
            {
                alias = assemblyName.Name;
            }

            aliasResolver.PushAlias(alias, assemblyName);
        }


        private void ParseDataNode(XmlTextReader nodeReader, bool isMetaData)
        {
            DataNodeInfo nodeInfo = new DataNodeInfo();

            nodeInfo.Name = nodeReader[ResXResourceWriter.NameStr];
            string typeName = nodeReader[ResXResourceWriter.TypeStr];

            string alias = null;
            AssemblyName assemblyName = null;

            if (!string.IsNullOrEmpty(typeName))
            {
                alias = GetAliasFromTypeName(typeName);
            }

            if (!string.IsNullOrEmpty(alias))
            {
                assemblyName = aliasResolver.ResolveAlias(alias);
            }

            if (assemblyName != null)
            {
                nodeInfo.TypeName = GetTypeFromTypeName(typeName) + ", " + assemblyName.FullName;
            }
            else
            {
                nodeInfo.TypeName = nodeReader[ResXResourceWriter.TypeStr];
            }

            nodeInfo.MimeType = nodeReader[ResXResourceWriter.MimeTypeStr];

            bool finishedReadingDataNode = false;
            nodeInfo.ReaderPosition = GetPosition(nodeReader);
            while (!finishedReadingDataNode && nodeReader.Read())
            {
                if (nodeReader.NodeType == XmlNodeType.EndElement &&
                    (nodeReader.LocalName.Equals(ResXResourceWriter.DataStr) ||
                     nodeReader.LocalName.Equals(ResXResourceWriter.MetadataStr)))
                {
                    // we just found </data>, quit or </metadata>
                    finishedReadingDataNode = true;
                }
                else
                {
                    // could be a <value> or a <comment>
                    if (nodeReader.NodeType == XmlNodeType.Element)
                    {
                        if (nodeReader.Name.Equals(ResXResourceWriter.ValueStr))
                        {
                            WhitespaceHandling oldValue = nodeReader.WhitespaceHandling;
                            try
                            {
                                // based on the documentation at http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrfsystemxmlxmltextreaderclasswhitespacehandlingtopic.asp 
                                // this is ok because:
                                // "Because the XmlTextReader does not have DTD information available to it,
                                // SignificantWhitepsace nodes are only returned within the an xml:space='preserve' scope." 
                                // the xml:space would not be present for anything else than string and char (see ResXResourceWriter)
                                // so this would not cause any breaking change while reading data from Everett (we never outputed
                                // xml:space then) or from whidbey that is not specifically either a string or a char.
                                // However please note that manually editing a resx file in Everett and in Whidbey because of the addition
                                // of xml:space=preserve might have different consequences...
                                nodeReader.WhitespaceHandling = WhitespaceHandling.Significant;
                                nodeInfo.ValueData = nodeReader.ReadString();
                            }
                            finally
                            {
                                nodeReader.WhitespaceHandling = oldValue;
                            }
                        }
                        else if (nodeReader.Name.Equals(ResXResourceWriter.CommentStr))
                        {
                            nodeInfo.Comment = nodeReader.ReadString();
                        }
                    }
                    else
                    {
                        // weird, no <xxxx> tag, just the inside of <data> as text
                        nodeInfo.ValueData = nodeReader.Value.Trim();
                    }
                }
            }

            if (nodeInfo.Name == null)
            {
                throw new ArgumentException(string.Format(SR.InvalidResXResourceNoName, nodeInfo.ValueData));
            }

            ResXDataNode dataNode = new ResXDataNode(nodeInfo, BasePath);

            if (UseResXDataNodes)
            {
                resData[nodeInfo.Name] = dataNode;
            }
            else
            {
                IDictionary data = (isMetaData ? resMetadata : resData);
                if (assemblyNames == null)
                {
                    data[nodeInfo.Name] = dataNode.GetValue(typeResolver);
                }
                else
                {
                    data[nodeInfo.Name] = dataNode.GetValue(assemblyNames);
                }
            }
        }

        private string GetAliasFromTypeName(string typeName)
        {
            int indexStart = typeName.IndexOf(",");
            return typeName.Substring(indexStart + 2);
        }

        private string GetTypeFromTypeName(string typeName)
        {
            int indexStart = typeName.IndexOf(",");
            return typeName.Substring(0, indexStart);
        }


        private sealed class ReaderAliasResolver : IAliasResolver
        {
            private Hashtable cachedAliases;

            internal ReaderAliasResolver()
            {
                cachedAliases = new Hashtable();
            }

            public AssemblyName ResolveAlias(string alias)
            {
                AssemblyName result = null;
                if (cachedAliases != null)
                {
                    result = (AssemblyName) cachedAliases[alias];
                }

                return result;
            }

            public void PushAlias(string alias, AssemblyName name)
            {
                if (cachedAliases != null && !string.IsNullOrEmpty(alias))
                {
                    cachedAliases[alias] = name;
                }
            }
        }
    }
}