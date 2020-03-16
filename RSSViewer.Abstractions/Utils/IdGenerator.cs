using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Utils
{
    public static class IdGenerator
    {
        public static string Generate()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
