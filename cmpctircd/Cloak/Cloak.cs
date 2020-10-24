namespace cmpctircd.Cloak
{
    /// <summary>
    /// <see cref="Cloak"/> abstract class.
    /// </summary>
    public abstract class Cloak
    {
        /// <summary>
        /// Gets a cloak string.
        /// </summary>
        /// <param name="cloakOptions">The client's cloaking parameters.</param>
        /// <returns>A cloak string.</returns>
        public abstract string GetCloakString(CloakOptions cloakOptions);
    }
}
