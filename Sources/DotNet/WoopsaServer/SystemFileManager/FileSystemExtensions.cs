using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Concept
{
    public static class FileSystemExtensions
    {
        #region Writer

        public static void AppendAllLines(this IFileSystem fileSystem, string path, IEnumerable<string> contents)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.AppendAllLines(path, contents, Encoding.Default);
        }

        public static void AppendAllLines(this IFileSystem fileSystem, string path, IEnumerable<string> contents, Encoding encoding)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.AppendAllText(path, LinesToString(fileSystem.NewLine, contents), encoding);
        }

        public static void AppendAllText(this IFileSystem fileSystem, string path, string contents)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.AppendAllText(path, contents, Encoding.Default);
        }

        public static void AppendAllText(this IFileSystem fileSystem, string path, string contents, Encoding encoding)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.AppendAllBytes(path, encoding.GetBytes(contents));
        }

        public static void WriteAllLines(this IFileSystem fileSystem, string path, IEnumerable<string> contents)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.WriteAllLines(path, contents, Encoding.Default);
        }

        public static void WriteAllLines(this IFileSystem fileSystem, string path, IEnumerable<string> contents, Encoding encoding)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.WriteAllText(path, LinesToString(fileSystem.NewLine, contents), encoding);
        }

        public static void WriteAllText(this IFileSystem fileSystem, string path, string contents)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.WriteAllText(path, contents, Encoding.Default);
        }

        public static void WriteAllText(this IFileSystem fileSystem, string path, string contents, Encoding encoding)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            fileSystem.WriteAllBytes(path, encoding.GetBytes(contents));
        }

        #endregion

        #region Reader

        public static string[] ReadAllLines(this IFileSystem fileSystem, string path)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            return StringToLines(fileSystem.ReadAllText(path));
        }

        public static string[] ReadAllLines(this IFileSystem fileSystem, string path, Encoding encoding)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            return StringToLines(fileSystem.ReadAllText(path, encoding));
        }

        public static string ReadAllText(this IFileSystem fileSystem, string path)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            byte[] data = fileSystem.ReadAllBytes(path);
            Encoding encoding = GetFileEncoding(data);

            data = RemoveBom(encoding, data);
            string text = encoding.GetString(data);
            text = text.Replace("\0", "");

            return text;
        }

        public static string ReadAllText(this IFileSystem fileSystem, string path, Encoding encoding)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            byte[] data = RemoveBom(encoding, fileSystem.ReadAllBytes(path));
            string text = encoding.GetString(data);
            text = text.Replace("\0", "");

            return text;
        }

        #endregion

        #region Utils

        public static string ChangeExtension(this IFileSystem _, string path, string extension) =>
            Path.ChangeExtension(path, extension);

        public static string GetExtension(this IFileSystem _, string path) =>
            Path.GetExtension(path);

        public static string GetFileName(this IFileSystem _, string path) =>
            Path.GetFileName(path);

        public static string GetFileNameWithoutExtension(this IFileSystem _, string path) =>
            Path.GetFileNameWithoutExtension(path);

        /// <summary>
        /// Creates a directory at the specified path.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="path">The path where should be created the directory.</param>
        /// <param name="overwrite">A value indicating if the directory should be overridden when already existing.</param>
        public static void CreateDirectory(this IFileSystem fileSystem, string path, bool overwrite)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            if (fileSystem.DirectoryExists(path))
            {
                if (overwrite)
                {
                    fileSystem.DeleteDirectory(path, true);
                    fileSystem.CreateDirectory(path);
                }
            }
            else
            {
                fileSystem.CreateDirectory(path);
            }
        }

        public static string GetFileLocation(this IFileSystem _, string path)
        {
            string result = null;
            if (path != null)
            {
                int lastIndex = path.LastIndexOfAny(_pathVolumeSeparators);
                result = lastIndex != -1 ? path.Substring(0, lastIndex + 1) : string.Empty;
            }
            return result;
        }

        public static string AppendBeginSeparator(this IFileSystem _, string path)
        {
            char pathSeparator = UsedPathSeparator(path);
            if (!string.IsNullOrEmpty(path))
            {
                return path[0] != pathSeparator ? pathSeparator + path : path;
            }

            return pathSeparator + string.Empty;
        }

        public static string AppendTrailingSeparator(this IFileSystem _, string path)
        {
            char pathSeparator = UsedPathSeparator(path);
            if (!string.IsNullOrEmpty(path))
            {
                return path[path.Length - 1] != pathSeparator ? path + pathSeparator : path;
            }

            return string.Empty + pathSeparator;
        }

        public static string RemoveBeginSeparator(this IFileSystem _, string path)
        {
            if (path.Length >= 1 && IsPathSeparator(path[0]))
            {
                return path.Substring(1, path.Length - 1);
            }

            return path;
        }

        public static string RemoveTrailingSeparator(this IFileSystem _, string path)
        {
            if (path.Length >= 1 && IsPathSeparator(path[path.Length - 1]))
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }

        public static string CombinePath(this IFileSystem fileSystem, string path1, params string[] paths)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            string result = path1;
            foreach (string path in paths)
            {
                result = fileSystem.InternalCombinePath(result, path);
            }
            return result;
        }

        public static string MergePath(this IFileSystem fileSystem, string basePath, params string[] paths)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            return fileSystem.CombinePath(basePath, paths);
        }

        public static bool IsPathRooted(this IFileSystem _, string path) => Path.IsPathRooted(path);

        /// <summary>
        /// Gets the absolute file path from a relative file path and a reference path.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="path">The relative file path.</param>
        /// <param name="referencePath">The reference path.</param>
        /// <returns>A new <see cref="string"/> representing an absolute file path.</returns>
        public static string GetAbsoluteFilePath(this IFileSystem fileSystem, string path, string referencePath)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            if (!fileSystem.IsPathRooted(referencePath))
                throw new ArgumentException(@"The path must be a rooted absolute path.", nameof(referencePath));

            if (fileSystem.IsPathRooted(path))
                return path;

            return fileSystem.MergePath(referencePath, path);
        }

        #endregion

        #region Private Helpers

        private static string InternalCombinePath(this IFileSystem fileSystem, string path1, string path2)
        {
            string NormalizePath(string path)
            {
                var result = new StringBuilder();
                char pathSeparator = UsedPathSeparator(path);
                foreach (char item in path)
                    result.Append(IsPathSeparator(item) ? pathSeparator : item);
                return result.ToString();
            }

            string p1Normalized = fileSystem.AppendTrailingSeparator(NormalizePath(path1));
            string p2Normalized = NormalizePath(path2);

            return path1 != string.Empty ? p1Normalized + p2Normalized : p2Normalized;
        }

        private static Encoding GetFileEncoding(byte[] data)
        {
            if (data.Length > 2 && data[0] == 0xef && data[1] == 0xbb && data[2] == 0xbf)
                return Encoding.UTF8;
            else if (data.Length > 1 && data[0] == 0xff && data[1] == 0xfe)
                return Encoding.Unicode;
            else if (data.Length > 1 && data[0] == 0xfe && data[1] == 0xff)
                return Encoding.BigEndianUnicode;
            else if (data.Length > 4 && data[0] == 0xff && data[1] == 0xfe && data[2] == 0 && data[3] == 0)
                return Encoding.UTF32;
            else if (data.Length > 4 && data[0] == 0 && data[1] == 0 && data[2] == 0xfe && data[3] == 0xff)
                return new UTF32Encoding(true, true);
            else if (data.Length > 3 && data[0] == 0x2b && data[1] == 0x2f && data[2] == 0x76)
                return Encoding.UTF7;
            else
                return Encoding.Default;
        }

        private static byte[] RemoveBom(Encoding encoding, byte[] data)
        {
            byte[] preamble = encoding.GetPreamble();
            if (data.StartsWith(preamble))
                data = data.Remove(preamble.Length);
            return data;
        }

        private static bool StartsWith(this byte[] data, byte[] value)
        {
            if (data == value)
            {
                return true;
            }
            else if (data != null && value != null && data.Length >= value.Length)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (data[i] != value[i])
                        return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static byte[] Remove(this byte[] data, int length)
        {
            if (data != null && data.Length >= length)
            {
                byte[] result = new byte[data.Length - length];

                for (int i = length; i < data.Length; i++)
                {
                    result[i - length] = data[i];
                }

                return result;
            }

            return new byte[0];
        }

        private static char UsedPathSeparator(string path)
        {
            int index = path.IndexOfAny(_pathSeparators);
            if (index != -1)
                return path[index];
            else
                return Path.DirectorySeparatorChar;
        }

        // If the file is part of the path, we return the path only until the last directory.
        private static string GetPathUntilLastDirectory(this IFileSystem fileSystem, string path, out bool hasPathChanged, out string fileName)
        {
            if (Path.HasExtension(path) && !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                hasPathChanged = true;
                fileName = fileSystem.GetFileName(path);
                return fileSystem.GetFileLocation(path);
            }
            hasPathChanged = false;
            fileName = string.Empty;
            return path;
        }

        private static readonly char[] _pathSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private static readonly char[] _pathVolumeSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar};

        private static bool IsPathSeparator(char c) =>
            c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;

        private static string LinesToString(string newLineSeparator, IEnumerable<string> lines) =>
            string.Join(newLineSeparator, lines);

        private static readonly string[] _newLineSeparators = { "\r\n", "\n" };

        private static string[] StringToLines(string text) =>
            text.Split(_newLineSeparators, StringSplitOptions.None);

        #endregion
    }
}