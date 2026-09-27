using System;

namespace CalradiaForge.Sdk.Patcher
{
    public enum ForgeMethodType
    {
        Normal,
        Getter,
        Setter
    }

    /// <summary>
    /// Indicates that this method should replace a target method at runtime using ForgePatcher.
    /// This completely bypasses the original method and substitutes it with this one.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ForgePatchAttribute : Attribute
    {
        public Type TargetType { get; }
        public string TargetMethod { get; }
        public ForgeMethodType MethodType { get; }

        public ForgePatchAttribute(Type targetType, string targetMethod, ForgeMethodType methodType = ForgeMethodType.Normal)
        {
            TargetType = targetType;
            MethodType = methodType;

            // Automatically resolve getter/setter method names
            if (methodType == ForgeMethodType.Getter)
                TargetMethod = "get_" + targetMethod;
            else if (methodType == ForgeMethodType.Setter)
                TargetMethod = "set_" + targetMethod;
            else
                TargetMethod = targetMethod;
        }
    }
}
