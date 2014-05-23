using System;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    interface ITypeCaster
    {
        object Cast(object source);
    }
    class Int32TypeCaster : ITypeCaster
    {
        public virtual object Cast(object source)
        {
            if (source == null)
                return 0;
            if (source is int)
                return (int)source;
            return int.Parse(source.ToString());
        }
    }

    class EnumCaster<T> : Int32TypeCaster where T : struct
    {
        public override object Cast(object source)
        {
            var i = base.Cast(source);
            return (T)i;
        }
    }

    class StringTypeCaster : ITypeCaster
    {
        public object Cast(object source)
        {
            if (source == null)
                return null;
            if (source is int)
                return ((int)source).ToString().TeamSpeakUnescape();
            return source.ToString().TeamSpeakUnescape();
        }
    }

    class BooleanTypeCaster : ITypeCaster
    {
        public object Cast(object source)
        {
            if (source == null)
                return false;
            if (source is int)
                return ((int)source) != 0;
            return int.Parse(source.ToString()) != 0;
        }
    }
    class TimeSpanTypeCaster : ITypeCaster
    {
        public object Cast(object source)
        {
            if (source == null)
                return false;
            if (source is int)
                return TimeSpan.FromSeconds((int)source);
            return TimeSpan.FromSeconds(int.Parse(source.ToString()));
        }
    }
}
