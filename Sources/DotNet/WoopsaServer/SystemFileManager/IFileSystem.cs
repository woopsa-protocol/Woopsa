using System.Collections.Generic;
using System.Reflection;

namespace Concept
{
    /// <summary>
    /// Defines an abstraction of the file system.
    /// </summary>
    /// <remarks>
    /// Following conditions must be met by any implementation of IFileSystem :
    /// - The following characters supported as path separators are one of : '/', '\'
    /// - File names are made of a name and an extension separated by '.'
    /// - Directory name '.' denotes "this" directory
    /// - Directory name '..' denotes parent directory
    /// Note : these requirements are compatible with Windows and Unix file systems, as well as Web urls.
    /// </remarks>
    public interface IFileSystem
    {
        #region File

        string NewLine { get; }

        void CopyDirectoryRecursively(string sourcePath, string targetPath);

        void AppendAllBytes(string filePath, byte[] data);

        void WriteAllBytes(string filePath, byte[] data);

        byte[] ReadAllBytes(string filePath);

        bool FileExists(string filePath);

        void DeleteFile(string filePath);

        void CopyFile(string sourcePath, string targetPath);

        void MoveFile(string path, string newPath);

        Dictionary<string, object> GetFileInfo(string fullPath);

        #endregion

        #region Directory

        string[] GetFiles(string path, string searchPattern = null);

        string[] GetDirectories(string path, string searchPattern = null);

        bool DirectoryExists(string path);

        void DeleteDirectory(string path, bool recursive);

        Dictionary<string, object> GetDirectoryInfo(string fullPath);

        void CreateDirectory(string path);

        void MoveDirectory(string path, string newPath);

        #endregion

        #region Assembly

        Assembly LoadAssembly(string path);

        #endregion
    }
}