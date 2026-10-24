using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SAGE.AddressablesUI
{
    /// <summary>Owns one asset handle per label. Call all APIs from Unity's main thread.</summary>
    public sealed class AddressableManager : IDisposable
    {
        private sealed class Entry
        {
            internal AsyncOperationHandle<IList<GameObject>> Handle;
            internal Task<IList<GameObject>> Task;
            internal bool ReleaseRequested;
            internal bool Released;
        }

        public static AddressableManager Instance { get; } = new AddressableManager();
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        public Task<IList<GameObject>> LoadAllAssetByLabelAsync(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label is required.", nameof(label));
            if (entries.TryGetValue(label, out var cached)) return cached.Task;
            var entry = new Entry { Handle = Addressables.LoadAssetsAsync<GameObject>(label, null) };
            entries.Add(label, entry);
            entry.Task = Complete(label, entry);
            return entry.Task;
        }

        private async Task<IList<GameObject>> Complete(string label, Entry entry)
        {
            try
            {
                var result = await AddressableOperation.Await(entry.Handle);
                if (entry.ReleaseRequested) throw new OperationCanceledException("Label was unloaded: " + label);
                return result;
            }
            catch
            {
                if (entries.TryGetValue(label, out var current) && ReferenceEquals(current, entry))
                    entries.Remove(label);
                Release(entry);
                throw;
            }
            finally
            {
                if (entry.ReleaseRequested) Release(entry);
            }
        }

        public IEnumerator IELoadAllAssetByLabel(string label)
        {
            var task = LoadAllAssetByLabelAsync(label);
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }

        public void UnloadAllAssetByLabel(string label)
        {
            if (!entries.TryGetValue(label, out var entry)) return;
            entries.Remove(label);
            entry.ReleaseRequested = true;
            // IsDone may be true before the Completed callback has run. Keep the
            // handle alive until our awaiter has actually consumed its result.
            if (entry.Task.IsCompleted) Release(entry);
        }

        public void UnloadAllAssets()
        {
            foreach (var entry in entries.Values)
            {
                entry.ReleaseRequested = true;
                if (entry.Task.IsCompleted) Release(entry);
            }
            entries.Clear();
        }

        private static void Release(Entry entry)
        {
            if (entry.Released) return;
            entry.Released = true;
            if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
        }

        // Dispose releases this owner's labels; the owner may subsequently be reused.
        public void Dispose() => UnloadAllAssets();
    }
}
