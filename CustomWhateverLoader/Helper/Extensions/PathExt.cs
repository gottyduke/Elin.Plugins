using System;
using System.IO;

namespace Cwl.Helper.Extensions;

public static class PathExt
{
    extension(FileInfo file)
    {
        public bool IsInDirectory(DirectoryInfo dir)
        {
            var filePath = Path.GetFullPath(file.FullName);
            var dirPath = Path.GetFullPath(dir.FullName);

            if (string.Equals(filePath, dirPath, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }

            if (!filePath.StartsWith(dirPath, StringComparison.InvariantCultureIgnoreCase)) {
                return false;
            }

            var dirSeparator = dirPath[^1];
            if (dirSeparator == Path.DirectorySeparatorChar ||
                dirSeparator == Path.AltDirectorySeparatorChar) {
                return true;
            }

            return filePath.Length <= dirPath.Length ||
                   filePath[dirPath.Length] == Path.DirectorySeparatorChar ||
                   filePath[dirPath.Length] == Path.AltDirectorySeparatorChar;
        }
    }

    extension(DirectoryInfo dir)
    {
        public bool IsInDirectory(FileInfo file)
        {
            return file.IsInDirectory(dir);
        }
    }
}