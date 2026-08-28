using UnityEngine;

namespace UI
{
    public enum UILayer
    {
        Background,
        Main,
        HUD,
        Popup,
        Overlay
    }

    public abstract class UIPanel : MonoBehaviour
    {
        public UILayer Layer;

        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            OnHide();
            gameObject.SetActive(false);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }
}
