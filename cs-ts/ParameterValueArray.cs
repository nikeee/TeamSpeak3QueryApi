using System.Linq;

namespace CsTs
{
    public class ParameterValueArray : IParameterValue
    {
        private readonly ParameterValue[] _arr;

        public ParameterValueArray()
            : this(null)
        { }
        public ParameterValueArray(ParameterValue[] arr)
        {
            _arr = arr;
        }

        public string CreateParameterLine()
        {
            if (_arr == null)
                return string.Empty;
            var strs = _arr.Select(kv => kv.CreateParameterLine());
            return string.Join("|", strs);
        }

        public static implicit operator ParameterValueArray(ParameterValue[] fromParameters)
        {
            return new ParameterValueArray(fromParameters);
        }

        public override string ToString()
        {
            if (_arr == null)
                return "Params: null";
            return string.Concat("Param: [", string.Join(", ", _arr.Select(s => s.ToString())), "]");
        }
    }
}
