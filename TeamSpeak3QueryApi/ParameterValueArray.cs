using System.Linq;

namespace TeamSpeak3QueryApi
{
    public class ParameterValueArray : IParameterValue
    {
        private readonly ParameterValue[] _sourceArray;

        public ParameterValueArray()
            : this(null)
        { }
        public ParameterValueArray(ParameterValue[] sourceArray)
        {
            _sourceArray = sourceArray;
        }

        public string CreateParameterLine()
        {
            if (_sourceArray == null)
                return string.Empty;
            var strs = _sourceArray.Select(kv => kv.CreateParameterLine());
            return string.Join("|", strs);
        }

        public static implicit operator ParameterValueArray(ParameterValue[] fromParameters)
        {
            return new ParameterValueArray(fromParameters);
        }

        public override string ToString()
        {
            if (_sourceArray == null)
                return "Params: null";
            return string.Concat("Param: [", string.Join(", ", _sourceArray.Select(s => s.ToString())), "]");
        }
    }
}
