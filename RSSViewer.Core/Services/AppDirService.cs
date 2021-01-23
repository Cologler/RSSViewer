using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RSSViewer.Services
{
    public class AppDirService
    {
        private const string DataRootDirectoryName = "Data";

        public DirectoryInfo DataRootDirectory { get; } = new(DataRootDirectoryName);

        public void EnsureCreated()
        {
            if (!this.DataRootDirectory.Exists)
            {
                this.DataRootDirectory.Create();
            }
        }

        public string GetDataFileFullPath(string name) => Path.Combine(this.DataRootDirectory.FullName, name);
    }
}
