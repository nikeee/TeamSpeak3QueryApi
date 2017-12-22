namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents an abstraction of a parameter value.</summary>
    public interface IParameterValue
    {
        /// <summary>Creates an escaped string representation of the parameter value.</summary>
        /// <returns>An escaped string representation of the parameter value.</returns>
        string CreateParameterLine(string parameterName);
    }
}
