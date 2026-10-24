using System;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SAGE.AddressablesUI
{
    // Completed callbacks also work on WebGL, where AsyncOperationHandle.Task is unavailable.
    internal static class AddressableOperation
    {
        internal static Task<T> Await<T>(AsyncOperationHandle<T> handle)
        {
            var completion = new TaskCompletionSource<T>();
            Action<AsyncOperationHandle<T>> finish = result =>
            {
                if (result.Status == AsyncOperationStatus.Succeeded)
                    completion.TrySetResult(result.Result);
                else
                    completion.TrySetException(result.OperationException ??
                        new InvalidOperationException("Addressables operation failed."));
            };
            if (handle.IsDone) finish(handle);
            else handle.Completed += finish;
            return completion.Task;
        }
    }
}
