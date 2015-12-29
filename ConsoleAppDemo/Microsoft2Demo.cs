using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ConsoleAppDemo
{
    public class Microsoft2Demo
    {
        public static void Start()
        {
            Run();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }

        private static void Run()
        {
            var tracking = new List<object>();

            // Dispose is not called, Finalize will be called later.
            using (null)
            {
                Console.WriteLine("\nDisposal Scenario: #1\n");
                var d3 = new Derived("d1", tracking);
            }

            // Dispose is implicitly called in the scope of the using statement.
            using (var d1 = new Derived("d2", tracking))
            {
                Console.WriteLine("\nDisposal Scenario: #2\n");
            }

            // Dispose is explicitly called.
            using (null)
            {
                Console.WriteLine("\nDisposal Scenario: #3\n");
                var d2 = new Derived("d3", tracking);
                d2.Dispose();
            }

            // Again, Dispose is not called, Finalize will be called later.
            using (null)
            {
                Console.WriteLine("\nDisposal Scenario: #4\n");
                var d4 = new Derived("d4", tracking);
            }

            // List the objects remaining to dispose.
            Console.WriteLine("\nObjects remaining to dispose = {0:d}", tracking.Count);
            foreach (Derived dd in tracking)
            {
                Console.WriteLine("    Reference Object: {0:s}, {1:x16}", dd.InstanceName, dd.GetHashCode());
            }

            // Queued finalizers will be exeucted when Main() goes out of scope.
            Console.WriteLine("\nDequeueing finalizers...");
        }

        /// <summary>
        /// <see cref="https://msdn.microsoft.com/en-us/library/b1yfkh5e%28v=vs.100%29.aspx?f=255&MSPPError=-2147217396"/>
        /// </summary>
        /// <seealso cref="System.IDisposable" />
        public abstract class Disposable : IDisposable
        {
            protected bool IsDisposed { get; private set; }

            /* NOTE: [Finalize]
                1. Implement Finalize only on objects that require finalization. There are performance costs associated with Finalize methods.
                3. Do not make the Finalize method more visible. It should be protected, not public.
                5. Do not directly call a Finalize method on an object other than the object's base class. This is not a valid operation in the C# programming language. */
            ~Disposable()
            {
                Console.WriteLine("Disposable.Finalize()");
                Dispose(false); // NOTE: [Finalize] 4. An object's Finalize method should free any external resources that the object owns. Moreover, a Finalize method should release only resources that the object has held onto. The Finalize method should not reference any other objects.
            }

            /* NOTE: [Dispose]
                1. Implement the dispose design pattern on a type that encapsulates resources that explicitly need to be freed. Users can free external resources by calling the public Dispose method.
                2. Implement the dispose design pattern on a base type that commonly has derived types that hold onto resources, even if the base type does not. If the base type has a Close method, often this indicates the need to implement Dispose. In such cases, do not implement a Finalize method on the base type. Finalize should be implemented in any derived types that introduce resources that require cleanup.
                   PS: author's opinion: the following is a micro optimization: "Finalize should be implemented in any derived types that introduce resources that require cleanup."
            */
            public void Dispose()
            {
                Console.WriteLine("Disposable.Dispose()");
                // NOTE: [Dispose] 6. Do not assume that Dispose will be called. Unmanaged resources owned by a type should also be released in a Finalize method in the event that Dispose is not called.
                //                    PS: This is most important. Do not trust anyone )))
                Dispose(true);
                // NOTE: [Finalize] 2. If you require a Finalize method, consider implementing IDisposable to allow users of your class to avoid the cost of invoking the Finalize method.
                // NOTE: [Dispose] 4. After Dispose has been called on an instance, prevent the Finalize method from running by calling the GC.SuppressFinalize. The exception to this rule is the rare situation in which work must be done in Finalize that is not covered by Dispose.
                GC.SuppressFinalize(this);
            }

            protected void ValidateAvailabilityComponents(string message)
            {
                if (IsDisposed)
                {
                    // NOTE: [Dispose] 7. Throw an ObjectDisposedException from instance methods on this type (other than Dispose) when resources are already disposed. This rule does not apply to the Dispose method because it should be callable multiple times without throwing an exception.
                    // NOTE: [Dispose] 9. Consider not allowing an object to be usable after its Dispose method has been called. Re-creating an object that has already been disposed is a difficult pattern to implement.
                    throw new ObjectDisposedException(message);
                }
            }

            private void Dispose(bool disposing)
            {
                // NOTE: [Dispose] 10. Allow a Dispose method to be called more than once without throwing an exception. The method should do nothing after the first call.
                if (!IsDisposed)
                {
                    IsDisposed = true;
                    DisposeCore(disposing);
                }
            }

            /* NOTE: [Dispose] 
                3. Free any disposable resources a type owns in its Dispose method.
                5. Call the base class's Dispose method if it implements IDisposable.
                8. Propagate the calls to Dispose through the hierarchy of base types. The Dispose method should free all resources held by this object and any object owned by this object. For example, you can create an object such as a TextReader that holds onto a Stream and an Encoding, both of which are created by the TextReader without the user's knowledge. Furthermore, both the Stream and the Encoding can acquire external resources. When you call the Dispose method on the TextReader, it should in turn call Dispose on the Stream and the Encoding, causing them to release their external resources.
            */
            protected abstract void DisposeCore(bool disposing);
        }

        public class Base : Disposable
        {
            public Base(string instanceName, List<object> tracking)
            {
                InstanceName = instanceName;
                Tracking = tracking;
                Tracking.Add(this);
            }

            public string InstanceName { get; }

            public List<object> Tracking { get; }

            ~Base()
            {
                Console.WriteLine("[{0}].Base.Finalize()", InstanceName); // NOTE: just to trace the release flow. it's not necessary in real life
            }

            protected override void DisposeCore(bool disposing)
            {
                Console.WriteLine("[{0}].Base.Dispose({1})", InstanceName, disposing);
                if (disposing)
                {
                    // Release managed resources.
                    Tracking.Remove(this);
                    Console.WriteLine("[{0}] Removed from tracking list: {1:x16}", InstanceName, GetHashCode());
                }
                // Release unmanaged resources.
            }
        }

        public class Derived : Base
        {
            private IntPtr umResource;

            public Derived(string instanceName, List<object> tracking) : base(instanceName, tracking)
            {
                umResource = Marshal.StringToCoTaskMemAuto(instanceName);
            }

            ~Derived()
            {
                Console.WriteLine("\n[{0}].Derived.Finalize()", InstanceName); // NOTE: just to trace the release flow. it's not necessary in real life
            }

            protected override void DisposeCore(bool disposing)
            {
                Console.WriteLine("[{0}].Derived.Dispose({1})", InstanceName, disposing);
                if (disposing)
                {
                    // Release managed resources.
                }
                // Release unmanaged resources.
                if (umResource != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(umResource);
                    Console.WriteLine("[{0}] Unmanaged memory freed at {1:x16}", InstanceName, umResource.ToInt64());
                    umResource = IntPtr.Zero;
                }
                base.DisposeCore(disposing);
            }
        }
    }
}