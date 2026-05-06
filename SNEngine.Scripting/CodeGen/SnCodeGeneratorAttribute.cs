namespace SNEngine.Scripting.CodeGen;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SnCodeGeneratorAttribute : Attribute
{
    public Type TargetNodeType { get; }
    public SnCodeGeneratorAttribute(Type targetNodeType)
    {
        TargetNodeType = targetNodeType;
    }
}