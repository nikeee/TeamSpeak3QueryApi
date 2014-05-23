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
                        if (Casters.ContainsKey(matchedEntry.FieldType))
                        {
                            try
                            {
                                var caster = Casters[matchedEntry.FieldType];
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
}
