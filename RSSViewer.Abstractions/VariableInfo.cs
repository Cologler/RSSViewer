using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace RSSViewer
{
    public class VariableInfo
    {
        public VariableInfo(PropertyInfo propertyInfo, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("message", nameof(name));

            this.IsRequired = propertyInfo.IsDefined(typeof(RequiredAttribute));
            this.PropertyInfo = propertyInfo;
            this.VariableType = propertyInfo.PropertyType;
            this.VariableName = name;
        }

        public PropertyInfo PropertyInfo { get; }

        public bool IsRequired { get; }

        public Type VariableType { get; }

        public string VariableName { get; }
    }
}
