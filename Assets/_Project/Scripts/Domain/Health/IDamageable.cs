namespace OctagonFighter.Domain.Health
{
    /// <summary>
    /// Receives resolved damage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies resolved damage and returns the resulting state.
        /// </summary>
        DamageResult ApplyDamage(in DamageEvent damage);
    }
}
