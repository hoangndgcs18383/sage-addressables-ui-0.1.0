using UnityEngine;

namespace SAGE.AddressablesUI
{
    public interface IUIData { }
    public interface IBackScreen { void BackToScreen(); }
    public interface IBaseScreen
    {
        void Initialize();
        void SetData(IUIData data = null);
        void Show();
        void Hide();
        RectTransform RectTransform { get; set; }
    }

    public enum CanvasType { Main, Popup, Loading }

    [RequireComponent(typeof(RectTransform))]
    public class BaseScreen : MonoBehaviour, IBaseScreen, IBackScreen
    {
        private RectTransform rectTransform;
        internal UIManager Owner { get; set; }
        public RectTransform RectTransform
        {
            get => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());
            set => rectTransform = value;
        }

        public virtual void Initialize() { }
        public virtual void SetData(IUIData data = null) { }
        public virtual void Show()
        {
            Owner?.RegisterBackScreen(this);
            gameObject.SetActive(true);
        }
        public virtual void Hide()
        {
            Owner?.UnregisterBackScreen(this);
            gameObject.SetActive(false);
        }
        public virtual void BackToScreen() => Hide();
    }
}
