using System;

namespace Opsi.Common
{
    public struct Option<T>
    {
        private readonly T _value;

        private Option(T value)
        {
            _value = value;
            IsSome = true;
        }

        public bool IsSome { get; }

        public bool IsNone => !IsSome;

        public T Value
        {
            get
            {
                if (!IsSome)
                {
                    throw new NullReferenceException();
                }

                return _value;
            }
        }

        public static Option<T> None()
        {
            return new Option<T>();
        }

        public static Option<T> Some(T value)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            Validate.NotNull(value, nameof(value));
#pragma warning restore CS8604 // Possible null reference argument.
            return new Option<T>(value);
        }
    }
}