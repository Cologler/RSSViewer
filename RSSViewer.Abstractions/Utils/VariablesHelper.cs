using RSSViewer.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RSSViewer.Utils
{
    public static class VariablesHelper
    {
        public class MissingRequiredVariableException : Exception
        {
            public MissingRequiredVariableException(string variableName)
            {
                this.VariableName = variableName;
            }

            public string VariableName { get; }
        }

        public class UnableConvertVariableException : Exception
        {
            public UnableConvertVariableException(string fromValue, Type toType)
            {
                this.FromValue = fromValue;
                this.ToType = toType;
            }

            public string FromValue { get; }

            public Type ToType { get; }
        }

        public static VariableInfo[] GetVariableInfos(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            var properties = type.GetProperties();
            var variableInfos = new List<VariableInfo>();
            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute<UserVariableAttribute>();
                if (attr != null)
                {
                    var variableInfo = new VariableInfo(property, attr.Name ?? property.Name);
                    variableInfos.Add(variableInfo);
                }
            }
            return variableInfos.ToArray();
        }

        public static void Inject(object obj, IReadOnlyCollection<VariableInfo> variableInfos, Dictionary<string, string> variables)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));
            if (variables is null)
                throw new ArgumentNullException(nameof(variables));
            if (variableInfos is null)
                throw new ArgumentNullException(nameof(variableInfos));

            foreach (var variableInfo in variableInfos)
            {
                if (variables.ContainsKey(variableInfo.VariableName))
                {
                    var stringValue = variables[variableInfo.VariableName];
                    if (TryConvert(stringValue, variableInfo.VariableType, out var value))
                    {
                        variableInfo.PropertyInfo.SetValue(obj, value);
                    }
                    else
                    {
                        throw new UnableConvertVariableException(stringValue, variableInfo.VariableType);
                    }
                }
                else if (variableInfo.IsRequired)
                {
                    throw new MissingRequiredVariableException(variableInfo.VariableName);
                }
            }
        }

        private static bool TryConvert(string value, Type type, out object result)
        {
            if (type == typeof(string))
            {
                result = value;
                return true;
            }

            if (type == typeof(int) && int.TryParse(value, out var r))
            {
                result = r;
                return true;
            }

            if (type == typeof(bool) && bool.TryParse(value, out var b))
            {
                result = b;
                return true;
            }

            result = null;
            return false;
        }

        public static bool IsVaild(VariableInfo variableInfo, string value) => TryConvert(value, variableInfo.VariableType, out _);
    }
}
