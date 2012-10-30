using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    class AnonymousEitherVisitor<TLeft, TRight, TResult> : IEitherVisitor<TLeft, TRight, TResult>
    {
        Func<TLeft, TResult> visitLeft;
        Func<TRight, TResult> visitRight;

        public AnonymousEitherVisitor(Func<TLeft, TResult> visitLeft, Func<TRight, TResult> visitRight)
        {
            this.visitLeft = visitLeft;
            this.visitRight = visitRight;
        }

        public TResult VisitLeft(TLeft value)
        {
            return visitLeft(value);
        }

        public TResult VisitRight(TRight value)
        {
            return visitRight(value);
        }
    }
}
