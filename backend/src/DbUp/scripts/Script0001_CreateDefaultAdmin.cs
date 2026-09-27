using System.Data;

namespace DbUp.Scripts
{
    /// <summary>
    /// Historical script. It used to create an "admin" user with the well known password "carcare".
    /// Kept (as a no-op) so the DbUp journal of existing databases stays consistent;
    /// the initial administrator is now created by <see cref="Script0004_CreateInitialAdmin"/>.
    /// </summary>
    internal class Script0001_CreateDefaultAdmin : DbUp.Engine.IScript
    {
        public string ProvideScript(Func<IDbCommand> dbCommandFactory) => "";
    }
}
