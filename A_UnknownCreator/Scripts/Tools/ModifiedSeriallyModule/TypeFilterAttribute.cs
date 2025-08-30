#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    public class TypeFilterAttribute : PropertyAttribute
    {
        public Type DerivedFrom { get; }

        public Func<IEnumerable<Type>, IEnumerable<Type>> DerivedFromFilter => sequence => sequence;

        public Func<IEnumerable<Type>, IEnumerable<Type>> Filter { get; protected set; }

        public string FilterName { get; }

        public TypeFilterAttribute(Type derivedFrom)
        {
            DerivedFrom = derivedFrom;
            Filter = DerivedFromFilter;
        }

        public TypeFilterAttribute(string filterName)
        {
            FilterName = filterName;
        }

        public Func<IEnumerable<Type>, IEnumerable<Type>> GetFilter(object parent)
        {
            return Filter ?? BindFilterDelegate(parent);
        }

        public Func<IEnumerable<Type>, IEnumerable<Type>> BindFilterDelegate(object parent)
        {
            Filter = (Func<IEnumerable<Type>, IEnumerable<Type>>)Delegate.CreateDelegate(
                typeof(Func<IEnumerable<Type>, IEnumerable<Type>>),
                parent,
                FilterName
                );
            return Filter;
        }
    }
}
#endif