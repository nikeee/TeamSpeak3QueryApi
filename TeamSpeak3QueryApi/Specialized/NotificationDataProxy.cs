using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    class NotificationDataProxy
    {
        public static IReadOnlyList<T> SerializeGeneric<T>(NotificationData data)
            where T : Notify
        {
            if (data.Payload.Count == 0)
                return new ReadOnlyCollection<T>(new T[0]);

            var pl = data.Payload;
            var fields = typeof(T).GetFields(BindingFlags.Public);
            var destType = Activator.CreateInstance<T>();

            Debugger.Break();

            foreach (var item in pl)
            {
                foreach (var v in item)
                {
                    var matchedEntry = fields.SingleOrDefault(fi => fi.CustomAttributes.OfType<QuerySerializeAttribute>().Any(qsa => qsa.Name == v.Key) || fi.Name == v.Key);

                }
            }


            throw new NotImplementedException();
        }
    }
}
