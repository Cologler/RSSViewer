using System;
using System.IO;
using System.Text;

namespace RSSViewer.Utils
{
    public static class FileSystemAtomicOperations
    {
        public static void Write(string path, string text) => Write(path, Encoding.UTF8.GetBytes(text));

        public static void Write(string path, byte[] bytes) => Write(path, new MemoryStream(bytes));

        public static void Write(string path, Stream stream)
        {
            var tmp = path + $"{Guid.NewGuid()}.tmp";

            try
            {
                using (var fs = File.OpenWrite(tmp))
                {
                    stream.CopyTo(fs);
                    fs.Flush(true);
                }
            }
            catch (Exception)
            {
                if (File.Exists(tmp))
                {
                    File.Delete(tmp);
                }
                throw;
            }

            File.Move(tmp, path, true);
        }
    }
}
