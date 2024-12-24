using System;

namespace Orbbec
{
    public delegate void ReleaseAction(IntPtr ptr);

    /*
     * \internal
     */
    public sealed class NativeHandle : IDisposable
    {
        private readonly ReleaseAction _action;
        private int _referenceCount;
        private bool _disposed;

        public IntPtr Ptr { get; private set; }
        public bool IsValid => Ptr != IntPtr.Zero;

        public NativeHandle(IntPtr ptr, ReleaseAction releaseAction)
        {
            ThrowIfNull(ptr);

            Ptr = ptr;
            _referenceCount = 1;
            _action = releaseAction;
            _disposed = false;
        }

        public int GetReferenceCount()
        {
            return _referenceCount;
        }

        private void ThrowIfNull(IntPtr ptr)
        {
            if(ptr == IntPtr.Zero)
            {
                throw new NullReferenceException("Handle is null");
            }
        }

        private void ThrowIfInvalid()
        {
            if (IsValid)
                return;

            throw new NullReferenceException("NativeHandle has previously been released");
        }

        public void Retain()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NativeHandle));
            }

            System.Threading.Interlocked.Increment(ref _referenceCount);
        }

        public void Release()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NativeHandle));
            }


            int newCount = System.Threading.Interlocked.Decrement(ref _referenceCount);
            if (newCount > 0)
                return;

            ThrowIfInvalid();
            _action?.Invoke(Ptr);
            Ptr = IntPtr.Zero; 
            _disposed = true; 
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources if any
            }

            Release();
            _disposed = true; 
        }

        ~NativeHandle()
        {
            Dispose(false);
        }
    }
}
