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
                                                                    {typeof(bool), new BooleanTypeCaster()},
                                                                    {typeof(ReasonId), new EnumCaster<ReasonId>()},
                                                                    {typeof(ClientType), new EnumCaster<ClientType>()}
                                                                };

        public static IReadOnlyList<T> SerializeGeneric<T>(NotificationData data)
                where T : Notify
        {
            if (data.Payload.Count == 0)
                return new ReadOnlyCollection<T>(new T[0]);

            var pl = data.Payload;
            var fields = typeof(T).GetFields();

            var destList = new List<T>(pl.Count);

            Debugger.Break();

            foreach (var item in pl)
            {
                var destType = Activator.CreateInstance<T>();
                foreach (var v in item)
                {
                    var matchedEntry = fields.SingleOrDefault(
                        fi =>
                        {
                            var ca = fi.GetCustomAttributes<QuerySerializeAttribute>(false);
                            return fi.Name == v.Key || ca.Any(qsa => qsa.Name == v.Key);
                        });
                    if (matchedEntry != null)
                    {


                        if (Casters.ContainsKey(matchedEntry.FieldType))
                        {
                            try
                            {
                                var caster = Casters[matchedEntry.FieldType]; //_casters.Single(c => c.Key == matchedEntry.FieldType);
                                matchedEntry.SetValue(destType, caster.Cast(v.Value));
                            }
                            catch (Exception)
                            {
                                Debugger.Break();
                                matchedEntry.SetValue(destType, DefaultCaster.Cast(v.Value));
                            }
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
}
