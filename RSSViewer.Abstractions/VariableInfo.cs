using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer
{
    public class VariableInfo
    {
        public VariableInfo(Type type, string name, bool isRequired, object defaultValue = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("message", nameof(name));
            this.IsRequired = isRequired;
            this.VariableType = type ?? throw new ArgumentNullException(nameof(type));
            this.VariableName = name;
            this.DefaultValue = defaultValue;
        }

        public bool IsRequired { get; }

        public Type VariableType { get; }

        public string VariableName { get; }

        public object DefaultValue { get; }

        public object ReadFrom(Dictionary<string, object> variables)
        {
            return this.IsRequired 
                ? variables[this.VariableName] 
                : variables.GetValueOrDefault(this.VariableName, this.DefaultValue);
        }

        public static VariableInfo String(string name, bool isRequired = false) => 
            new VariableInfo(typeof(string), name, isRequired);
    }
}
