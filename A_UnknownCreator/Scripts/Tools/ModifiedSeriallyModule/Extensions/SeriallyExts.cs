#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using static UnityEditor.TypeCache;

namespace UnknownCreator.Modules
{
    public static class SeriallyExts
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic
        | BindingFlags.Public
        | BindingFlags.Instance;

        private const BindingFlags PropertyFlags = BindingFlags.NonPublic
        | BindingFlags.Public
        | BindingFlags.Instance;


        private const string IndexKey = "index";

        private static readonly Regex ArrayRegex = new Regex(
            $"\\[(?<{IndexKey}>\\d+)\\]",
            RegexOptions.Compiled);

        public static bool TryGetArrayIndex(this string propertyStr, out int index)
        {
            index = -1;
            var arrayMatch = ArrayRegex.Match(propertyStr);
            if (!arrayMatch.Success) return false;

            index = int.Parse(arrayMatch.Groups[IndexKey].Value);
            return true;
        }

        public static FieldInfo GetFieldInfo(this object target, string propertyStr)
        {
            return target.GetType().GetField(propertyStr, FieldFlags);
        }

        public static PropertyInfo GetPropertyInfo(this object target, string propertyStr)
        {
            return target.GetType().GetProperty(propertyStr, PropertyFlags);
        }

        public static object GetValue(this object target, string fieldName)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("field cannot be null or whitespace", nameof(fieldName));

            var field = target.GetFieldInfo(fieldName);
            if (field != null)
                return field.GetValue(target);

            var property = target.GetPropertyInfo(fieldName);
            if (property != null)
                return property.GetValue(target);

            return null;
        }

        public static Type GetManagedReferenceFieldType(this SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference) return null;

            return GetTypeFromManagedReferenceFullTypename(property.managedReferenceFieldTypename, out var type)
                ? type
                : null;
        }

        public static Type GetManagedReferenceValueType(this SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference) return null;

            return GetTypeFromManagedReferenceFullTypename(property.managedReferenceFullTypename, out var type)
                ? type
                : null;
        }

        public static Type[] GetSelectableManagedReferenceValueTypes(this SerializedProperty property)
        {
            var baseType = property.GetManagedReferenceFieldType() ?? throw new ArgumentException(nameof(property));
            return GetTypesDerivedFrom(baseType)
                .Prepend(baseType)
                .Where(IsSerializeReferenceable)
                .ToArray();
        }

        public static void SetManagedReferenceValueToNew(this SerializedProperty property, Type type)
        {
            property.managedReferenceValue = type != null
                ? Activator.CreateInstance(type)
                : null;
            var so = property.serializedObject;
            so.ApplyModifiedProperties();
        }

        public static object GetValue(this SerializedProperty property,
        Func<IEnumerable<string>, IEnumerable<string>> pathModifier = null)
        {
            IEnumerable<string> path = property.propertyPath.Replace("Array.data[", "[").Split('.');
            if (pathModifier != null) path = pathModifier(path);

            var target = (object)property.serializedObject.targetObject;
            return target.GetValueRecur(path);
        }

        public static T GetCustomAttribute<T>(this Type type)
        where T : Attribute
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return type.GetCustomAttributes(typeof(T), true).Select(attr => (T)attr).FirstOrDefault();
        }

        public static object ElementAtOrDefault(this IEnumerable sequence, int index)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            foreach (var element in sequence)
            {
                if (index == 0) return element;
                index--;
            }
            return null;
        }

        public static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return count <= 0
                ? source.Skip(0)
                : SkipLastIterator(source, count);
        }

        private static IEnumerable<TSource> SkipLastIterator<TSource>(IEnumerable<TSource> source, int count)
        {

            var queue = new Queue<TSource>();

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (queue.Count == count)
                    {
                        do
                        {
                            yield return queue.Dequeue();
                            queue.Enqueue(e.Current);
                        }
                        while (e.MoveNext());
                        break;
                    }
                    else
                    {
                        queue.Enqueue(e.Current);
                    }
                }
            }
        }

        private static bool IsSerializeReferenceable(this Type type)
        {
            return !type.IsAbstract
                && !type.IsInterface
                && !typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        private static object GetValueRecur(this object target, IEnumerable<string> propertyPath)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var propertyStr = propertyPath.FirstOrDefault();
            if (propertyStr == null) return target;

            target = propertyStr.TryGetArrayIndex(out int index)
                ? (target as IEnumerable).ElementAtOrDefault(index)
                : target.GetValue(propertyStr);

            return target.GetValueRecur(propertyPath.Skip(1));
        }

        private static bool GetTypeFromManagedReferenceFullTypename(string typeName, out Type type)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                type = null;
                return false;
            }

            var splitFieldTypename = typeName.Split(' ');
            var assemblyName = splitFieldTypename[0];
            var subStringTypeName = splitFieldTypename[1];
            if (splitFieldTypename.Length > 2)
            {
                subStringTypeName = typeName.Substring(assemblyName.Length + 1);
            }

            var assembly = Assembly.Load(assemblyName);
            var targetType = assembly.GetType(subStringTypeName);
            type = targetType;
            return type != null;
        }
    }
}
#endif