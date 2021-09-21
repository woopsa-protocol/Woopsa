using Concept;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoopsaServer.OStudio
{
    //[Flags]
    //public enum FileSystemManagerCapabilities { CreateDirectory = 1, /*,...*/load = 128 }



    public class FileSystemManager
    {
        #region Constructor

        public FileSystemManager(string referencePath)
        {
            // TODO
            _fileSystem = new FileSystemLocal(referencePath); ;
        }

        #endregion

        #region Fields / Attributes

        private readonly IFileSystem _fileSystem;

        #endregion


        // TODO à ranger
        //FileSystemManagerCapabilities Capabilities { get; }

        //void test()
        //{
        //    FileSystemManagerCapabilities val = FileSystemManagerCapabilities.CreateDirectory | FileSystemManagerCapabilities.load;
        //    val.HasFlag(FileSystemManagerCapabilities.CreateDirectory);
        //}

        #region Directories

        public string CreateDirectory(string path)
        {
            try
            {
                _fileSystem.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return null;
        }

        public string DeleteDirectory(string path)
        {
            try
            {
                _fileSystem.DeleteDirectory(path, true);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return null;
        }

        public string GetDirectoryInfo(string path)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(_fileSystem.GetDirectoryInfo(path));
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string GetDirectories(string path)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(_fileSystem.GetDirectories(path));
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string MoveDirectory(string path, string newPath)
        {
            try
            {
                _fileSystem.MoveDirectory(path, newPath);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return null;
        }

        #endregion

        #region Files

        public string MoveFile(string path, string newPath)
        {
            try
            {
                _fileSystem.MoveFile(path, newPath);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return null;
        }

        public string DeleteFile(string path)
        {
            try
            {
                _fileSystem.DeleteFile(path);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return null;

        }

        public string GetFileInfo(string path)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(_fileSystem.GetFileInfo(path));
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string GetFiles(string path)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(_fileSystem.GetFiles(path));
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string ReadAllBytes(string path)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(_fileSystem.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string WriteAllBytes(string path, string bytesSerialized)
        {
            try
            {
                byte[] bytes = System.Text.Json.JsonSerializer.Deserialize<byte[]>(bytesSerialized);
                return WriteAllBytes(path, bytes);
            }
             catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public void CopyDirectory(string pathToCopy, string pathToPaste)
        {
            _fileSystem.CopyDirectoryRecursively(pathToCopy, pathToPaste);
        }

        #endregion

        #region private Methods

        public void CopyFile(string pathToCopy, string pathToPaste)
        {
            _fileSystem.CopyFile(pathToCopy, pathToPaste);
        }

        private string WriteAllBytes(string path, byte[] bytes)
        {
            try
            {
                if (bytes.Length > 0)
                {
                    _fileSystem.WriteAllBytes(path, bytes);
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return null;
        }

        #endregion
    }
}
