using OctagonFighter.Domain.Health;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Combat
{
    /// <summary>
    /// Marks a trigger collider as a discrete body region so combat queries can derive the
    /// resolved hit region from the collided volume instead of the attack's authored name.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class HitRegionVolume : MonoBehaviour
    {
        [SerializeField] private HitRegion _region = HitRegion.Body;

        /// <summary>
        /// Gets the body region this volume represents.
        /// </summary>
        public HitRegion Region => _region;

        /// <summary>
        /// Sets the region. Used by editors and tests that build volumes programmatically.
        /// </summary>
        public void SetRegion(HitRegion region)
        {
            _region = region;
        }
    }
}
