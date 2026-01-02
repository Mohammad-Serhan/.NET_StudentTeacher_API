using System;

namespace StudentApi.Attributes
{
    /// <summary>
    /// Simple attribute to mark endpoints that require specific permissions
    /// This is just a marker - the middleware will handle the actual permission checking
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequiredPermissionAttribute : Attribute
    {
        public string[] Permissions { get; }

        public RequiredPermissionAttribute(params string[] permissions)
        {
            Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        }
    }
}