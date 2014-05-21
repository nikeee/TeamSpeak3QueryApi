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

        public static implicit operator Parameter(string[] fromArray)
        {
            Debug.Assert(fromArray != null);

            if (fromArray.Length == 0)
                throw new ArgumentException("Invalid parameters");

            var name = fromArray[0];
            if (fromArray.Length == 2)
                return new Parameter(name, new ParameterValue(fromArray[1]));

            var values = new ParameterValue[fromArray.Length - 1];
            for (int i = 1; i < fromArray.Length; ++i)
                values[i - 1] = new ParameterValue(fromArray[i]);
            return new Parameter(name, new ParameterValueArray(values));
        }
    }
}
