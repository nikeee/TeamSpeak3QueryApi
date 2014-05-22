using System;
using System.Diagnostics;

namespace CsTs
{
    public class Parameter
    {
        public string Name { get; set; }
        public IParameterValue Value { get; set; }

        public Parameter(string name, ParameterValue value)
            : this(name, value as IParameterValue)
        { }
        public Parameter(string name, ParameterValueArray values)
            : this(name, values as IParameterValue)
        { }

        public Parameter(string name, IParameterValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            Name = name;
            Value = value;
        }

        public static Parameter FromArray(string[] sourceArray)
        {
            return sourceArray;
        }

        public static implicit operator Parameter(string[] sourceArray)
        {
            Debug.Assert(sourceArray != null);

            if (sourceArray == null || sourceArray.Length == 0)
                return null; //throw new ArgumentException("Invalid parameters");

            var name = sourceArray[0];
            if (sourceArray.Length == 2)
                return new Parameter(name, new ParameterValue(sourceArray[1]));

            var values = new ParameterValue[sourceArray.Length - 1];
            for (int i = 1; i < sourceArray.Length; ++i)
                values[i - 1] = new ParameterValue(sourceArray[i]);
            return new Parameter(name, new ParameterValueArray(values));
        }
    }
}
