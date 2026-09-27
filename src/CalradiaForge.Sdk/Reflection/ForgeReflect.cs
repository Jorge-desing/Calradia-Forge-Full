using System;
using System.Reflection;

namespace CalradiaForge.Sdk.Reflection
{
    /// <summary>
    /// Provides simple, rapid utilities for reflecting into native or other mod's private spaces.
    /// This removes the headache of managing BindingFlags manually.
    /// </summary>
    public static class ForgeReflect
    {
        private const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        
        // High-performance caches to avoid Reflection overhead in hot paths (like Tick)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, FieldInfo> _fieldCache = new System.Collections.Concurrent.ConcurrentDictionary<string, FieldInfo>();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, MethodInfo> _methodCache = new System.Collections.Concurrent.ConcurrentDictionary<string, MethodInfo>();

        private static FieldInfo GetCachedField(Type type, string fieldName)
        {
            string key = $"{type.FullName}:{fieldName}";
            if (!_fieldCache.TryGetValue(key, out var field))
            {
                field = type.GetField(fieldName, AllFlags);
                if (field == null) throw new MissingFieldException(type.Name, fieldName);
                _fieldCache[key] = field;
            }
            return field;
        }

        private static MethodInfo GetCachedMethod(Type type, string methodName)
        {
            string key = $"{type.FullName}:{methodName}";
            if (!_methodCache.TryGetValue(key, out var method))
            {
                method = type.GetMethod(methodName, AllFlags);
                if (method == null) throw new MissingMethodException(type.Name, methodName);
                _methodCache[key] = method;
            }
            return method;
        }

        /// <summary>
        /// Retrieves the value of a private or protected field from an instance.
        /// </summary>
        public static object GetPrivateField(object instance, string fieldName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            return GetCachedField(instance.GetType(), fieldName).GetValue(instance);
        }

        /// <summary>
        /// Retrieves the value of a private or protected field from a static class.
        /// </summary>
        public static object GetPrivateStaticField(Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return GetCachedField(type, fieldName).GetValue(null);
        }

        /// <summary>
        /// Sets the value of a private or protected field on an instance.
        /// </summary>
        public static void SetPrivateField(object instance, string fieldName, object value)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            GetCachedField(instance.GetType(), fieldName).SetValue(instance, value);
        }

        /// <summary>
        /// Sets the value of a private or protected field on a static class.
        /// </summary>
        public static void SetPrivateStaticField(Type type, string fieldName, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            GetCachedField(type, fieldName).SetValue(null, value);
        }

        /// <summary>
        /// Invokes a private or protected method on an instance.
        /// </summary>
        public static object CallPrivateMethod(object instance, string methodName, params object[] parameters)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            return GetCachedMethod(instance.GetType(), methodName).Invoke(instance, parameters);
        }
    }
}
