using System.Globalization;

namespace TeamSpeak3QueryApi.Net;

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
    public ParameterValue(string value) => Value = value;

    /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.ParameterValue"/> using the specified <see cref="T:System.Int32" /> as a value.</summary>
    /// <param name="value">The value.</param>
    public ParameterValue(int value) => Value = value.ToString(CultureInfo.CurrentCulture);

    /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.ParameterValue"/> using the specified <see cref="T:System.Boolean" /> as a value.</summary>
    /// <param name="value">The value.</param>
    public ParameterValue(bool value) => Value = value ? "1" : "0";

    /// <summary>Creates an escaped string representation of the parameter.</summary>
    /// <returns>An escaped string representation of the parameter.</returns>
    public string CreateParameterLine(string parameterName)
    {
        return string.Concat(parameterName.TeamSpeakEscape(), '=', (Value ?? string.Empty).TeamSpeakEscape());
    }

    /// <summary>Creates a new parameter value using a <see cref="T:System.String"/> as value.</summary>
    /// <param name="fromParameter">The value</param>
    public static implicit operator ParameterValue(string fromParameter) => new ParameterValue(fromParameter);

    /// <summary>Creates a new parameter value using a <see cref="T:System.Int32"/> as value.</summary>
    /// <param name="fromParameter">The value</param>
    public static implicit operator ParameterValue(int fromParameter) => new ParameterValue(fromParameter.ToString(CultureInfo.CurrentCulture));

    /// <summary>Creates a new parameter value using a <see cref="T:System.Int64"/> as value.</summary>
    /// <param name="fromParameter">The value</param>
    public static implicit operator ParameterValue(long fromParameter) => new ParameterValue(fromParameter.ToString(CultureInfo.CurrentCulture));

    /// <summary>Creates a new parameter value using a <see cref="T:System.Int32"/> as value.</summary>
    /// <param name="fromParameter">The value</param>
    public static implicit operator ParameterValue(bool fromParameter) => new ParameterValue(fromParameter ? "1" : "0");

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => string.Concat("Param: ", Value ?? "null");
}
