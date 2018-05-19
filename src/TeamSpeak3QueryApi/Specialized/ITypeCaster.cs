using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    interface ITypeCaster
    {
        dynamic Cast(object source);
    }

    class Int16TypeCaster : ITypeCaster
    {
        public virtual dynamic Cast(object source)
        {
            if (source == null)
                return (short)0;
            if (source is short)
                return (short)source;
            return short.Parse(source.ToString());
        }
    }

    class Int32TypeCaster : ITypeCaster
    {
        public virtual dynamic Cast(object source)
        {
            if (source == null)
                return 0;
            if (source is int)
                return (int)source;
            return int.Parse(source.ToString());
        }
    }

    class Int64TypeCaster : ITypeCaster
    {
        public virtual dynamic Cast(object source)
        {
            if (source == null)
                return 0;
            if (source is long)
                return (long)source;
            return long.Parse(source.ToString());
        }
    }

    class EnumTypeCaster<T> : Int32TypeCaster where T : struct
    {
        public override dynamic Cast(object source)
        {
            var i = base.Cast(source);
            return (T)i;
        }
    }

    class StringTypeCaster : ITypeCaster
    {
        public dynamic Cast(object source)
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
        public dynamic Cast(object source)
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
        public dynamic Cast(object source)
        {
            if (source == null)
                return false;
            if (source is int)
                return TimeSpan.FromSeconds((int)source);
            return TimeSpan.FromSeconds(int.Parse(source.ToString()));
        }
    }

    class DateTimeTypeCaster : ITypeCaster
    {
        public dynamic Cast(object source)
        {
            if (source == null)
                return false;

            long unixTimestamp;

            if (source is long)
                unixTimestamp = (long)source;
            else
                unixTimestamp = long.Parse(source.ToString());

            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
        }
    }

    class ReadonlyListIntCaster : ITypeCaster
    {
        public dynamic Cast(object source)
        {
            if (source == null)
                return false;
            if (source is int)
                return new List<int> { (int)source };

            var sourceStr = source as string;

            if (string.IsNullOrWhiteSpace(sourceStr))
                return null;
            
            return sourceStr.Split(',').Select(int.Parse).ToList();
        }
    }
}
