using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SwitchSMAPI.Framework.Reflection {

    /// <summary>Concrete implementation of <see cref="IReflectionHelper"/>.</summary>
    public class Reflector : IReflectionHelper {

        private const BindingFlags ALL = BindingFlags.Instance | BindingFlags.Static
                                       | BindingFlags.Public   | BindingFlags.NonPublic;

        // ── Cache ──────────────────────────────────────────────────────────────

        private readonly Dictionary<(Type, string, bool), FieldInfo?>    _fields   = new();
        private readonly Dictionary<(Type, string, bool), PropertyInfo?> _props    = new();
        private readonly Dictionary<(Type, string, bool), MethodInfo?>   _methods  = new();

        // ── Fields ────────────────────────────────────────────────────────────

        public IPrivateField<TValue> GetField<TValue>(object obj, string name, bool required = true)
            => GetField<TValue>(obj.GetType(), name, required, obj);

        public IPrivateField<TValue> GetField<TValue>(Type type, string name, bool required = true)
            => GetField<TValue>(type, name, required, null);

        private IPrivateField<TValue> GetField<TValue>(Type type, string name, bool required, object? target) {
            var key = (type, name, false);
            if (!_fields.TryGetValue(key, out FieldInfo? fi)) {
                fi = type.GetField(name, ALL);
                _fields[key] = fi;
            }
            if (fi == null && required)
                throw new InvalidOperationException($"The type '{type.FullName}' has no field '{name}'.");
            if (fi == null)
                return new NullField<TValue>();
            return new PrivateField<TValue>(fi, target);
        }

        // ── Properties ────────────────────────────────────────────────────────

        public IPrivateProperty<TValue> GetProperty<TValue>(object obj, string name, bool required = true)
            => GetProperty<TValue>(obj.GetType(), name, required, obj);

        public IPrivateProperty<TValue> GetProperty<TValue>(Type type, string name, bool required = true)
            => GetProperty<TValue>(type, name, required, null);

        private IPrivateProperty<TValue> GetProperty<TValue>(Type type, string name, bool required, object? target) {
            var key = (type, name, true);
            if (!_props.TryGetValue(key, out PropertyInfo? pi)) {
                pi = type.GetProperty(name, ALL);
                _props[key] = pi;
            }
            if (pi == null && required)
                throw new InvalidOperationException($"The type '{type.FullName}' has no property '{name}'.");
            if (pi == null)
                return new NullProperty<TValue>();
            return new PrivateProperty<TValue>(pi, target);
        }

        // ── Methods ───────────────────────────────────────────────────────────

        public IPrivateMethod GetMethod(object obj, string name, bool required = true)
            => GetMethod(obj.GetType(), name, required, obj);

        public IPrivateMethod GetMethod(Type type, string name, bool required = true)
            => GetMethod(type, name, required, null);

        private IPrivateMethod GetMethod(Type type, string name, bool required, object? target) {
            var key = (type, name, false);
            if (!_methods.TryGetValue(key, out MethodInfo? mi)) {
                mi = type.GetMethod(name, ALL);
                _methods[key] = mi;
            }
            if (mi == null && required)
                throw new InvalidOperationException($"The type '{type.FullName}' has no method '{name}'.");
            if (mi == null)
                return new NullMethod();
            return new PrivateMethod(mi, target);
        }

        // ── Inner implementations ─────────────────────────────────────────────

        private class PrivateField<TValue> : IPrivateField<TValue> {
            public FieldInfo FieldInfo { get; }
            private readonly object? _target;
            public PrivateField(FieldInfo fi, object? target) { FieldInfo = fi; _target = target; }
            public TValue    GetValue()           => (TValue)FieldInfo.GetValue(_target)!;
            public void      SetValue(TValue v)   => FieldInfo.SetValue(_target, v);
        }

        private class PrivateProperty<TValue> : IPrivateProperty<TValue> {
            public PropertyInfo PropertyInfo { get; }
            private readonly object? _target;
            public PrivateProperty(PropertyInfo pi, object? target) { PropertyInfo = pi; _target = target; }
            public TValue GetValue()          => (TValue)PropertyInfo.GetValue(_target)!;
            public void   SetValue(TValue v)  => PropertyInfo.SetValue(_target, v);
        }

        private class PrivateMethod : IPrivateMethod {
            public MethodInfo MethodInfo { get; }
            private readonly object? _target;
            public PrivateMethod(MethodInfo mi, object? target) { MethodInfo = mi; _target = target; }
            public void    Invoke(params object?[] args)          => MethodInfo.Invoke(_target, args);
            public TReturn Invoke<TReturn>(params object?[] args) => (TReturn)MethodInfo.Invoke(_target, args)!;
        }

        private class NullField<TValue>    : IPrivateField<TValue>    {
            public FieldInfo FieldInfo => null!;
            public TValue    GetValue() => default!;
            public void      SetValue(TValue _) { }
        }

        private class NullProperty<TValue> : IPrivateProperty<TValue> {
            public PropertyInfo PropertyInfo => null!;
            public TValue GetValue() => default!;
            public void   SetValue(TValue _) { }
        }

        private class NullMethod : IPrivateMethod {
            public MethodInfo MethodInfo => null!;
            public void    Invoke(params object?[] _) { }
            public TReturn Invoke<TReturn>(params object?[] _) => default!;
        }
    }
}
