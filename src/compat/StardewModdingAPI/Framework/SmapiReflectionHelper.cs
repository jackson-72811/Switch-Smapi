using System;
using System.Reflection;
using InternalReflector = SwitchSMAPI.Framework.Reflection.Reflector;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// Bridges <see cref="IReflectionHelper"/> to the internal reflector,
    /// wrapping each result in an adapter that implements the compat interfaces.
    /// </summary>
    internal sealed class SmapiReflectionHelper : IReflectionHelper {

        private readonly InternalReflector _inner;

        public SmapiReflectionHelper(InternalReflector inner) => _inner = inner;

        // ── Fields ────────────────────────────────────────────────────────────

        public IPrivateField<TValue> GetField<TValue>(object obj, string name, bool required = true)
            => new FieldWrapper<TValue>(_inner.GetField<TValue>(obj, name, required));

        public IPrivateField<TValue> GetField<TValue>(Type type, string name, bool required = true)
            => new FieldWrapper<TValue>(_inner.GetField<TValue>(type, name, required));

        // ── Properties ────────────────────────────────────────────────────────

        public IPrivateProperty<TValue> GetProperty<TValue>(object obj, string name, bool required = true)
            => new PropertyWrapper<TValue>(_inner.GetProperty<TValue>(obj, name, required));

        public IPrivateProperty<TValue> GetProperty<TValue>(Type type, string name, bool required = true)
            => new PropertyWrapper<TValue>(_inner.GetProperty<TValue>(type, name, required));

        // ── Methods ───────────────────────────────────────────────────────────

        public IPrivateMethod GetMethod(object obj, string name, bool required = true)
            => new MethodWrapper(_inner.GetMethod(obj, name, required));

        public IPrivateMethod GetMethod(Type type, string name, bool required = true)
            => new MethodWrapper(_inner.GetMethod(type, name, required));

        // ── Adapters ──────────────────────────────────────────────────────────

        private sealed class FieldWrapper<TValue> : IPrivateField<TValue> {
            private readonly SwitchSMAPI.Framework.Reflection.IPrivateField<TValue> _f;
            public FieldWrapper(SwitchSMAPI.Framework.Reflection.IPrivateField<TValue> f) => _f = f;
            public FieldInfo FieldInfo  => _f.FieldInfo;
            public TValue    GetValue() => _f.GetValue();
            public void      SetValue(TValue value) => _f.SetValue(value);
        }

        private sealed class PropertyWrapper<TValue> : IPrivateProperty<TValue> {
            private readonly SwitchSMAPI.Framework.Reflection.IPrivateProperty<TValue> _p;
            public PropertyWrapper(SwitchSMAPI.Framework.Reflection.IPrivateProperty<TValue> p) => _p = p;
            public PropertyInfo PropertyInfo => _p.PropertyInfo;
            public TValue GetValue()         => _p.GetValue();
            public void   SetValue(TValue v) => _p.SetValue(v);
        }

        private sealed class MethodWrapper : IPrivateMethod {
            private readonly SwitchSMAPI.Framework.Reflection.IPrivateMethod _m;
            public MethodWrapper(SwitchSMAPI.Framework.Reflection.IPrivateMethod m) => _m = m;
            public MethodInfo MethodInfo => _m.MethodInfo;
            public void    Invoke(params object?[] args)          => _m.Invoke(args);
            public TReturn Invoke<TReturn>(params object?[] args) => _m.Invoke<TReturn>(args);
        }
    }
}
