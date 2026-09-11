using UnityEngine;
using UnityEngine.Events;

namespace TwinBody.Mechanisms
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    [AddComponentMenu("Twin Body/Mechanisms/Mechanism Button 2D")]
    public sealed class MechanismButton2D : MonoBehaviour
    {
        [Header("允许触发的对象（可多选）")]
        public bool AllowPlayerBullets = true;
        public bool AllowPlayerContact = false;
        public bool AllowEnemyBullets = false;

        [Header("控制目标")]
        public MechanismDoor2D TargetDoor;
        public bool ButtonEnabled = true;

        [Header("识别层（填写实际碰撞体的 Layer）")]
        public LayerMask PlayerBulletLayers = 1 << 16;
        public LayerMask PlayerLayers = 1 << 9;
        public LayerMask EnemyBulletLayers = 1 << 12;

        [Header("按钮表现（可不填）")]
        public SpriteRenderer ButtonSprite;
        public Color PressedColor = Color.green;
        public UnityEvent OnPressed = new UnityEvent();

        public bool IsPressed { get; private set; }
        private Color originalColor;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            PlayerBulletLayers = LayerMask.GetMask("PlayerProjectiles");
            PlayerLayers = LayerMask.GetMask("Player");
            EnemyBulletLayers = LayerMask.GetMask("Projectiles");
            int volumeLayer = LayerMask.NameToLayer("Volumes");
            if (volumeLayer >= 0) gameObject.layer = volumeLayer;
            ButtonSprite = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (ButtonSprite != null) originalColor = ButtonSprite.color;
            if (!GetComponent<BoxCollider2D>().isTrigger)
                Debug.LogWarning("MechanismButton2D: 请勾选 BoxCollider2D.Is Trigger。", this);
        }

        private void OnTriggerEnter2D(Collider2D other) => TryPress(other);

        // Also handles enabling a permission while a character is already inside.
        private void OnTriggerStay2D(Collider2D other) => TryPress(other);

        private bool AcceptsLayer(int layer)
        {
            int bit = 1 << layer;
            return (AllowPlayerBullets && (PlayerBulletLayers.value & bit) != 0)
                || (AllowPlayerContact && (PlayerLayers.value & bit) != 0)
                || (AllowEnemyBullets && (EnemyBulletLayers.value & bit) != 0);
        }

        private void TryPress(Collider2D other)
        {
            if (!isActiveAndEnabled || !ButtonEnabled || IsPressed || other == null) return;
            if (TargetDoor == null || !TargetDoor.isActiveAndEnabled) return;
            if (!AcceptsLayer(other.gameObject.layer)) return;
            // Latch BEFORE callbacks: Twin's multiple colliders and a whole barrage
            // must not trigger the button more than once.
            IsPressed = true;
            if (ButtonSprite != null) ButtonSprite.color = PressedColor;
            TargetDoor.Open();
            OnPressed.Invoke();
        }

        public void SetButtonEnabled(bool value) => ButtonEnabled = value;
        public void SetAllowPlayerBullets(bool value) => AllowPlayerBullets = value;
        public void SetAllowPlayerContact(bool value) => AllowPlayerContact = value;
        public void SetAllowEnemyBullets(bool value) => AllowEnemyBullets = value;

        /// <summary>Explicit level reset. Ensure the doorway is clear before closing.</summary>
        public void ResetButtonAndCloseDoor()
        {
            IsPressed = false;
            if (ButtonSprite != null) ButtonSprite.color = originalColor;
            if (TargetDoor != null) TargetDoor.Close();
        }
    }
}
