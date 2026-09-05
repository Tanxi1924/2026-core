using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace TwinBody
{
    /// <summary>Like Ladder: the map volume registers an available interaction with a character ability.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Corgi Engine/Environment/Drop Through Platform (Custom)")]
    public class DropThroughPlatform : CorgiMonoBehaviour
    {
        [Tooltip("Separate solid, horizontal platform collider. Do not assign the trigger itself.")]
        public BoxCollider2D Surface;
        private readonly Dictionary<Collider2D, TwinBodyDropThrough> _occupants =
            new Dictionary<Collider2D, TwinBodyDropThrough>();

        public bool Available => isActiveAndEnabled && Surface != null &&
            Surface.enabled && Surface.gameObject.activeInHierarchy && !Surface.isTrigger;

        private void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }

        private void OnTriggerEnter2D(Collider2D other) { Register(other); }
        private void OnTriggerStay2D(Collider2D other) { Register(other); }

        private void Register(Collider2D other)
        {
            if (!Available || other.attachedRigidbody == null || other.isTrigger) return;
            var ability = other.attachedRigidbody.GetComponent<TwinBodyDropThrough>();
            if (ability == null || !ability.isActiveAndEnabled) return;
            _occupants[other] = ability;
            ability.RegisterPlatform(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_occupants.TryGetValue(other, out var ability)) return;
            _occupants.Remove(other);
            // One head leaving must not unregister the other head or the connector.
            foreach (var occupant in _occupants.Values)
                if (occupant == ability) return;
            if (ability != null) ability.UnregisterPlatform(this);
        }

        private void OnDisable()
        {
            foreach (var ability in _occupants.Values)
                if (ability != null) ability.UnregisterPlatform(this);
            _occupants.Clear();
        }
    }
}
