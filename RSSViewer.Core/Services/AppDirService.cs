using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RSSViewer.Services
{
    public class AppDirService
    {
        private const string DataRootDir = "Data";

        private readonly DirectoryInfo _dataRootDir = new DirectoryInfo(DataRootDir);

        public void EnsureCreated()
        {
            if (!this._dataRootDir.Exists)
            {
                this._dataRootDir.Create();
            }
        }

        public string GetDataFileFullPath(string name) => Path.Combine(this._dataRootDir.FullName, name);
    }
}
