using System;
using System.Reflection.Emit;

namespace UnknownCreator.Modules
{
    public static class ObjectUtils
    {
        public static T CreateInstance<T>(Type type) where T : class
        => (T)Activator.CreateInstance(type);

        public static T CreateInstance<T>(object obj) where T : class
        => CreateInstance<T>(obj);

        public static T CreateInstance<T>(string name) where T : class
        => CreateInstance<T>(Type.GetType(name));

        public static object GenerateObject(Type type)
        {
            var constructorInfo = type.GetConstructor(Type.EmptyTypes) ?? throw new ArgumentException($"The type {type.FullName} does not have a parameterless constructor.");
            var dynamicMethod = new DynamicMethod(string.Empty,
                                                  type,
                                                  Type.EmptyTypes,
                                                  type.Module);

            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructorInfo);
            il.Emit(OpCodes.Ret);
            var func = dynamicMethod.CreateDelegate(typeof(Func<object>)) as Func<object>;
            return func();
        }

        public static Func<T> GenerateObject<T>() where T : new()
        {
            return GenerateObject(typeof(T)) as Func<T>;
        }

        public static Func<object> GenerateObjectByFunc(Type type)
        {
            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            if (constructorInfo == null)
            {
                throw new ArgumentException($"The type {type.FullName} does not have a parameterless constructor.");
            }

            var dynamicMethod = new DynamicMethod(string.Empty,
                                                  typeof(object),
                                                  Type.EmptyTypes,
                                                  type.Module);

            var il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructorInfo);
            il.Emit(OpCodes.Ret);
            return (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
        }



    }
}
