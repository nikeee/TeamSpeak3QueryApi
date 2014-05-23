using System.Globalization;

namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents the value of a parameter which consits of a single value.</summary>
    public class ParameterValue : IParameterValue
    {
        /// <summary>The value.</summary>
        /// <returns>The value.</returns>
        public string Value { get; set; }

        /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.ParameterValue"/> using <see langword="null" /> as a value.</summary>
        public ParameterValue()
            : this(null)
        { }

        /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.ParameterValue"/> using the specified <see cref="T:System.String" /> as a value.</summary>
        /// <param name="value">The value.</param>
        public ParameterValue(string value)
        {
            Value = value;
        }

        /// <summary>Creates an escaped string representation of the parameter value.</summary>
        /// <returns>An escaped string representation of the parameter value.</returns>
        public string CreateParameterLine()
        {
            return (Value ?? "").TeamSpeakEscape();
        }

        /// <summary>Creates a new parameter value using a <see cref="T:System.String"/> as value.</summary>
        /// <param name="fromParameter">The value</param>
        public static implicit operator ParameterValue(string fromParameter)
        {
            return new ParameterValue(fromParameter);
        }
        /// <summary>Creates a new parameter value using a <see cref="T:System.Int32"/> as value.</summary>
        /// <param name="fromParameter">The value</param>
        public static implicit operator ParameterValue(int fromParameter)
        {
            return new ParameterValue(fromParameter.ToString(CultureInfo.CurrentCulture));
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return string.Concat("Param: ", Value ?? "null");
        }
    }
}
