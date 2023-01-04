using System.Linq;

namespace TeamSpeak3QueryApi.Net;

/// <summary>Represents the value of a parameter which consits of a multiple values.</summary>
public class ParameterValueArray : IParameterValue
{
    private readonly ParameterValue[] _sourceArray;

    /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.ParameterValue"/> using <see langword="null" /> as a value.</summary>
    public ParameterValueArray()
        : this(null)
    { }

    /// <summary>Creates a new instance of <see cref="T:TeamSpeak3QueryApi.Net.ParameterValue"/> using the specified <see cref="T:System.String[]" /> as a value.</summary>
    /// <param name="sourceArray">The values.</param>
    public ParameterValueArray(ParameterValue[] sourceArray) => _sourceArray = sourceArray;

    /// <summary>Creates an escaped string representation of the parameter value.</summary>
    /// <returns>An escaped string representation of the parameter value.</returns>
    public string CreateParameterLine(string parameterName)
    {
        if (_sourceArray == null)
            return string.Empty;

        var strs = _sourceArray.Select(kv => kv.CreateParameterLine(parameterName)).ToArray();
        return string.Join("|", strs);

        /*
        // Pretty much design failure
        var sb = new StringBuilder();
        sb.Append(strs[0]);
        for (int i = 1; i < strs.Length; ++i)
            sb.Append('|').Append(parameterName).Append('=').Append(strs[1]);
        return sb.ToString();
        */
    }

    /// <summary>Creates a new parameter value using a <see cref="T:System.String[]"/> as value.</summary>
    /// <param name="fromParameters">The values</param>
    public static implicit operator ParameterValueArray(ParameterValue[] fromParameters) => new ParameterValueArray(fromParameters);

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        if (_sourceArray == null)
            return "Params: null";
        return string.Concat("Param: [", string.Join(", ", _sourceArray.Select(s => s.ToString())), "]");
    }
}
