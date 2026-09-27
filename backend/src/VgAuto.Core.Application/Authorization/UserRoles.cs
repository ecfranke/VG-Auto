using System;

namespace VgAuto.Core.Application.Authorization
{
    /// <summary>
    /// Account roles.
    ///   user        - works in the application (work, clients, vehicles, inventory)
    ///   admin       - additionally company settings and the accounts of normal users
    ///   superadmin  - additionally administrators and roles
    /// The owner (the initial administrator) is a super administrator that nobody else can change.
    /// </summary>
    public static class UserRoles
    {
        public const string User = "user";
        public const string Admin = "admin";
        public const string SuperAdmin = "superadmin";

        public static bool IsValid(string role) => role == User || role == Admin || role == SuperAdmin;

        public static bool IsAdmin(string role) => role == Admin || role == SuperAdmin;

        public static string Normalize(string role)
        {
            var value = role?.Trim().ToLowerInvariant();
            return IsValid(value) ? value : User;
        }
    }

    public enum AdminAction
    {
        CreateAccount,
        EditProfile,
        ResetPassword,
        Unlock,
        Disable,
        Enable,
        UnlinkMicrosoft,
        ChangeRole,
    }

    /// <param name="Role">role of the administrator performing the action</param>
    public record AdminActor(string Role, bool IsOwner);

    /// <param name="HasAccount">false for employees without a login (mechanics)</param>
    /// <param name="Role">current role of the target account (user when it has none)</param>
    public record AdminTarget(bool HasAccount, string Role, bool IsOwner, bool IsSelf);

    /// <summary>Who may do what in the user administration.</summary>
    public static class AdminPermissions
    {
        /// <summary>Returns null when the action is allowed, otherwise the reason.</summary>
        /// <param name="newRole">the role to assign (ChangeRole, CreateAccount)</param>
        public static string Check(AdminActor actor, AdminTarget target, AdminAction action, string newRole = null)
        {
            if (actor == null || !UserRoles.IsAdmin(actor.Role)) return "Administrator rights are required.";
            var super = actor.Role == UserRoles.SuperAdmin;

            if (action == AdminAction.CreateAccount)
            {
                if (target.HasAccount) return "This employee already has a login.";
                var role = newRole ?? UserRoles.User;
                if (!UserRoles.IsValid(role)) return "Unknown role.";
                if (!super && role != UserRoles.User) return "Only a super administrator can create administrators.";
                return null;
            }

            if (action != AdminAction.EditProfile && !target.HasAccount) return "This employee has no login.";

            if (target.IsSelf)
            {
                // own name and contact details only; password, role and status are changed elsewhere or by another administrator
                return action == AdminAction.EditProfile ? null : "You cannot change your own account here.";
            }

            if (target.IsOwner) return "The owner account can only be changed by its owner.";

            if (!super && target.HasAccount && target.Role != UserRoles.User)
                return "Only a super administrator can change administrators.";

            if (action == AdminAction.ChangeRole)
            {
                if (!super) return "Only a super administrator can change roles.";
                if (!UserRoles.IsValid(newRole)) return "Unknown role.";
            }
            return null;
        }
    }
}
