using System;
using System.Diagnostics;

namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents a Query API parameter.</summary>
    public class Parameter
    {
        /// <summary>The name of the Query API parameter.</summary>
        /// <returns>The name of the parameter.</returns>
        public string Name { get; set; }
        /// <summary>The value of the Query API parameter.</summary>
        /// <returns>The value of the parameter.</returns>
        public IParameterValue Value { get; set; }

        /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.Parameter"/>.</summary>
        /// <param name="name">The name of the Query API parameter.</param>
        /// <param name="value">The value of the Query API parameter.</param>
        public Parameter(string name, ParameterValue value)
            : this(name, value as IParameterValue)
        { }

        /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.Parameter"/>.</summary>
        /// <param name="name">The name of the Query API parameter.</param>
        /// <param name="values">The array value of the Query API parameter.</param>
        public Parameter(string name, ParameterValueArray values)
            : this(name, values as IParameterValue)
        { }

        /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.Parameter"/>.</summary>
        /// <param name="name">The name of the Query API parameter.</param>
        /// <param name="value">The value of the Query API parameter.</param>
        public Parameter(string name, IParameterValue value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            Name = name;
            Value = value;
        }

        public string GetEscapedRepresentation()
        {
            return Value.CreateParameterLine(Name);
        }

        /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.Parameter"/> using a string array.</summary>
        /// <param name="sourceArray">The first item represents the name of the parameter, the rest is used as a value.</param>
        /// <returns>A new instance of <see cref="T:TeamSpeak3QueryApi.Net.Parameter"/>.</returns>
        public static Parameter FromArray(string[] sourceArray)
        {
            return sourceArray;
        }

        /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.Parameter"/> using a string array.</summary>
        /// <param name="sourceArray">The first item represents the name of the parameter, the rest is used as a value.</param>
        /// <returns>A new instance of <see cref="T:TeamSpeak3QueryApi.Net.Parameter"/>.</returns>
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
