// Benchpress: A collection of micro-benchmarks.
// Translated by hand from JavaScript.

using System;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace V8Benchpress
{

    // -----------------------------------------------------------------------------
    // F r a m e w o r k
    // -----------------------------------------------------------------------------
    public abstract class Benchmark
    {
        public abstract string Name { get; }
        public abstract void Run();
    }

    // -----------------------------------------------------------------------------
    // F i b o n a c c i
    // -----------------------------------------------------------------------------
    public class Fibonacci : Benchmark
    {
        private static int doFib(int n)
        {
            if (n <= 1) return 1;
            return doFib(n - 1) + doFib(n - 2);
        }

        public override void Run()
        {
            var result = doFib(20);
            if (result != 10946) throw new InvalidOperationException("Wrong result: " + result + " should be: 10946");
        }

        public override string Name { get { return "Fibonacci"; } }
    }

    // -----------------------------------------------------------------------------
    // L o o p
    // -----------------------------------------------------------------------------
    public class Loop : Benchmark
    {
        private static void loop()
        {
            var sum = 0;
            for (var i = 0; i < 200; i++)
            {
                for (var j = 0; j < 100; j++)
                    sum++;
            }
            if (sum != 20000) throw new InvalidOperationException("Wrong result: " + sum + " should be: 20000");
        }

        public override void Run()
        {
            loop();
        }

        public override string Name { get { return "Loop"; } }
    }


    // -----------------------------------------------------------------------------
    // T o w e r s
    // -----------------------------------------------------------------------------
    public class Towers : Benchmark
    {
        private class TowersDisk
        {
            public int size;
            public TowersDisk next;

            public TowersDisk(int size)
            {
                this.size = size;
            }
        }

        private TowersDisk[] towersPiles;
        private int towersMovesDone;

        private void towersPush(int pile, TowersDisk disk)
        {
            var top = towersPiles[pile];
            if ((top != null) && (disk.size >= top.size))
                throw new InvalidOperationException("Cannot put a big disk on a smaller disk");
            disk.next = top;
            towersPiles[pile] = disk;
        }

        private TowersDisk towersPop(int pile)
        {
            var top = towersPiles[pile];
            if (top == null) throw new InvalidOperationException("Attempting to remove a disk from an empty pile");
            towersPiles[pile] = top.next;
            top.next = null;
            return top;
        }

        private void towersMoveTop(int from, int to)
        {
            towersPush(to, towersPop(from));
            towersMovesDone++;
        }

        private void towersMove(int from, int to, int disks)
        {
            if (disks == 1)
                towersMoveTop(from, to);
            else
            {
                var other = 3 - from - to;
                towersMove(from, other, disks - 1);
                towersMoveTop(from, to);
                towersMove(other, to, disks - 1);
            }
        }

        private void towersBuild(int pile, int disks)
        {
            for (var i = disks - 1; i >= 0; i--)
                towersPush(pile, new TowersDisk(i));
        }

        public override void Run()
        {
            towersPiles = new TowersDisk[3];
            towersMovesDone = 0;

            towersBuild(0, 13);
            towersMove(0, 1, 13);
            if (towersMovesDone != 8191)
                throw new InvalidOperationException("Error in result: " + towersMovesDone + " should be: 8191");
        }

        public override string Name { get { return "Towers"; } }
    }

    // -----------------------------------------------------------------------------
    // S i e v e
    // -----------------------------------------------------------------------------
    public class Sieve : Benchmark
    {
        private int doSieve(bool[] flags, int size)
        {
            var primeCount = 0;
            for (var i = 1; i < size; i++) flags[i] = true;
            for (var i = 2; i < size; i++)
            {
                if (flags[i])
                {
                    primeCount++;
                    for (var k = i + 1; k <= size; k += i) flags[k - 1] = false;
                }
            }
            return primeCount;
        }

        public override void Run()
        {
            var flags = new bool[1001];
            var result = doSieve(flags, 1000);
            if (result != 168)
                throw new InvalidOperationException("Wrong result: " + result + " should be: 168");
        }

        public override string Name { get { return "Seive"; } }
    }

    // -----------------------------------------------------------------------------
    // P e r m u t e
    // -----------------------------------------------------------------------------
    public class Permute : Benchmark
    {
        private int permuteCount;

        public static void swap(int n, int k, int[] array)
        {
            var tmp = array[n];
            array[n] = array[k];
            array[k] = tmp;
        }

        public void doPermute(int n, int[] array)
        {
            permuteCount++;
            if (n != 1)
            {
                doPermute(n - 1, array);
                for (var k = n - 1; k >= 1; k--)
                {
                    swap(n, k, array);
                    doPermute(n - 1, array);
                    swap(n, k, array);
                }
            }
        }

        public override void Run()
        {
            permuteCount = 0;

            var array = new int[8];
            for (var i = 1; i <= 7; i++) array[i] = i - 1;
            permuteCount = 0;
            doPermute(7, array);
            if (permuteCount != 8660) throw new InvalidOperationException("Wrong result: " + permuteCount + " should be: 8660");
        }

        public override string Name { get { return "Permute"; } }
    }

    // -----------------------------------------------------------------------------
    // Q u e e n s
    // -----------------------------------------------------------------------------
    public class Queens : Benchmark
    {
        private static bool tryQueens(int i, bool[] a, bool[] b, bool[] c, int[] x)
        {
            var j = 0;
            var q = false;
            while ((!q) && (j != 8))
            {
                j++;
                q = false;
                if (b[j] && a[i + j] && c[i - j + 7])
                {
                    x[i] = j;
                    b[j] = false;
                    a[i + j] = false;
                    c[i - j + 7] = false;
                    if (i < 8)
                    {
                        q = tryQueens(i + 1, a, b, c, x);
                        if (!q)
                        {
                            b[j] = true;
                            a[i + j] = true;
                            c[i - j + 7] = true;
                        }
                    }
                    else
                    {
                        q = true;
                    }
                }
            }
            return q;
        }

        public override void Run()
        {
            var a = new bool[9];
            var b = new bool[17];
            var c = new bool[15];
            var x = new int[9];
            for (var i = -7; i <= 16; i++)
            {
                if ((i >= 1) && (i <= 8)) a[i] = true;
                if (i >= 2) b[i] = true;
                if (i <= 7) c[i + 7] = true;
            }

            if (!tryQueens(1, b, a, c, x))
                throw new InvalidOperationException("Error in queens");
        }

        public override string Name { get { return "Queens"; } }
    }

    // -----------------------------------------------------------------------------
    // R e c u r s e
    // -----------------------------------------------------------------------------
    public class Recurse : Benchmark
    {
        private static int recurse(int n)
        {
            if (n <= 0) return 1;
            recurse(n - 1);
            return recurse(n - 1);
        }

        public override void Run()
        {
            recurse(13);
        }

        public override string Name { get { return "Recurse"; } }
    }

    // -----------------------------------------------------------------------------
    // S u m
    // -----------------------------------------------------------------------------
    public class Sum : Benchmark
    {
        private static int doSum(int start, int end)
        {
            var sum = 0;
            for (var i = start; i <= end; i++) sum += i;
            return sum;
        }

        public override void Run()
        {
            var result = doSum(1, 10000);
            if (result != 50005000) throw new InvalidOperationException("Wrong result: " + result + " should be: 50005000");
        }

        public override string Name { get { return "Sum"; } }
    }

    // -----------------------------------------------------------------------------
    // H e l p e r   f u n c t i o n s   f o r   s o r t s
    // -----------------------------------------------------------------------------
    public class SortData
    {
        protected const int randomInitialSeed = 74755;
        protected static int randomSeed;

        protected static int random()
        {
            randomSeed = ((randomSeed * 1309) + 13849) % 65536;
            return randomSeed;
        }

        public int min;
        public int max;
        public int[] array;

        public SortData(int length)
        {
            randomSeed = randomInitialSeed;
            array = new int[length];
            for (var i = 0; i < length; i++) array[i] = random();

            min = array[0];
            max = min;
            for (var i = 0; i < length; i++)
            {
                var e = array[i];
                if (e > max) max = e;
                if (e < min) min = e;
            }
        }

        public void Check()
        {
            if ((array[0] != min) || (array[array.Length - 1] != max))
                throw new InvalidOperationException("Array is not sorted");
            for (var i = 1; i < array.Length; i++)
            {
                if (array[i - 1] > array[i]) throw new InvalidOperationException("Array is not sorted");
            }
        }
    }

    // -----------------------------------------------------------------------------
    // B u b b l e S o r t
    // -----------------------------------------------------------------------------
    public class BubbleSort : Benchmark
    {
        private static void doBubblesort(int[] a)
        {
            for (var i = a.Length - 2; i >= 0; i--)
            {
                for (var j = 0; j <= i; j++)
                {
                    var c = a[j];
                    var n = a[j + 1];
                    if (c > n)
                    {
                        a[j] = n;
                        a[j + 1] = c;
                    }
                }
            }
        }

        public override void Run()
        {
            var data = new SortData(130);
            doBubblesort(data.array);
            data.Check();
        }

        public override string Name { get { return "BubbleSort"; } }
    }

    // -----------------------------------------------------------------------------
    // Q u i c k S o r t
    // -----------------------------------------------------------------------------
    public class QuickSort : Benchmark
    {
        private static void doQuicksort(int[] a, int low, int high)
        {
            var pivot = a[(low + high) >> 1];
            var i = low;
            var j = high;
            while (i <= j)
            {
                while (a[i] < pivot) i++;
                while (pivot < a[j]) j--;
                if (i <= j)
                {
                    var tmp = a[i];
                    a[i] = a[j];
                    a[j] = tmp;
                    i++;
                    j--;
                }
            }

            if (low < j) doQuicksort(a, low, j);
            if (i < high) doQuicksort(a, i, high);
        }

        public override void Run()
        {
            var data = new SortData(800);
            doQuicksort(data.array, 0, data.array.Length - 1);
            data.Check();
        }

        public override string Name { get { return "QuickSort"; } }
    }

    // -----------------------------------------------------------------------------
    // T r e e S o r t
    // -----------------------------------------------------------------------------
    public class TreeSort : Benchmark
    {
        private class TreeNode
        {
            private readonly int value;
            private TreeNode left;
            private TreeNode right;

            public TreeNode(int value)
            {
                this.value = value;
            }

            public void insert(int n)
            {
                if (n < value)
                {
                    if (left == null) left = new TreeNode(n);
                    else left.insert(n);
                }
                else
                {
                    if (right == null) right = new TreeNode(n);
                    else right.insert(n);
                }
            }

            public bool check()
            {
                return ((left == null) || ((left.value < value) && left.check())) &&
                       ((right == null) || ((right.value >= value) && right.check()));
            }
        }

        public override void Run()
        {
            var data = new SortData(1000);
            var tree = new TreeNode(data.array[0]);
            for (var i = 1; i < data.array.Length; i++) tree.insert(data.array[i]);
            if (!tree.check()) throw new InvalidOperationException("Invalid result, tree not sorted");
        }

        public override string Name { get { return "TreeSort"; } }
    }

    // -----------------------------------------------------------------------------
    //  T a k
    // -----------------------------------------------------------------------------
    public class Tak : Benchmark
    {
        private static int tak(int x, int y, int z)
        {
            if (y >= x) return z;
            return tak(tak(x - 1, y, z), tak(y - 1, z, x), tak(z - 1, x, y));
        }

        public override void Run()
        {
            tak(18, 12, 6);
        }

        public override string Name { get { return "Tak"; } }
    }

    // -----------------------------------------------------------------------------
    //  T a k l
    // -----------------------------------------------------------------------------
    public class Takl : Benchmark
    {
        private class ListElement
        {
            public readonly int length;
            public readonly ListElement next;

            public ListElement(int length, ListElement next)
            {
                this.length = length;
                this.next = next;
            }
        }

        private static ListElement makeList(int length)
        {
            if (length == 0) return null;
            return new ListElement(length, makeList(length - 1));
        }

        private static bool isShorter(ListElement x, ListElement y)
        {
            var xTail = x;
            var yTail = y;
            while (yTail != null)
            {
                if (xTail == null) return true;
                xTail = xTail.next;
                yTail = yTail.next;
            }
            return false;
        }

        private static ListElement doTakl(ListElement x, ListElement y, ListElement z)
        {
            if (isShorter(y, x))
                return doTakl(doTakl(x.next, y, z), doTakl(y.next, z, x), doTakl(z.next, x, y));
            else
                return z;
        }

        public override void Run()
        {
            var result = doTakl(makeList(15), makeList(10), makeList(6));
            if (result.length != 10)
                throw new InvalidOperationException("Wrong result: " + result.length + " should be: 10");
        }

        public override string Name { get { return "Takl"; } }
    }

    // -----------------------------------------------------------------------------
    // M a i n
    // -----------------------------------------------------------------------------
    public static class Benchmarks
    {
        private const int iterations = 500;

        [Import(@"function(message) {
                      var tt = document.createElement('tt');
                      tt.innerText = message.replace(new RegExp('/</g'), '&lt;');
                      var div = document.createElement('div');
                      div.appendChild(tt);
                      document.body.appendChild(div);
                  }")]
        extern public static void Log(string message);

        public static void Main()
        {
            var benchmarks = new Benchmark[]
                                 {
                                     new Fibonacci(),
                                     new Loop(),
                                     new Towers(),
                                     new Sieve(),
                                     new Permute(),
                                     new Queens(),
                                     new Recurse(),
                                     new Sum(),
                                     new BubbleSort(),
                                     new QuickSort(),
                                     new TreeSort(),
                                     new Tak(),
                                     new Takl()
                                 };

            var then = DateTime.Now;
            for (var i = 0; i < benchmarks.Length; i++)
            {
                var testthen = DateTime.Now;
                for (var iter = 0; iter < iterations; iter++)
                    benchmarks[i].Run();
                var testnow = DateTime.Now;
                Log(benchmarks[i].Name + ": " + (testnow - testthen).TotalMilliseconds + "ms");
            }
            var now = DateTime.Now;
            Log("Overall: " + (now - then).TotalMilliseconds + "ms");
        }
    }
}