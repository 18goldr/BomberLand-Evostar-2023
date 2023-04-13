#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Collections.Extensions;

namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public class MultiTypeCollection
    {
        private readonly MultiValueDictionary<Type, object> _dictionary = new MultiValueDictionary<Type, object>();

        public void Add<T>(T t)
        {
            Debug.Assert(null != t, "Null not allowed in collection");

            this._dictionary.Add(typeof(T), t);
        }

        public void Add(Type t, object o)
        {

            Debug.Assert(t.IsInstanceOfType(o),
                $"Trying to add object with incorrect type!  Adding {o.GetType().Name} but expected {t.Name}");

            this._dictionary.Add(t, o);
        }

        public void RemoveAllOfType(Type t)
        {
            this._dictionary.Remove(t);
        }

        public void AddDynamic(object o)
        {
            this.Add(o.GetType(), o);
        }

        public IEnumerable<T> Get<T>()
        {
            if (this._dictionary.TryGetValue(typeof(T), out var collection)) return collection.Cast<T>();

            return Enumerable.Empty<T>();
        }
    }
}