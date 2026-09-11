using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEngine;

[AddComponentMenu("Corgi Engine/Weapons/Matrix Barrage Weapon")]
public class MatrixBarrageWeapon : ProjectileWeapon
{
    [Header("Matrix Barrage")]
    public MatrixBarragePattern Pattern;
    [Tooltip("Optional center anchor. Uses this weapon's position when empty.")]
    public Transform MatrixAnchor;
    [Tooltip("Offset from anchor in world XY. Does not change when the enemy flips.")]
    public Vector2 WorldCenterOffset = new Vector2(0f, 2f);
    [Tooltip("World-space movement direction: 0 = right, 90 = up, 180 = left.")]
    public float DirectionAngle = 90f;
    public enum FlightModes { Parallel, Radial }
    public FlightModes FlightMode = FlightModes.Parallel;
    [Tooltip("Override stock Projectile.Speed. Stock Corgi moves at Speed / 10.")]
    public bool OverrideSpeed = true;
    [Min(0f)] public float SpeedUnitsPerSecond = 6f;
    [Tooltip("Zero acceleration preserves equal spacing for parallel bullets.")]
    public bool ForceZeroAcceleration = true;
    [Min(0.01f), Tooltip("Visual guide only; does not resize bullet colliders.")]
    public float PreviewRadius = 0.12f;
    [Min(0.01f)] public float PreviewArrowLength = 0.7f;

    private readonly List<Vector3> _shotOffsets = new List<Vector3>();
    private readonly List<Vector3> _previewOffsets = new List<Vector3>();
    private Vector3 _shotDirection;
    private FlightModes _shotFlightMode;
    private bool _poolWarning;

    public Vector3 MatrixCenter => (MatrixAnchor != null ? MatrixAnchor.position : transform.position)
        + new Vector3(WorldCenterOffset.x, WorldCenterOffset.y, 0f);
    public Vector3 ParallelDirection => Quaternion.Euler(0f, 0f, DirectionAngle) * Vector3.right;

    // All matrix positions and direction are world-space and immune to character flipping.
    public override void DetermineSpawnPosition() { SpawnPosition = MatrixCenter; }

    protected override void WeaponUse()
    {
        if (Pattern == null || ObjectPooler == null) return;
        Pattern.GetOffsets(_shotOffsets);
        if (_shotOffsets.Count == 0) return;
        _shotDirection = ParallelDirection;
        _shotFlightMode = FlightMode;
        // Delegate weapon feedback, recoil and the spawn loop to Corgi.
        int previousCount = ProjectilesPerShot;
        ProjectilesPerShot = _shotOffsets.Count;
        try { base.WeaponUse(); }
        finally { ProjectilesPerShot = previousCount; }
    }

    public override GameObject SpawnProjectile(Vector3 spawnPosition, int projectileIndex,
        int totalProjectiles, bool triggerObjectActivation = true)
    {
        if (ObjectPooler == null || projectileIndex < 0 || projectileIndex >= _shotOffsets.Count) return null;
        GameObject bulletObject = ObjectPooler.GetPooledGameObject();
        if (bulletObject == null)
        {
            if (!_poolWarning)
            {
                Debug.LogWarning("Matrix barrage pool exhausted: increase Pool Size or enable Pool Can Expand.", this);
                _poolWarning = true;
            }
            return null;
        }
        Projectile bullet = bulletObject.GetComponent<Projectile>();
        if (bullet == null)
        {
            Debug.LogError("Matrix barrage requires a Corgi Projectile on the pooled object's root.", this);
            return null;
        }
        Vector3 offset = _shotOffsets[projectileIndex];
        Vector3 direction = _shotFlightMode == FlightModes.Radial && offset.sqrMagnitude > 0.000001f
            ? offset.normalized : _shotDirection;
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        bulletObject.transform.SetPositionAndRotation(spawnPosition + offset, rotation);
        bullet.SetWeapon(this);
        if (Owner != null) bullet.SetOwner(Owner.gameObject);
        // Stock Projectile initializes and resets Speed in OnEnable.
        bulletObject.SetActive(true);
        if (!bulletObject.activeInHierarchy) return null; // e.g. spawn security rejected this cell
        bullet.DirectionCanBeChangedBySpawner = true;
        bullet.SetDirection(direction, rotation, true);
        if (OverrideSpeed) bullet.Speed = Mathf.Max(0f, SpeedUnitsPerSecond) * 10f;
        if (ForceZeroAcceleration) bullet.Acceleration = 0f;
        if (triggerObjectActivation) bullet.TriggerOnSpawnComplete();
        return bulletObject;
    }

    protected override void OnDrawGizmosSelected()
    {
        if (Pattern == null) return;
        Pattern.GetOffsets(_previewOffsets);
        Color oldColor = Gizmos.color;
        Gizmos.color = Color.cyan;
        foreach (Vector3 offset in _previewOffsets)
        {
            Vector3 position = MatrixCenter + offset;
            Vector3 direction = FlightMode == FlightModes.Radial && offset.sqrMagnitude > 0.000001f
                ? offset.normalized : ParallelDirection;
            Vector3 end = position + direction * PreviewArrowLength;
            Vector3 side = new Vector3(-direction.y, direction.x, 0f);
            Gizmos.DrawWireSphere(position, PreviewRadius);
            Gizmos.DrawLine(position, end);
            Gizmos.DrawLine(end, end - direction * 0.15f + side * 0.08f);
            Gizmos.DrawLine(end, end - direction * 0.15f - side * 0.08f);
        }
        Gizmos.color = oldColor;
    }
}

