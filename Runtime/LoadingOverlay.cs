using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SAGE.AddressablesUI
{
    /// <summary>Unscaled loading blocker and fade. Can be replaced with a custom CanvasGroup.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private float fadeDuration = 0.25f;
        private Coroutine delayedShow;
        private TaskCompletionSource<bool> fade;
        private int revision;

        internal static LoadingOverlay Create(RectTransform parent)
        {
            var go = new GameObject("Loading Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            return go.AddComponent<LoadingOverlay>();
        }
        private void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            Hide();
        }
        private void Supersede()
        {
            revision++;
            if (delayedShow != null) StopCoroutine(delayedShow);
            delayedShow = null;
            fade?.TrySetCanceled();
            fade = null;
        }
        public void Show(float delay = 3f)
        {
            Supersede();
            group.alpha = 0;
            group.blocksRaycasts = true;
            transform.SetAsLastSibling();
            delayedShow = StartCoroutine(ShowAfter(Mathf.Max(0, delay)));
        }
        private IEnumerator ShowAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            group.alpha = 1;
            delayedShow = null;
        }
        public void Hide()
        {
            Supersede();
            group.alpha = 0;
            group.blocksRaycasts = false;
        }
        public Task FadeAsync(float alpha)
        {
            Supersede();
            fade = new TaskCompletionSource<bool>();
            var completion = fade;
            var current = revision;
            transform.SetAsLastSibling();
            StartCoroutine(Fade(Mathf.Clamp01(alpha), current, completion));
            return completion.Task;
        }
        private IEnumerator Fade(float alpha, int current, TaskCompletionSource<bool> completion)
        {
            var from = group.alpha;
            group.blocksRaycasts = true;
            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (revision != current) yield break;
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, alpha, fadeDuration <= 0 ? 1 : elapsed / fadeDuration);
                yield return null;
            }
            if (revision != current) yield break;
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0;
            fade = null;
            completion.TrySetResult(true);
        }
        private void OnDisable()
        {
            Supersede();
            if (group == null) return;
            group.alpha = 0;
            group.blocksRaycasts = false;
        }
    }
}
