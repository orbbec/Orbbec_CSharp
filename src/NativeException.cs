using System;

namespace Orbbec
{
    public class NativeException : Exception
    {
        public NativeException(string errorMsg) : base(errorMsg)
        {

        }

        public static void HandleError(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return;

            using (Error error = new Error(ptr))
            {
                string message = error.GetMessage();
                string function = error.GetFunction();
                string args = error.GetArgs();
                ExceptionType type = error.GetExceptionType();
                string errorMsg = $"ErrorType: {type}\nErrorFun: {function}({args})\nErrorMsg: {message}";
                throw new NativeException(errorMsg);
            }
        }
    }
}