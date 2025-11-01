using System;
using System.IO;
using System.Linq;

namespace SHUtil
{
    public static class PathUtil
    {
        public static bool IsValidPath(string path, bool checkExist = false)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                return false;

            // Check IsValidPathString?
            char[] invalidChars = Path.GetInvalidPathChars();
            if (invalidChars == null || invalidChars.Length <= 0 || path.Any(c => invalidChars.Contains(c) || char.IsControl(c)))
                return false;

            // Check IsValidPath?
            try
            {
                string rootPath = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(rootPath) || string.IsNullOrWhiteSpace(rootPath))
                    return false;

                string fullPath = Path.GetFullPath(path);
                if (string.IsNullOrEmpty(fullPath) || string.IsNullOrWhiteSpace(fullPath))
                    return false;

                if (checkExist && File.Exists(fullPath) == false && Directory.Exists(fullPath) == false)
                    return false;
            }
            catch (Exception e)
            {
                SHLog.LogError(e.ToString());
                return false;
            }

            return true;
        }
    }
}
