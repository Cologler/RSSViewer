﻿using System.Collections.Generic;

namespace RSSViewer.Configuration
{
    public class SyncSourceConf
    {
        public string ProviderName { get; set; }

        /// <summary>
        /// use for display.
        /// </summary>
        public string Name { get; set; }

        public Dictionary<string, string> Variables { get; set; }
    }
}
