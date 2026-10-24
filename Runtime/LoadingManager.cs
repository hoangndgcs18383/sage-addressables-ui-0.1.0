using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SAGE.AddressablesUI
{
    /// <summary>Loads and activates scenes. The returned object owns additive scene unloading.</summary>
    public static class LoadingManager
    {
        public static async Task<AddressableScene> LoadSceneAsync(string address,
            LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Scene address is required.");
            var handle = Addressables.LoadSceneAsync(address, mode, true);
            try
            {
                await AddressableOperation.Await(handle);
                return new AddressableScene(handle);
            }
            catch
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
        }
    }

    public sealed class AddressableScene
    {
        private AsyncOperationHandle<SceneInstance> handle;
        private Task unloading;
        internal AddressableScene(AsyncOperationHandle<SceneInstance> handle) { this.handle = handle; }
        public bool IsLoaded => handle.IsValid() && handle.Result.Scene.isLoaded;
        public Scene Scene => handle.IsValid() ? handle.Result.Scene : default;

        public Task UnloadAsync()
        {
            if (unloading != null) return unloading;
            if (!IsLoaded) return Task.CompletedTask;
            unloading = Unload();
            return unloading;
        }
        private async Task Unload()
        {
            // The unload operation owns releasing the scene load handle.
            var operation = Addressables.UnloadSceneAsync(handle, false);
            try
            {
                await AddressableOperation.Await(operation);
                handle = default;
            }
            finally
            {
                if (operation.IsValid()) Addressables.Release(operation);
            }
        }
    }
}
