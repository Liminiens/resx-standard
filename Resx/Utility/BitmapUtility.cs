using System;
using System.Drawing;
using System.IO;

namespace Resx.Utility
{
    internal static class BitmapUtility
    {
        public static Image CreateFromArray(byte[] array)
        {
            try
            {
                return Bitmap.FromStream(new MemoryStream(array));
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
