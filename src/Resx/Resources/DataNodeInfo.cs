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
            var result = new DataNodeInfo
            {
                Name = Name,
                Comment = Comment,
                TypeName = TypeName,
                MimeType = MimeType,
                ValueData = ValueData,
                ReaderPosition = new Point(ReaderPosition.X, ReaderPosition.Y)
            };
            return result;
        }
    }
}