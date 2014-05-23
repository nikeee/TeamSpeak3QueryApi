using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;

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
                                                                    {typeof(ClientType), new EnumCaster<ClientType>()},
                                                                    {typeof(TimeSpan), new TimeSpanTypeCaster()},
                                                                    {typeof(long), new Int64TypeCaster()}
                                                                };

        public static IReadOnlyList<T> SerializeGeneric<T>(NotificationData data)
                where T : Notify
        {
            if (data.Payload.Count == 0)
                return new ReadOnlyCollection<T>(new T[0]);

            var pl = data.Payload;
            var fields = typeof(T).GetFields();

            var destList = new List<T>(pl.Count);

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

                        var castedValue = CastForType(matchedEntry.FieldType, v.Value);
                        try
                        {
                            matchedEntry.SetValue(destType, castedValue);
                        }
                        catch (Exception)
                        {
                            Debugger.Break();
                            throw;
                        }
                    }
                }
                destList.Add(destType);
            }
            return new ReadOnlyCollection<T>(destList);
        }

        private static dynamic CastForType(Type type, object value)
        {
            if (type.IsArray)
            {
                if (value == null)
                    return null;

                var arrayOf = type.GetElementType();
                var str = value.ToString();
                var arr = str.Split('|');
                dynamic typedArray = Array.CreateInstance(arrayOf, arr.Length);

                for (int i = 0; i < arr.Length; ++i)
                    typedArray[i] = CastForType(arrayOf, arr[i]);

                return typedArray;
            }

            if (Casters.ContainsKey(type))
            {
                try
                {
                    var caster = Casters[type];
                    return caster.Cast(value);
                }
                catch (Exception)
                {
                    Debugger.Break();
                    return DefaultCaster.Cast(value);
                }
            }
            return DefaultCaster.Cast(value);
        }
    }
}
