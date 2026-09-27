namespace VgAuto.Core.Application.Authorization
{
    public static class AppClaims
    {
        public const string RootRole = "Root";
        public const string FullName = "FullName";
        /// <summary>Present while the user still has to replace an initial/temporary password.</summary>
        public const string PasswordChangeRequired = "pwd_change_required";
        public const string AuthMethod = "amr";
        /// <summary>Role of the account (user / admin / superadmin), for display only: permissions are checked against the database.</summary>
        public const string AccountRole = "vg_role";
    }
}
