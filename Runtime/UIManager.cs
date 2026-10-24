using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SAGE.AddressablesUI
{
    public sealed class UIManager : MonoBehaviour
    {
        private sealed class ScreenEntry
        {
            internal BaseScreen Screen;
            internal AsyncOperationHandle<GameObject> Handle;
        }

        public static UIManager Instance { get; private set; }
        [SerializeField] private RectTransform mainCanvas;
        [SerializeField] private RectTransform popupCanvas;
        [SerializeField] private RectTransform loadingCanvas;
        [SerializeField] private LoadingOverlay loadingOverlay;
        [SerializeField] private Camera uiCamera = null;
        public Camera UICamera => uiCamera;
        public AddressableManager Assets { get; } = new AddressableManager();
        public Func<string, string> AddressResolver { get; set; } = key => key;

        private readonly Dictionary<string, ScreenEntry> screens = new Dictionary<string, ScreenEntry>();
        private readonly Dictionary<string, Task<BaseScreen>> pending = new Dictionary<string, Task<BaseScreen>>();
        private readonly List<IBackScreen> backScreens = new List<IBackScreen>();
        private bool disposed;
        private int loadingRequests;

        /// <summary>Creates a persistent manager, Canvas layers and loading overlay if needed.</summary>
        public static UIManager Create()
        {
            if (Instance != null) return Instance;
            return new GameObject("SAGE UIManager").AddComponent<UIManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            if (mainCanvas == null) mainCanvas = CreateLayer("Main", 0);
            if (popupCanvas == null) popupCanvas = CreateLayer("Popup", 100);
            if (loadingCanvas == null) loadingCanvas = CreateLayer("Loading", 200);
            if (loadingOverlay == null) loadingOverlay = LoadingOverlay.Create(loadingCanvas);
        }

        private RectTransform CreateLayer(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        private RectTransform GetParent(CanvasType type)
        {
            switch (type)
            {
                case CanvasType.Popup: return popupCanvas;
                case CanvasType.Loading: return loadingCanvas;
                default: return mainCanvas;
            }
        }

        public async Task DoInitializeAsync(string screenLabel = "BaseScreen")
        {
            EnsureAlive();
            await Assets.LoadAllAssetByLabelAsync(screenLabel);
        }

        public T GetScreen<T>(string screenName) where T : class, IBaseScreen =>
            screens.TryGetValue(screenName, out var entry) ? entry.Screen as T : null;

        public async Task<T> ShowAndLoadScreenAsync<T>(string screenName, IUIData data = null,
            CanvasType parent = CanvasType.Main, Action<T> callback = null) where T : class, IBaseScreen
        {
            EnsureAlive();
            if (string.IsNullOrWhiteSpace(screenName)) throw new ArgumentException("Screen name is required.");
            loadingRequests++;
            if (loadingRequests == 1) ShowCircleLoading();
            try
            {
                BaseScreen screen;
                if (screens.TryGetValue(screenName, out var entry)) screen = entry.Screen;
                else
                {
                    if (!pending.TryGetValue(screenName, out var task))
                    {
                        var completion = new TaskCompletionSource<BaseScreen>();
                        task = completion.Task;
                        pending.Add(screenName, task);
                        CreateScreen(screenName, parent, completion);
                    }
                    screen = await task;
                }
                EnsureAlive();
                var typed = screen as T;
                if (typed == null) throw new InvalidOperationException("Screen has a different component type: " + screenName);
                var rect = screen.RectTransform;
                rect.SetParent(GetParent(parent), false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();
                screen.SetData(data);
                screen.Show();
                callback?.Invoke(typed);
                return typed;
            }
            finally
            {
                loadingRequests--;
                if (!disposed && loadingRequests == 0) HideCircleLoading();
            }
        }

        private async void CreateScreen(string key, CanvasType parent, TaskCompletionSource<BaseScreen> completion)
        {
            AsyncOperationHandle<GameObject> handle = default;
            var instantiated = false;
            BaseScreen createdScreen = null;
            try
            {
                var address = AddressResolver(key);
                if (string.IsNullOrWhiteSpace(address)) throw new InvalidOperationException("No address for: " + key);
                handle = Addressables.InstantiateAsync(address, GetParent(parent), false);
                var go = await AddressableOperation.Await(handle);
                instantiated = true;
                EnsureAlive();
                var screen = go.GetComponent<BaseScreen>();
                if (screen == null) throw new InvalidOperationException("Prefab needs BaseScreen on its root: " + key);
                createdScreen = screen;
                go.SetActive(false);
                screen.Owner = this;
                screen.Initialize();
                EnsureAlive();
                screens.Add(key, new ScreenEntry { Screen = screen, Handle = handle });
                pending.Remove(key);
                completion.TrySetResult(screen);
            }
            catch (Exception error)
            {
                if (createdScreen != null) UnregisterBackScreen(createdScreen);
                if (handle.IsValid())
                {
                    if (instantiated) Addressables.ReleaseInstance(handle);
                    else Addressables.Release(handle);
                }
                pending.Remove(key);
                completion.TrySetException(error);
            }
        }

        public async void ShowAndLoadScreen<T>(string screenName, IUIData data = null,
            CanvasType parent = CanvasType.Main, Action<T> callback = null) where T : class, IBaseScreen
        {
            try { await ShowAndLoadScreenAsync(screenName, data, parent, callback); }
            catch (Exception error) { Debug.LogException(error); }
        }

        public void RegisterBackScreen(IBackScreen screen)
        {
            UnregisterBackScreen(screen);
            backScreens.Add(screen);
        }
        public void UnregisterBackScreen(IBackScreen screen) => backScreens.Remove(screen);
        public void Back()
        {
            if (backScreens.Count == 0) return;
            var screen = backScreens[backScreens.Count - 1];
            backScreens.RemoveAt(backScreens.Count - 1);
            screen.BackToScreen();
        }
        public void HideAllPreviousScreens()
        {
            while (backScreens.Count > 0) Back();
        }
        public void HideScreen(string key)
        {
            if (screens.TryGetValue(key, out var entry)) entry.Screen.Hide();
        }
        public bool UnloadScreen(string key)
        {
            if (pending.ContainsKey(key)) throw new InvalidOperationException("Screen is still loading: " + key);
            if (!screens.TryGetValue(key, out var entry)) return false;
            screens.Remove(key);
            UnregisterBackScreen(entry.Screen);
            if (entry.Handle.IsValid()) Addressables.ReleaseInstance(entry.Handle);
            return true;
        }

        public void ShowCircleLoading(float delayShow = 3f) => loadingOverlay.Show(delayShow);
        public void HideCircleLoading() => loadingOverlay.Hide();
        public Task FadeInLoadingAsync() => loadingOverlay.FadeAsync(1f);
        public Task FadeOutLoadingAsync() => loadingOverlay.FadeAsync(0f);

        private void EnsureAlive()
        {
            if (disposed) throw new ObjectDisposedException(nameof(UIManager));
        }
        private void OnDestroy()
        {
            disposed = true;
            foreach (var entry in screens.Values)
                if (entry.Handle.IsValid()) Addressables.ReleaseInstance(entry.Handle);
            screens.Clear();
            backScreens.Clear();
            Assets.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
