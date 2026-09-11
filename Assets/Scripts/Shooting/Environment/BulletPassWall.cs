using UnityEngine;

namespace TwinBody.Environment
{
    /// <summary>Unity 6: per-wall filtering for physical 2D projectiles.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Twin Body/Environment/Bullet Pass Wall")]
    public sealed class BulletPassWall : MonoBehaviour
    {
        [Header("子弹通行开关")]
        [SerializeField, InspectorName("允许 Twin 子弹通过")]
        private bool allowTwinBullets = true;
        [SerializeField, InspectorName("允许敌人子弹通过")]
        private bool allowEnemyBullets;

        [Header("子弹所在 Layer")]
        [ InspectorName("Twin 子弹层")]
        private LayerMask twinBulletLayers = 1 << 16;
        [ InspectorName("敌人子弹层")]
        private LayerMask enemyBulletLayers = 1 << 12;

        //碰撞过滤
        private int overridePriority = 100;

        private Collider2D[] wallColliders;
        private int[] originalExclusions;
        private int[] originalPriorities;

        public bool AllowTwinBullets => allowTwinBullets;
        public bool AllowEnemyBullets => allowEnemyBullets;

        private void Reset()
        {
            twinBulletLayers = LayerMask.GetMask("PlayerProjectiles");
            enemyBulletLayers = LayerMask.GetMask("Projectiles");
        }

        private void OnEnable()
        {
            // Only this object: each wall collider object owns its own policy.
            wallColliders = GetComponents<Collider2D>();
            originalExclusions = new int[wallColliders.Length];
            originalPriorities = new int[wallColliders.Length];
            for (int i = 0; i < wallColliders.Length; i++)
            {
                originalExclusions[i] = wallColliders[i].excludeLayers.value;
                originalPriorities[i] = wallColliders[i].layerOverridePriority;
            }
            ApplySettings();
        }

        // Supports changing serialized toggles in the Inspector during Play Mode.
        // Avoid physics API calls from OnValidate (which may run off the main thread).
        private void Update() => ApplySettings();

        public void SetTwinBulletsPass(bool value)
        {
            allowTwinBullets = value;
            ApplySettings();
        }

        public void SetEnemyBulletsPass(bool value)
        {
            allowEnemyBullets = value;
            ApplySettings();
        }

        public void ToggleTwinBulletsPass() => SetTwinBulletsPass(!allowTwinBullets);
        public void ToggleEnemyBulletsPass() => SetEnemyBulletsPass(!allowEnemyBullets);

        public void ApplySettings()
        {
            if (!isActiveAndEnabled || wallColliders == null) return;
            int exclusions = (allowTwinBullets ? twinBulletLayers.value : 0)
                           | (allowEnemyBullets ? enemyBulletLayers.value : 0);
            for (int i = 0; i < wallColliders.Length; i++)
            {
                Collider2D wall = wallColliders[i];
                if (wall == null) continue;
                int mask = originalExclusions[i] | exclusions;
                int priority = exclusions == 0 ? originalPriorities[i]
                    : Mathf.Max(originalPriorities[i], overridePriority);
                if (wall.excludeLayers.value != mask) wall.excludeLayers = mask;
                if (wall.layerOverridePriority != priority) wall.layerOverridePriority = priority;
            }
        }

        private void OnDisable()
        {
            if (wallColliders == null) return;
            for (int i = 0; i < wallColliders.Length; i++)
            {
                if (wallColliders[i] == null) continue;
                wallColliders[i].excludeLayers = originalExclusions[i];
                wallColliders[i].layerOverridePriority = originalPriorities[i];
            }
            wallColliders = null;
        }
    }
}
