using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    class AnonymousEitherVisitor<TLeft, TRight> : IEitherVisitor<TLeft, TRight>
    {
        Action<TLeft> visitLeft;
        Action<TRight> visitRight;

        public AnonymousEitherVisitor(Action<TLeft> visitLeft, Action<TRight> visitRight)
        {
            this.visitLeft = visitLeft;
            this.visitRight = visitRight;
        }

        public void VisitLeft(TLeft value)
        {
            visitLeft(value);
        }

        public void VisitRight(TRight value)
        {
            visitRight(value);
        }
    }
}
