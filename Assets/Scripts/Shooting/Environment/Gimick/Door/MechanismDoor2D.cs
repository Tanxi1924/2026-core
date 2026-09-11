using UnityEngine;
using UnityEngine.Events;

namespace TwinBody.Mechanisms
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Twin Body/Mechanisms/Mechanism Door 2D")]
    public sealed class MechanismDoor2D : MonoBehaviour
    {
        [Header("初始状态")]
        public bool StartOpen = false;
        [Header("门的实体碰撞体（必须指定，不包含按钮）")]
        public Collider2D[] Blockers = new Collider2D[0];
        [Header("开门表现")]
        public bool HideSpritesWhenOpen = true;
        public SpriteRenderer[] DoorSprites = new SpriteRenderer[0];
        [Header("反馈事件（可接音效、粒子或 MMFeedbacks）")]
        public UnityEvent OnOpened = new UnityEvent();
        public UnityEvent OnClosed = new UnityEvent();

        public bool IsOpen { get; private set; }
        private bool initialized;

        private void Reset()
        {
            Blockers = GetComponents<Collider2D>();
            DoorSprites = GetComponents<SpriteRenderer>();
        }

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            IsOpen = StartOpen;
            ApplyState();
            if (Blockers == null || Blockers.Length == 0)
                Debug.LogWarning("MechanismDoor2D: 请指定门的实体 Collider2D。", this);
        }

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        public void SetOpen(bool open)
        {
            Initialize();
            if (IsOpen == open) return;
            IsOpen = open;
            ApplyState();
            if (open) OnOpened.Invoke();
            else OnClosed.Invoke();
        }

        private void ApplyState()
        {
            if (Blockers != null)
                foreach (Collider2D blocker in Blockers)
                    if (blocker != null) blocker.enabled = !IsOpen;
            if (HideSpritesWhenOpen && DoorSprites != null)
                foreach (SpriteRenderer sprite in DoorSprites)
                    if (sprite != null) sprite.enabled = !IsOpen;
        }
    }
}
