using System.Drawing;

namespace Resx.Resources
{
    internal class DataNodeInfo
    {
        public string Name;
        public string Comment;
        public string TypeName;
        public string MimeType;
        public string ValueData;
        public Point ReaderPosition; //only used to track position in the reader

        public DataNodeInfo Clone()
        {
            DataNodeInfo result = new DataNodeInfo();
            result.Name = Name;
            result.Comment = Comment;
            result.TypeName = TypeName;
            result.MimeType = MimeType;
            result.ValueData = ValueData;
            result.ReaderPosition = new Point(ReaderPosition.X, ReaderPosition.Y);
            return result;
        }
    }
}