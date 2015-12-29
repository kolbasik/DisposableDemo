using System;

namespace ConsoleAppDemo
{
    public class DisposableDemo
    {
        public static void Start()
        {
            Console.WriteLine("Test #1: dispose");
            new Generation2().Dispose();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            Console.WriteLine("Test #2: finalize");
            new Generation2();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            Console.WriteLine("Press any key...");
            Console.ReadKey(true);
        }

        public abstract class Disposable : IDisposable
        {
            protected bool IsDisposed { get; private set; }

            ~Disposable()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    DisposeCore(disposing);
                }
            }

            protected abstract void DisposeCore(bool disposing);
        }

        public class Generation1 : Disposable
        {
            ~Generation1()
            {
                Console.WriteLine("Generation1.Finalizer()");
            }

            protected override void DisposeCore(bool disposing)
            {
                Console.WriteLine("Generation1.Dispose()");
            }
        }

        public class Generation2 : Generation1
        {
            ~Generation2()
            {
                Console.WriteLine("Generation2.Finalizer()");
            }

            protected override void DisposeCore(bool disposing)
            {
                Console.WriteLine("Generation2.Dispose()");
                base.DisposeCore(disposing);
            }
        }
    }
}