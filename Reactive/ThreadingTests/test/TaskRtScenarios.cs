using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace plinq_devtests
{
    internal static class TaskRtScenarios
    {

        //
        // Some simple TPL scenario tests.
        //

        internal static bool RunTaskScenarioTests()
        {
            bool passed = true;

            // Matrix multiply:
            passed &= MatrixMultiplySample.Test(2);
            passed &= MatrixMultiplySample.Test(8);
            passed &= MatrixMultiplySample.Test(32);
            passed &= MatrixMultiplySample.Test(64);
            passed &= MatrixMultiplySample.Test(128);
            passed &= MatrixMultiplySample.Test(256);
            passed &= MatrixMultiplySample.Test(400);

            //// N-queens:
            NQueensTest.NQueens();

            //// Sorting:
            passed &= SortSample.Test(1024);
            passed &= SortSample.Test(16 * 1024);
#if !PFX_LEGACY_3_5
            passed &= SortSample.Test(16 * 1024 * 1024);
#endif
            // Some other random scenarios:
            passed &= Tree.Test();
            passed &= Fib.Test(8);
            passed &= Fib.Test(16);
#if !PFX_LEGACY_3_5
            passed &= Fib.Test(32);
#endif

            return passed;
        }

        /*----------------------------------------------------------------------------------------
           This sample shows proper parallel matrix multiplication. 
           It is adapted from a similar sample in the CILK demos.
        ----------------------------------------------------------------------------------------*/

        class MatrixMultiplySample
        {
            const int Threshold = 64;

            public static bool Test(int size)
            {
                TestHarness.TestLog("* MatrixMultiplySample.Test({0}x{0})", size);

                Matrix m = new Matrix(size);
                Matrix n = new Matrix(size);
                Matrix res1 = new Matrix(size);
                Matrix res2 = new Matrix(size);
                Initialize(m, n);

                Multiply2(m, n, res1); // Sequential.
                Multiply(m, n, res2); // Parallel.

                return CheckResult(res1) && CheckResult(res2);
            }

            /*----------------------------------------------------------------------------------------
               Initialize and check results (by using unit matrices, the result elements all contain 
               the size of the matrix)
            ----------------------------------------------------------------------------------------*/
            static void Initialize(Matrix m, Matrix n)
            {
                for (int c = 0; c < m.Size; c++)
                {
                    for (int r = 0; r < n.Size; r++)
                    {
                        m[r, c] = 1.0F;
                        n[r, c] = 1.0F;
                    }
                }
            }

            static bool CheckResult(Matrix matrix)
            {
                for (int r = 0; r < matrix.Size; r++)
                {
                    for (int c = 0; c < matrix.Size; c++)
                    {
                        if (matrix[r, c] != matrix.Size)
                        {
                            TestHarness.TestLog("  > Matrix multiply failed! ([" + r + "," + c + "] = " + matrix[r, c] + ")");
                            return false;
                        }
                    }
                }

                return true;
            }

            /*----------------------------------------------------------------------------------------
               Parallel matrix multiplication using quad-multiplication. (Adapted from CILK demos)
      
               |m00 | m01|   |n00 | n01|     |m00*n00 | m00*n01|   |m01*n10 | m01*n11|
               |----+----| x |----+----|  =  |--------+--------| + |--------+--------|
               |m10 | m11|   |n10 | n10|     |m10*n00 | m10*n10|   |m11*n10 | m11*n11|
            ----------------------------------------------------------------------------------------*/
            static void Multiply(Matrix m, Matrix n, Matrix r)
            {
                if (m.Size <= Math.Max(3, Threshold))
                {
                    Multiply2(m, n, r);
                }
                else
                {
                    Parallel.Invoke(
                      delegate { Multiply(m.Q00, n.Q00, r.Q00); Multiply(m.Q01, n.Q10, r.Q00); },
                      delegate { Multiply(m.Q00, n.Q01, r.Q01); Multiply(m.Q01, n.Q11, r.Q01); },
                      delegate { Multiply(m.Q10, n.Q00, r.Q10); Multiply(m.Q11, n.Q10, r.Q10); },
                      delegate { Multiply(m.Q10, n.Q01, r.Q11); Multiply(m.Q11, n.Q11, r.Q11); }
                    );
                }
            }

            static void Multiply2(Matrix m, Matrix n, Matrix r)
            {
                Debug.Assert(m.Size == n.Size);
                Debug.Assert(m.Size == r.Size);

                for (int i = 0; i < r.Size; i++)
                {
                    for (int j = 0; j < r.Size; j++)
                    {
                        for (int k = 0; k < r.Size; k++)
                        {
                            r[i, j] += m[i, k] * n[k, j];
                        }
                    }
                }
            }
        }


        /*----------------------------------------------------------------------------------------
           An abstraction for a slice of a row inside shared float[,] array
        ----------------------------------------------------------------------------------------*/
        struct Row
        {
            readonly int row;
            readonly int column;
            float[,] matrix;

            public Row(float[,] matrix, int row, int column)
            {
                this.row = row;
                this.column = column;
                this.matrix = matrix;
            }

            public float this[int c]
            {
                get { return matrix[row, column + c]; }
                set { matrix[row, column + c] = value; }
            }
        }


        /*----------------------------------------------------------------------------------------
           An abstraction for a square matrix inside shared float[,] array
        ----------------------------------------------------------------------------------------*/
        struct Matrix
        {
            readonly int column;
            readonly int row;
            int size;
            float[,] matrix;

            // create a new matrxi
            public Matrix(int size)
            {
                this.size = size;
                row = 0;
                column = 0;
                matrix = new float[size, size];
            }

            // create a matrix view on a float[,] array
            public Matrix(float[,] matrix)
                : this(matrix, 0, 0, matrix.GetLength(0))
            { }


            // create a new sub matrix view on another matrix
            public Matrix(float[,] matrix, int column, int row, int size)
            {
                this.matrix = matrix;
                this.column = column;
                this.row = row;
                this.size = size;
            }

            public float this[int r, int c]
            {
                get
                {
                    return matrix[row + r, column + c];
                }
                set
                {
                    matrix[row + r, column + c] = value;
                }
            }

            public Row this[int r]
            {
                get { return new Row(matrix, row, column); }
            }

            // Get a quadrant of a matrix
            public Matrix Q00 { get { int half = size / 2; return new Matrix(matrix, column, row, half); } }
            public Matrix Q01 { get { int half = size / 2; return new Matrix(matrix, column + half, row, half); } }
            public Matrix Q10 { get { int half = size / 2; return new Matrix(matrix, column, row + half, half); } }
            public Matrix Q11 { get { int half = size / 2; return new Matrix(matrix, column + half, row + half, half); } }

            public int Size { get { return size; } }
        }

        /* Solutions to NQueens:
        The following table gives the number of solutions for n queens, both unique and distinct .

        n:          1 2 3 4  5 6  7  8  9   10    11     12     13      14        15 
        unique:     1 0 0 1  2 1  6 12 46   92   341  1,787  9,233  45,752   285,053 
        distinct:   1 0 0 2 10 4 40 92 352 724 2,680 14,200 73,712 365,596 2,279,184 

        Note that the 6 queens puzzle has fewer solutions than the 5 queens puzzle.
        */

        class NQueensTest
        {

#if StopAtFirstSolution
            const int rows = 28;
            static volatile int[] result;    
#else
            const int rows = 13;
            static int resultCount;
#endif
            const int threshold = 8;

            static string ShowBoard(int[] board)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < board.Length; i++)
                {
                    sb.Append((i == 0 ? "[" : ",") + board[i]);
                }
                sb.Append("]");
                return sb.ToString();
            }

            internal static int NQueens()
            {
                TestHarness.TestLog("* NQueens.Test()");

#if StopAtFirstSolution
                  result = null;
                  Task.DoAll(delegate { NQueens(new int[0]); });
                  return ShowBoard(result);
#else
                resultCount = 0;
                Task.Factory.StartNew(delegate { NQueens(new int[0]); }).Wait();
                return resultCount;
#endif
            }


            static void NQueens(int[] board)
            {
#if StopAtFirstSolution
              if (result==null)  
#endif
                {
                    if (board.Length == rows)
                    {
                        // done
#if StopAtFirstSolution
                          result = board;
#else
                        System.Threading.Interlocked.Increment(ref resultCount);
#endif
                    }
                    else
                    {
                        // for each queen position in the next column..
                        // note: we do not use "Par.For" since in most cases there
                        // are no safe continuations and the delegate allocation
                        // would dominate the computation.
                        for (int q = 0; q < rows; q++)
                        {
                            // check if this is a safe spot
                            bool safe = true;
                            for (int r = 0; r < board.Length; r++)
                            {
                                int p = board[r];
                                if (q == p || q == p - (board.Length - r) || q == p + (board.Length - r))
                                {
                                    safe = false;
                                    break;
                                }
                            }

                            // concurrently explore from here
                            if (safe)
                            {
                                int[] nextBoard = new int[board.Length + 1];
                                for (int r = 0; r < board.Length; r++) nextBoard[r] = board[r];
                                nextBoard[board.Length] = q;
                                if (rows - board.Length <= threshold)  // do the last part sequentially
                                    NQueens(nextBoard);
                                else
                                    Task.Factory.StartNew(delegate { NQueens(nextBoard); });
                            }
                        }
                    }
                }
            }
        }

        /*----------------------------------------------------------------------------------------
           This sample parallel merge sort and parallel quick sort.
           It is adapted from a similar sample in the CILK demos.
        ----------------------------------------------------------------------------------------*/

        class SortSample
        {
            const int QuickThreshold = 256 * 1024;  // elements before merge sort uses parallel quick sort
            const int SeqQuickThreshold = 8 * 1024;    // elements before parallel quicksort use sequential quick sort
            const int InsertionThreshold = 32;          // elements before quick sort uses insertion sort

            static internal bool Test(int size)
            {
                TestHarness.TestLog("* SortSample.Test(size={0})", size);

                int[] input1 = new int[size];
                int[] input2 = new int[size];
                int[] input3 = new int[size];
                Random r = new Random(0);
                for (int i = 0; i < size; i++)
                {
                    input1[i] = input2[i] = input3[i] = r.Next();
                }

                SeqQuickSort(input1);
                QuickSort(input2);
                MergeSort(input3);

                if (!IsSorted(input1))
                {
                    TestHarness.TestLog("  > error: sequential sort failed");
                    return false;
                }
                if (!IsSorted(input2))
                {
                    TestHarness.TestLog("  > error: parallel quick sort failed");
                    return false;
                }
                if (!IsSorted(input3))
                {
                    TestHarness.TestLog("  > error: parallel merge sort failed");
                    return false;
                }

                return true;
            }

            private static bool IsSorted<T>(T[] domain) where T : IComparable<T>
            {
                for (int i = 0; i < domain.Length - 1; i++)
                {
                    if (domain[i].CompareTo(domain[i + 1]) > 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            /*----------------------------------------------------------------------------------------
               Parallel merge sort
            ----------------------------------------------------------------------------------------*/
            static void MergeSort<T>(T[] domain) where T : IComparable<T>
            {
                if (domain == null) return;
                if (domain.Length <= QuickThreshold)
                {
                    QuickSort(domain);
                }
                else
                {
                    T[] mergeDomain = new T[domain.Length];
                    MergeSort(domain, mergeDomain, 0, domain.Length);
                }
            }

            private static void MergeSort<T>(T[] domain, T[] mergeDomain, int lo, int n) where T : IComparable<T>
            {
                Debug.Assert(domain != null && mergeDomain != null);
                Debug.Assert(domain.Length == mergeDomain.Length);
                Debug.Assert(domain.Length >= lo + n - 1);

                if (n <= QuickThreshold)
                {
                    QuickSort(domain, lo, lo + n - 1);
                }
                else
                {
                    // 1) sort 4 quadrants in parallel.
                    // 2) merge first two quadrants and last two quadrants to the mergeDomain
                    // 3) merge the merged halves in mergeDomain back to the original domain
                    int part = n / 4;
                    Parallel.Invoke(
                      delegate
                      {
                          Parallel.Invoke(delegate { MergeSort(domain, mergeDomain, lo, part); }
                                     , delegate { MergeSort(domain, mergeDomain, lo + part, part); });
                          Merge(domain, lo, part
                               , domain, lo + part, part
                               , mergeDomain, lo);
                      },
                      delegate
                      {
                          Parallel.Invoke(delegate { MergeSort(domain, mergeDomain, lo + 2 * part, part); }
                                     , delegate { MergeSort(domain, mergeDomain, lo + 3 * part, n - 3 * part); });
                          Merge(domain, lo + 2 * part, part
                               , domain, lo + 3 * part, n - 3 * part
                               , mergeDomain, lo + 2 * part);
                      }
                    );
                    Merge(mergeDomain, lo, 2 * part
                         , mergeDomain, lo + 2 * part, n - 2 * part
                         , domain, lo);
                }
            }

            // Merge two sorted arrays into a sorted output array
            // (note: we could even merge in parallel by splitting the arrays first)
            private static void Merge<T>(T[] in1, int lo1, int n1,
                                         T[] in2, int lo2, int n2,
                                         T[] output, int lo) where T : IComparable<T>
            {
                int hi1 = lo1 + n1 - 1;
                int hi2 = lo2 + n2 - 1;
                int i = lo1;
                int j = lo2;
                int k = lo;
                T x = in1[i];
                T y = in2[j];
                while (true)
                {
                    if (x.CompareTo(y) <= 0)
                    {
                        output[k++] = x;
                        i++;
                        if (i > hi1) break;
                        x = in1[i];
                    }
                    else
                    {
                        output[k++] = y;
                        j++;
                        if (j > hi2) break;
                        y = in2[j];
                    }
                }
                while (i <= hi1) { output[k++] = in1[i++]; }
                while (j <= hi2) { output[k++] = in2[j++]; }
                Debug.Assert((k - lo) == (hi1 - lo1 + 1) + (hi2 - lo2 + 1));
            }


            /*----------------------------------------------------------------------------------------
              Parallel quicksort
            ----------------------------------------------------------------------------------------*/
            static void QuickSort<T>(T[] domain) where T : IComparable<T>
            {
                QuickSort(domain, 0, domain.Length - 1);
#if DEBUG
                if (!IsSorted(domain)) throw new Exception("sort failed");
#endif
            }

            private static void QuickSort<T>(T[] domain, int lo, int hi) where T : IComparable<T>
            {
                if (hi - lo + 1 <= SeqQuickThreshold)
                {
                    SeqQuickSort(domain, lo, hi);
                }
                else
                {
                    int pivot = Partition(domain, lo, hi);
                    Parallel.Invoke(
                      delegate { QuickSort(domain, lo, pivot - 1); },
                      delegate { QuickSort(domain, pivot + 1, hi); });
                }
            }

            // Sequential quicksort
            static void SeqQuickSort<T>(T[] domain) where T : IComparable<T>
            {
                SeqQuickSort(domain, 0, domain.Length - 1);
            }

            private static void SeqQuickSort<T>(T[] domain, int lo, int hi) where T : IComparable<T>
            {
                if (hi - lo + 1 <= InsertionThreshold)
                {
                    InsertionSort(domain, lo, hi);
                }
                else
                {
                    int pivot = Partition(domain, lo, hi);
                    SeqQuickSort(domain, lo, pivot - 1);
                    SeqQuickSort(domain, pivot + 1, hi);
                }
            }

            // Partition the input elements "domain[lo,hi]" and return the pivot index.
            // All elements before the pivot index are smaller than the pivot element,
            // while elements after the index are larger than or equal to the pivot element.
            private static int Partition<T>(T[] domain, int lo, int hi) where T : IComparable<T>
            {
                Debug.Assert(domain != null);
                Debug.Assert(domain.Length > hi);
                Debug.Assert(hi > lo);

                T pivot = domain[hi];
                int left = lo - 1;
                int right = hi;
                while (true)
                {
                    T r;
                    T l;
                    while (pivot.CompareTo((l = domain[++left])) > 0) { }
                    while (pivot.CompareTo((r = domain[--right])) < 0 && left < right) { }
                    if (left < right)
                    {
                        domain[right] = l;
                        domain[left] = r;
                    }
                    else
                    {
                        break;
                    }
                }
                domain[hi] = domain[left];
                domain[left] = pivot;
                return left;
            }

            /*----------------------------------------------------------------------------------------
               Insertion sort
            ----------------------------------------------------------------------------------------*/
            static void InsertionSort<T>(T[] domain) where T : IComparable<T>
            {
                Debug.Assert(domain != null);
                InsertionSort(domain, 0, domain.Length - 1);
            }

            private static void InsertionSort<T>(T[] domain, int lo, int hi) where T : IComparable<T>
            {
                Debug.Assert(domain != null);
                Debug.Assert(domain.Length > hi);

                for (int i = lo + 1; i <= hi; i++)
                {
                    T temp = domain[i];
                    int j = i - 1;
                    while (j >= lo && (domain[j].CompareTo(temp) > 0))
                    {
                        domain[j + 1] = domain[j];
                        j--;
                    }
                    domain[j + 1] = temp;
                }
            }
        }

        // -------------------------------------------------
        // Example of futures by summing the values in a tree
        // -------------------------------------------------
        class Tree
        {
            const int threshold = 13;
#if PFX_LEGACY_3_5
            const int treedepth = 20;
#else
            const int treedepth = 29;
#endif

            public static bool Test()
            {
                TestHarness.TestLog("* Tree.Test(treedepth={0})", treedepth);

                Tree t = Generate(treedepth);

                int s0 = t.SeqSum();
                int s1 = t.ParSum();

                if (s0 != s1)
                {
                    TestHarness.TestLog("  > failed: seq sum = {0}, but par sum = {1}", s0, s1);
                    return false;
                }

                return true;
            }

            // -------------------------------------------------
            // Sequential sum of the tree
            // -------------------------------------------------
            int SeqSum()
            {
                if (depth == 0)
                    return value;
                else
                    return value + left.SeqSum() + right.SeqSum();
            }


            // -------------------------------------------------
            // Parallel sum of the tree
            // -------------------------------------------------
            int ParSum()
            {
                if (depth < threshold) return SeqSum();

                Task<int> l = Task.Factory.StartNew<int>(left.ParSum);
                int r = right.ParSum();
                return (value + l.Result + r);
            }

            // -------------------------------------------------
            // Constructors and test generation
            // -------------------------------------------------
            int depth;
            int value;
            Tree left;
            Tree right;

            Tree(int d, int v, Tree l, Tree r)
            {
                depth = d;
                value = v;
                left = l;
                right = r;
            }

            Tree(int v) : this(0, v, null, null) { }

            static Tree Generate(int depth)
            {
                if (depth <= 0)
                    return new Tree(0);
                else
                {
                    Tree sub = Generate(depth - 1);
                    return new Tree(depth, depth, sub, sub);
                }
            }
        }

        // -------------------------------------------------
        // Example that shows how to use "Future"s to calculate fibonacci numbers.
        // -------------------------------------------------
        class Fib
        {
            const int threshold = 20;

            public static bool Test(int n)
            {
                TestHarness.TestLog("* Fib.Test(n={0})", n);

                int seq = SeqFib(n);
                int par = ParFib(n);

                if (seq != par)
                {
                    TestHarness.TestLog("  > error: seq fib {0} != par fib {1}  -- FUTURES", seq, par);
                    return false;
                }

                int par2 = ParDoFib(n);

                if (seq != par2)
                {
                    TestHarness.TestLog("  > error: seq fib {0} != par fib {1}  -- DO", seq, par2);
                    return false;
                }

                return true;
            }

            // -------------------------------------------------
            // Sequential fibonacci numbers (actually NFIB numbers)
            // -------------------------------------------------
            public static int SeqFib(int n)
            {
                if (n <= 1)
                    return n;
                else
                    return SeqFib(n - 1) + SeqFib(n - 2);
            }


            // -------------------------------------------------
            // Calculate fibonacci numbers in parallel. 
            // Calculation of fibonacci resembles a tree where each
            // recursive call can be done in parallel before adding
            // them up.
            //
            // In general futures can be stored as normal values.
            // For example, you could iterate over a tree and compute some
            // value for each node as a future and store it in that node.
            // A later phase could use those future values and all the computations
            // would be ran in parallel somewhere between the time of creation
            // and the later access of the value.
            // -------------------------------------------------
            public static int ParFib(int n)
            {
                // Below a certain threshold, we are better off calculating it sequentially
                if (n <= threshold) return SeqFib(n);

                // Otherwise, we create a future that calculates Fib(n-1). Idle threads may start doing this in parallel
                Task<int> f1 = Task<int>.Factory.StartNew(delegate { return ParFib(n - 1); });
                int f2 = ParFib(n - 2);  // and go on calculating Fib(n-2)
                return (f2 + f1.Result);          // by asking the Value, the future is forced to be evaluated by now
            }

            // A variation with Parallel.Invoke
            public static int ParDoFib(int n)
            {
                // Below a certain threshold, we are better off calculating it sequentially
                if (n <= threshold) return SeqFib(n);

                // Otherwise, we calculate Fib(n-1) and Fib(n-2) in parallel
                int f1 = 0, f2 = 0;
                Parallel.Invoke(delegate { f1 = ParDoFib(n - 1); }
                           , delegate { f2 = ParDoFib(n - 2); });
                return (f1 + f2);
            }

        }

    }


}
