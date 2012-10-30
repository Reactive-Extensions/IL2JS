using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;
using System.Globalization;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive
{
    abstract class Either<TLeft, TRight>
    {
        Either()
        {
        }

        public static Either<TLeft, TRight> CreateLeft(TLeft value)
        {
            return new Either<TLeft, TRight>.Left(value);
        }

        public static Either<TLeft, TRight> CreateRight(TRight value)
        {
            return new Either<TLeft, TRight>.Right(value);
        }

        public abstract TResult Switch<TResult>(Func<TLeft, TResult> caseLeft, Func<TRight, TResult> caseRight);
        public abstract void Switch(Action<TLeft> caseLeft, Action<TRight> caseRight);

        public sealed class Left : Either<TLeft, TRight>, IEquatable<Left>
        {
            public TLeft Value { get; private set; }

            public Left(TLeft value)
            {
                Value = value;
            }

            public override TResult Switch<TResult>(Func<TLeft, TResult> caseLeft, Func<TRight, TResult> caseRight)
            {
                return caseLeft(Value);
            }

            public override void Switch(Action<TLeft> caseLeft, Action<TRight> caseRight)
            {
                caseLeft(Value);
            }

            public bool Equals(Left other)
            {
                if (other == this)
                    return true;
                if (other == null)
                    return false;
                return EqualityComparer<TLeft>.Default.Equals(Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Left);
            }

            public override int GetHashCode()
            {
                return EqualityComparer<TLeft>.Default.GetHashCode(Value);
            }

            public override string ToString()
            {
#if IL2JS
                return string.Format("Left({0})", Value);
#else
                return string.Format(CultureInfo.CurrentCulture, "Left({0})", Value);
#endif
            }
        }

        public sealed class Right : Either<TLeft, TRight>, IEquatable<Right>
        {
            public TRight Value { get; private set; }

            public Right(TRight value)
            {
                Value = value;
            }

            public override TResult Switch<TResult>(Func<TLeft, TResult> caseLeft, Func<TRight, TResult> caseRight)
            {
                return caseRight(Value);
            }

            public override void Switch(Action<TLeft> caseLeft, Action<TRight> caseRight)
            {
                caseRight(Value);
            }

            public bool Equals(Right other)
            {
                if (other == this)
                    return true;
                if (other == null)
                    return false;
                return EqualityComparer<TRight>.Default.Equals(Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Right);
            }

            public override int GetHashCode()
            {
                return EqualityComparer<TRight>.Default.GetHashCode(Value);
            }

            public override string ToString()
            {
#if IL2JS
                return string.Format("Right({0})", Value);
#else
                return string.Format(CultureInfo.CurrentCulture, "Right({0})", Value);
#endif
            }
        }
    }
}
