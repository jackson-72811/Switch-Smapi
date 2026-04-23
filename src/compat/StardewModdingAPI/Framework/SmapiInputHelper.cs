using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using InternalInputHelper = SwitchSMAPI.Framework.Input.InputHelper;
using InternalCursor      = SwitchSMAPI.Framework.Events.ICursorPosition;

namespace StardewModdingAPI.Framework {

    /// <summary>Bridges <see cref="IInputHelper"/> to the internal input helper.</summary>
    internal sealed class SmapiInputHelper : IInputHelper {

        private readonly InternalInputHelper _inner;

        public SmapiInputHelper(InternalInputHelper inner) => _inner = inner;

        public ICursorPosition GetCursorPosition()
            => new CursorAdapter(_inner.GetCursorPosition());

        public bool IsDown(SButton button)
            => _inner.IsDown(SButtonConverter.ToInternal(button));

        public bool IsSuppressed(SButton button)
            => _inner.IsSuppressed(SButtonConverter.ToInternal(button));

        public void Suppress(SButton button)
            => _inner.Suppress(SButtonConverter.ToInternal(button));

        public SButtonState GetState(SButton button) {
            var ib = SButtonConverter.ToInternal(button);
            if (_inner.IsSuppressed(ib)) return SButtonState.None;
            bool isDown = _inner.IsDown(ib);
            bool wasDown = _inner.IsPressedThisTick(ib);
            if (wasDown && isDown) return SButtonState.Pressed;
            if (isDown)           return SButtonState.Held;
            return SButtonState.None;
        }

        public IEnumerable<SButton> GetPressedButtons()
            => _inner.GetPressedButtons().Select(SButtonConverter.ToCompat);

        // ── ICursorPosition adapter ───────────────────────────────────────────

        private sealed class CursorAdapter : ICursorPosition {
            private readonly InternalCursor _c;
            public CursorAdapter(InternalCursor c) => _c = c;
            public Vector2 ScreenPixels   => _c.ScreenPixels;
            public Vector2 Tile           => _c.Tile;
            public Vector2 AbsolutePixels => _c.AbsolutePixels;
            public ICursorPosition Clone() => new CursorAdapter(_c);
        }
    }
}
