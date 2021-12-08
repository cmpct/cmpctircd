namespace cmpctircd.Cloak
{
    /// <summary>
    /// CloakService abstract class.
    /// </summary>
    public abstract class CloakService
    {
        /// <summary>
        /// Gets a Cloak.
        /// </summary>
        /// <returns>A Cloak.</returns>
        public abstract Cloak GetCloak();
    }
}