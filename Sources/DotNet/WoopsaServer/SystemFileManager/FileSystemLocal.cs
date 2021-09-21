using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Concept
{
    public class FileSystemLocal : IFileSystem
    {
        #region Constructor

        public FileSystemLocal(string referencePath)
        {
            _referencePath = referencePath ?? throw new ArgumentNullException(nameof(referencePath));
        }

        #endregion

        #region Properties

        public string NewLine => Environment.NewLine;

        #endregion

        #region File

        public void AppendAllBytes(string filePath, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), $@"The argument '{nameof(filePath)}' is null, empty or consists only of white-space characters.");

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string fullPath = this.GetAbsoluteFilePath(filePath, _referencePath);
            using (var stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.Write(data, 0, data.Length);
            }
        }

        public void CopyFile(string sourcePath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentNullException(nameof(sourcePath), $@"The argument '{nameof(sourcePath)}' is null, empty or consists only of white-space characters.");

            string fullSourcePath = this.GetAbsoluteFilePath(sourcePath, _referencePath);

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentNullException(nameof(targetPath), $@"The argument '{nameof(targetPath)}' is null, empty or consists only of white-space characters.");

            string fullTargetPath = this.GetAbsoluteFilePath(targetPath, _referencePath);

            File.Copy(fullSourcePath, fullTargetPath, true);
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), $@"The argument '{nameof(filePath)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(filePath, _referencePath);
            File.Delete(fullPath);
        }

        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                string fullPath = this.GetAbsoluteFilePath(filePath, _referencePath);
                return File.Exists(fullPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public byte[] ReadAllBytes(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), $@"The argument '{nameof(filePath)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(filePath, _referencePath);
            return File.ReadAllBytes(fullPath);
        }

        public void WriteAllBytes(string filePath, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), $@"The argument '{nameof(filePath)}' is null, empty or consists only of white-space characters.");

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string fullPath = this.GetAbsoluteFilePath(filePath, _referencePath);

            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!DirectoryExists(directoryPath))
            {
                CreateDirectory(directoryPath);
            }

            File.WriteAllBytes(fullPath, data);
        }

        /// <summary>
        /// Get file info to return to Filemanager
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetFileInfo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), $@"The argument '{nameof(filePath)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(filePath, _referencePath);

            if (FileExists(fullPath))
            {
                FileInfo fileInfo = new FileInfo(fullPath);
                return new Dictionary<string, object>
                {
                    { "Path", fullPath },
                    { "Filename", fileInfo.Name },
                    { "File Type", fileInfo.Extension.Replace(".", "") },
                    { "Protected", fileInfo.IsReadOnly ? 1 : 0 },
                    { "Date Created", fileInfo.CreationTime.ToString() },
                    { "Date Modified", fileInfo.LastWriteTime.ToString() },
                    { "Size", fileInfo.Length }
                };
            }
            else
            {
                return null;
            }
        }

        public string[] GetFiles(string path, string searchPattern = null)
        {
            if (path ==null)
                throw new ArgumentNullException(nameof(path), $@"The argument '{nameof(path)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
            return searchPattern == null ? Directory.GetFiles(fullPath) : Directory.GetFiles(fullPath, searchPattern);
        }

        public void MoveFile(string path, string newPath)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), $@"The argument '{nameof(path)}' is null, empty or consists only of white-space characters.");
            if (string.IsNullOrWhiteSpace(newPath))
                throw new ArgumentNullException(nameof(newPath), $@"The argument '{nameof(newPath)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
            string fullNewPath = this.GetAbsoluteFilePath(newPath, _referencePath);
            File.Move(fullPath, fullNewPath);
        }

        #endregion

        #region Directory

        /// <summary>
        /// Get directory info to return to Filemanager
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetDirectoryInfo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), $@"The argument '{nameof(filePath)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(filePath, _referencePath);

            if (DirectoryExists(fullPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(fullPath);

                return new Dictionary<string, object>
                {
                    { "Path", fullPath },
                    { "Filename", dirInfo.Name },
                    { "File Type", "dir" },
                    { "Protected", dirInfo.Attributes.HasFlag(FileAttributes.ReadOnly) ? 1 : 0 },
                    { "Date Created", dirInfo.CreationTime.ToString() },
                    { "Date Modified", dirInfo.LastWriteTime.ToString() },
                };
            }
            else
            {
                return null;
            }
        }

        public string[] GetDirectories(string path, string searchPattern = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path), $@"The argument '{nameof(path)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
            return searchPattern == null ? Directory.GetDirectories(fullPath) : Directory.GetDirectories(fullPath, searchPattern);
        }

        public bool DirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
                return Directory.Exists(fullPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), $@"The argument '{nameof(path)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
            Directory.Delete(fullPath, recursive);
        }

        public void CreateDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), $@"The argument '{nameof(path)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
            Directory.CreateDirectory(fullPath);
        }

        public void MoveDirectory(string path, string newPath)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), $@"The argument '{nameof(path)}' is null, empty or consists only of white-space characters.");
            if (string.IsNullOrWhiteSpace(newPath))
                throw new ArgumentNullException(nameof(newPath), $@"The argument '{nameof(newPath)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
            string fullNewPath = this.GetAbsoluteFilePath(newPath, _referencePath);
            Directory.Move(fullPath, fullNewPath);
        }

        public void CopyDirectoryRecursively(string sourcePath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentNullException(nameof(sourcePath), $@"The argument '{nameof(sourcePath)}' is null, empty or consists only of white-space characters.");

            string fullSourcePath = this.GetAbsoluteFilePath(sourcePath, _referencePath);

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentNullException(nameof(targetPath), $@"The argument '{nameof(targetPath)}' is null, empty or consists only of white-space characters.");

            string fullTargetPath = this.GetAbsoluteFilePath(targetPath, _referencePath);

            Directory.CreateDirectory(fullTargetPath);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(fullSourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(fullSourcePath, fullTargetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(fullSourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(fullSourcePath, fullTargetPath), true);
            }
        }

        #endregion

        #region Assembly

        public Assembly LoadAssembly(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path), $@"The argument '{nameof(path)}' is null, empty or consists only of white-space characters.");

            string fullPath = this.GetAbsoluteFilePath(path, _referencePath);
            return Assembly.LoadFrom(fullPath);
        }

        #endregion

        #region Private Members

        private readonly string _referencePath;

        #endregion
    }
}