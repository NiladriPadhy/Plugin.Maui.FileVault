namespace Plugin.Maui.FileVault;

internal static class SecureWipe
{
    public static void File(string path, bool overwrite)
    {
        if (!System.IO.File.Exists(path))
        {
            return;
        }

        if (overwrite)
        {
            try
            {
                var length = new FileInfo(path).Length;
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
                var buffer = new byte[Math.Min(8192, Math.Max(1, length))];
                var remaining = length;
                while (remaining > 0)
                {
                    var n = (int)Math.Min(buffer.Length, remaining);
                    stream.Write(buffer, 0, n);
                    remaining -= n;
                }

                stream.Flush(true);
            }
            catch (IOException)
            {
                // Still attempt the delete below.
            }
        }

        System.IO.File.Delete(path);
    }
}
