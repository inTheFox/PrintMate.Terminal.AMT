using System;

namespace PrintMate.Terminal.ConfigurationSystem.Attributes
{
    /// <summary>
    /// Marks a property as sensitive and requiring encryption in storage.
    /// Only applies to string properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EncryptedAttribute : Attribute
    {
    }
}
