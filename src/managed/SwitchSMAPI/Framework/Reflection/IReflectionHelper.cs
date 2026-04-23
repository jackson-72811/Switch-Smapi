using System;
using System.Reflection;

namespace SwitchSMAPI.Framework.Reflection {

    /// <summary>Wraps a private field for safe reading and writing.</summary>
    public interface IPrivateField<TValue> {
        FieldInfo FieldInfo { get; }
        TValue    GetValue();
        void      SetValue(TValue value);
    }

    /// <summary>Wraps a private property for safe reading and writing.</summary>
    public interface IPrivateProperty<TValue> {
        PropertyInfo PropertyInfo { get; }
        TValue       GetValue();
        void         SetValue(TValue value);
    }

    /// <summary>Wraps a private method for safe invocation.</summary>
    public interface IPrivateMethod {
        MethodInfo MethodInfo { get; }
        void       Invoke(params object?[] arguments);
        TReturn    Invoke<TReturn>(params object?[] arguments);
    }

    /// <summary>Provides access to private game fields, properties, and methods.</summary>
    public interface IReflectionHelper {

        /// <summary>Get a private instance field.</summary>
        IPrivateField<TValue> GetField<TValue>(object obj, string name, bool required = true);

        /// <summary>Get a private static field.</summary>
        IPrivateField<TValue> GetField<TValue>(Type type, string name, bool required = true);

        /// <summary>Get a private instance property.</summary>
        IPrivateProperty<TValue> GetProperty<TValue>(object obj, string name, bool required = true);

        /// <summary>Get a private static property.</summary>
        IPrivateProperty<TValue> GetProperty<TValue>(Type type, string name, bool required = true);

        /// <summary>Get a private instance method.</summary>
        IPrivateMethod GetMethod(object obj, string name, bool required = true);

        /// <summary>Get a private static method.</summary>
        IPrivateMethod GetMethod(Type type, string name, bool required = true);
    }
}
