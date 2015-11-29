using System;
using System.IO;
using System.IO.Compression;

namespace xSocks.Controller
{
    public class FileManager
    {
        public static bool ByteArrayToFile(string fileName, byte[] content)
        {
            try
            {
                FileStream fileStream =
                   new FileStream(fileName, FileMode.Create, FileAccess.Write);
                fileStream.Write(content, 0, content.Length);
                fileStream.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
            }
            return false;
        }

        public static void UncompressFile(string fileName, byte[] content)
        {
            FileStream destinationFile = File.Create(fileName);

            // Because the uncompressed size of the file is unknown, 
            // we are using an arbitrary buffer size.
            byte[] buffer = new byte[4096];
            int n;

            using (GZipStream input = new GZipStream(new MemoryStream(content),
                CompressionMode.Decompress, false))
            {
                while (true)
                {
                    n = input.Read(buffer, 0, buffer.Length);
                    if (n == 0)
                    {
                        break;
                    }
                    destinationFile.Write(buffer, 0, n);
                }
            }
            destinationFile.Close();
        }
    }
}
