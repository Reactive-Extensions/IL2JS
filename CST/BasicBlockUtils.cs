//
// Various algorithms on basic block graphs
//

using System;
using Microsoft.LiveLabs.Extras;
using JST=Microsoft.LiveLabs.JavaScript.JST;

namespace Microsoft.LiveLabs.CST
{

    public class BBEdge
    {
        public BasicBlock Source;
        public BasicBlock Target;

        public override bool Equals(object obj)
        {
            var edge = obj as BBEdge;
            return edge != null && Source.Equals(edge.Source) && Target.Equals(edge.Target);
        }

        protected static int Rot17(int v)
        {
            return (int)(((uint)v << 17) | ((uint)v >> 15));
        }

        public override int GetHashCode()
        {
            return Source.GetHashCode() ^ Rot17(Target.GetHashCode());
        }
    }

    public enum LoopFlavor
    {
        Loop,
        DoWhile,
        FlippedDoWhile,
        WhileDo,
        FlippedWhileDo,
        Unknown
    }

    public class BBLoop
    {
        public BasicBlock Head;
        public BasicBlock Tail;
        public IMSet<BasicBlock> Body;
        public LoopFlavor Flavor;
        public JST.Identifier Label;

        public BBLoop(BasicBlock head, BasicBlock tail, IMSet<BasicBlock> body, JST.Identifier label)
        {
            Head = head;
            Tail = tail;
            Body = body;
            Label = label;

            var headEscapes = false;
            var headbranchbb = head as BranchBasicBlock;
            foreach (var t in head.Targets)
            {
                if (!body.Contains(t))
                    headEscapes = true;
            }

            var tailEscapes = false;
            var tailbranchbb = tail as BranchBasicBlock;
            foreach (var t in tail.Targets)
            {
                if (!body.Contains(t))
                    tailEscapes = true;
            }

            if (!headEscapes && tailEscapes && tailbranchbb != null)
            {
                if (tailbranchbb.Target.Equals(head))
                    Flavor = LoopFlavor.DoWhile;
                else if (tailbranchbb.Fallthrough.Equals(head))
                    Flavor = LoopFlavor.FlippedDoWhile;
                else
                    throw new InvalidOperationException("invalid loop");
            }
            else if (headEscapes && !tailEscapes && headbranchbb != null)
            {
                if (body.Contains(headbranchbb.Target))
                    Flavor = LoopFlavor.WhileDo;
                else if (body.Contains(headbranchbb.Fallthrough))
                    Flavor = LoopFlavor.FlippedWhileDo;
                else
                    throw new InvalidOperationException("invalid loop");
            }
            else if (!headEscapes && !tailEscapes)
                Flavor = LoopFlavor.Loop;
            else if (headEscapes && tailEscapes && headbranchbb != null && tailbranchbb != null)
            {
                // Could encode as do-while with a break at start, or while-do with a break at end.
                if (body.Contains(headbranchbb.Target))
                    Flavor = LoopFlavor.WhileDo;
                else if (body.Contains(headbranchbb.Fallthrough))
                    Flavor = LoopFlavor.FlippedWhileDo;
                else
                    throw new InvalidOperationException("invalid loop");
            }
            else
                Flavor = LoopFlavor.Unknown;
        }

        public BasicBlock ContinueTarget
        {
            get
            {
                switch (Flavor)
                {
                    case LoopFlavor.Loop:
                    case LoopFlavor.WhileDo:
                    case LoopFlavor.FlippedWhileDo:
                        return Head;
                    case LoopFlavor.DoWhile:
                    case LoopFlavor.FlippedDoWhile:
                        return Tail;
                    case LoopFlavor.Unknown:
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public BasicBlock LoopControl
        {
            get {
                switch (Flavor)
                {
                    case LoopFlavor.Loop:
                    case LoopFlavor.WhileDo:
                    case LoopFlavor.FlippedWhileDo:
                        return Head;
                    case LoopFlavor.DoWhile:
                    case LoopFlavor.FlippedDoWhile:
                        return Tail;
                    case LoopFlavor.Unknown:
                        return null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void Append(Writer w)
        {
            if (Label != null)
            {
                w.Append(Label.Value);
                w.Append(": ");
            }
            w.Append("Head = B");
            w.Append(Head.Id);
            w.Append(", Tail = B");
            w.Append(Tail.Id);
            w.Append(", Body = { ");
            var first = true;
            foreach (var bb in Body)
            {
                if (first)
                    first = false;
                else
                    w.Append(", ");
                w.Append("B");
                w.Append(bb.Id);
            }
            w.Append(" }, Flavor = ");
            switch (Flavor)
            {
                case LoopFlavor.Loop:
                    w.Append("LOOP");
                    break;
                case LoopFlavor.DoWhile:
                    w.Append("DO-WHILE");
                    break;
                case LoopFlavor.FlippedDoWhile:
                    w.Append("DO-WHILE (flipped)");
                    break;
                case LoopFlavor.WhileDo:
                    w.Append("WHILE-DO");
                    break;
                case LoopFlavor.FlippedWhileDo:
                    w.Append("WHILE-DO (flipped)");
                    break;
                case LoopFlavor.Unknown:
                    w.Append("UNKNOWN");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


    public static class BasicBlockUtils
    {

        public static ISeq<BasicBlock> PostOrder(BasicBlock root)
        {
            var res = new Seq<BasicBlock>();
            PostOrderFrom(root, res, new Set<BasicBlock>());
            return res;
        }

        private static void PostOrderFrom(BasicBlock bb, ISeq<BasicBlock> res, Set<BasicBlock> visited)
        {
            if (!visited.Contains(bb))
            {
                visited.Add(bb);
                foreach (var t in bb.Targets)
                    PostOrderFrom(t, res, visited);
                res.Add(bb);
            }
        }

        public static ISeq<BasicBlock> PreOrder(BasicBlock root)
        {
            var res = new Seq<BasicBlock>();
            PreOrderFrom(root, res, new Set<BasicBlock>());
            return res;
        }

        private static void PreOrderFrom(BasicBlock bb, ISeq<BasicBlock> res, Set<BasicBlock> visited)
        {
            if (!visited.Contains(bb))
            {
                visited.Add(bb);
                res.Add(bb);
                foreach (var t in bb.Targets)
                    PreOrderFrom(t, res, visited);
            }
        }

        public static IMap<BasicBlock, IMSet<BasicBlock>> Dominators(BasicBlock root)
        {
            var preorder = PreOrder(root);
            var map = new Map<BasicBlock, IntSet>(preorder.Count);
            for (var i = 0; i < preorder.Count; i++)
            {
                var vec = new IntSet(preorder.Count);
                if (preorder[i].Equals(root))
                    vec[i] = true;
                else
                    vec.SetAll(true);
                map.Add(preorder[i], vec);
            }
            var change = default(bool);
            do
            {
                change = false;
                for (var i = 0; i < preorder.Count; i++)
                {
                    var bb = preorder[i];
                    if (!bb.Equals(root))
                    {
                        var vec = new IntSet(preorder.Count);
                        vec.SetAll(true);
                        foreach (var s in bb.Sources)
                            vec.IntersectInPlace(map[s]);
                        vec[i] = true;
                        if (!vec.Equals(map[bb]))
                        {
                            map[bb] = vec;
                            change = true;
                        }
                    }
                }
            }
            while (change);
            var res = new Map<BasicBlock, IMSet<BasicBlock>>();
            for (var i = 0; i < preorder.Count; i++)
            {
                var set = new Set<BasicBlock>();
                var vec = map[preorder[i]];
                foreach (var j in vec)
                    set.Add(preorder[j]);
                res.Add(preorder[i], set);
            }
            return res;
        }

        public static ISeq<BBEdge> AllEdges(BasicBlock root)
        {
            var res = new Seq<BBEdge>();
            AllEdgesFrom(root, res, new Set<BasicBlock>());
            return res;
        }

        private static void AllEdgesFrom(BasicBlock bb, ISeq<BBEdge> edges, Set<BasicBlock> visited)
        {
            if (!visited.Contains(bb))
            {
                visited.Add(bb);
                foreach (var t in bb.Targets)
                {
                    var edge = new BBEdge { Source = bb, Target = t };
                    edges.Add(edge);
                    AllEdgesFrom(t, edges, visited);
                }
            }
        }

        public static ISeq<BBEdge> BackEdges(BasicBlock root)
        {
            var res = new Seq<BBEdge>();
            var edges = AllEdges(root);
            var dominators = Dominators(root);
            foreach (var edge in edges)
            {
                if (dominators[edge.Source].Contains(edge.Target))
                    res.Add(edge);
            }
            return res;
        }

        private static void ReachableFrom(BasicBlock bb, Set<BasicBlock> visited)
        {
            if (!visited.Contains(bb))
            {
                visited.Add(bb);
                foreach (var s in bb.Sources)
                    ReachableFrom(s, visited);
            }
        }

        public static ISeq<BBLoop> Loops(BasicBlock root)
        {
            var res = new Seq<BBLoop>();
            var backEdges = BackEdges(root);
            foreach (var backEdge in backEdges)
            {
                var body = new Set<BasicBlock> { backEdge.Target };
                ReachableFrom(backEdge.Source, body);
                res.Add(new BBLoop(backEdge.Target, backEdge.Source, body, null));
            }
            return res;
        }
    }
}