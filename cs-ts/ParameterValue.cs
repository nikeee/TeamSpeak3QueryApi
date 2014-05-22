namespace CsTs
{
    public class ParameterValue : IParameterValue
    {
        //public string Key { get; set; }
        public string Value { get; set; }

        public ParameterValue()
            : this(null)
        { }

        public ParameterValue(string value)
        {
            Value = value;
        }

        public string CreateParameterLine()
        {
            //var k = Key ?? "";
            var v = Value ?? "";
            //return k.TeamSpeakEscape() + "=" + v.TeamSpeakEscape();
            return v.TeamSpeakEscape();
        }
        //public static implicit operator Parameter(string fromParameter)
        //{
        //    return new Parameter(fromParameter);
        //}
        public static implicit operator ParameterValue(string fromParameter)
        {
            return new ParameterValue(fromParameter);
        }
        public static implicit operator ParameterValue(int fromParameter)
        {
            return new ParameterValue(fromParameter.ToString());
        }

        public override string ToString()
        {
            return string.Concat("Param: ", Value ?? "null");
        }
    }
}
