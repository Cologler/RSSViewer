using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Annotations
{
    public class UserVariableAttribute : Attribute
    {
        public UserVariableAttribute(string name = null)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
