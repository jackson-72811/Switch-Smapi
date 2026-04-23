using System;
using System.Reflection;

namespace StardewModdingAPI {
    public interface IPrivateField<TValue> {
        FieldInfo FieldInfo { get; }
        TValue    GetValue();
        void      SetValue(TValue value);
    }
    public interface IPrivateProperty<TValue> {
        PropertyInfo PropertyInfo { get; }
        TValue       GetValue();
        void         SetValue(TValue value);
    }
    public interface IPrivateMethod {
        MethodInfo MethodInfo { get; }
        void       Invoke(params object?[] arguments);
        TReturn    Invoke<TReturn>(params object?[] arguments);
    }
    public interface IReflectionHelper {
        IPrivateField<TValue>    GetField<TValue>(object obj, string name, bool required = true);
        IPrivateField<TValue>    GetField<TValue>(Type type, string name, bool required = true);
        IPrivateProperty<TValue> GetProperty<TValue>(object obj, string name, bool required = true);
        IPrivateProperty<TValue> GetProperty<TValue>(Type type, string name, bool required = true);
        IPrivateMethod           GetMethod(object obj, string name, bool required = true);
        IPrivateMethod           GetMethod(Type type, string name, bool required = true);
    }
}
