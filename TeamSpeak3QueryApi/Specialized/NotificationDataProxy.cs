using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    internal class NotificationDataProxy
    {
        private static readonly ITypeCaster DefaultCaster = new StringTypeCaster();
        private static readonly Dictionary<Type, ITypeCaster> Casters = new Dictionary<Type, ITypeCaster>
                                                                {
                                                                    {typeof(int), new Int32TypeCaster()},
                                                                    {typeof(string), DefaultCaster},
                                                                    {typeof(bool), new BooleanTypeCaster()}
                                                                };

        public static IReadOnlyList<T> SerializeGeneric<T>(NotificationData data)
                where T : Notify
        {
            if (data.Payload.Count == 0)
                return new ReadOnlyCollection<T>(new T[0]);

            var pl = data.Payload;
            var fields = typeof(T).GetFields(BindingFlags.Public);

            var destList = new List<T>(pl.Count);

            Debugger.Break();

            foreach (var item in pl)
            {
                var destType = Activator.CreateInstance<T>();
                foreach (var v in item)
                {
                    var matchedEntry = fields.SingleOrDefault(fi => fi.CustomAttributes.OfType<QuerySerializeAttribute>().Any(qsa => qsa.Name == v.Key) || fi.Name == v.Key);
                    if (matchedEntry != null)
                    {
                        if (Casters.ContainsKey(matchedEntry.FieldType))
                        {
                            var caster = Casters[matchedEntry.FieldType]; //_casters.Single(c => c.Key == matchedEntry.FieldType);
                            matchedEntry.SetValue(destType, caster.Cast(v.Value));
                        }
                        else
                        {
                            matchedEntry.SetValue(destType, DefaultCaster.Cast(v.Value));
                        }
                    }
                }
                destList.Add(destType);
            }

            return new ReadOnlyCollection<T>(destList);
        }
    }

    interface ITypeCaster
    {
        object Cast(object source);
    }

    class Int32TypeCaster : ITypeCaster
    {
        public object Cast(object source)
        {
            return int.Parse((string)source);
        }
    }

    class StringTypeCaster : ITypeCaster
    {
        public object Cast(object source)
        {
            return (string)source;
        }
    }

    class BooleanTypeCaster : ITypeCaster
    {
        public object Cast(object source)
        {
            return int.Parse((string)source) != 0;
        }
    }
}
