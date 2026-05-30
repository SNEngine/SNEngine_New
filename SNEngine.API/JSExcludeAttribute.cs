using System;

namespace SNEngine.API;

/// <summary>
/// Marks a method so that it will NOT be included in the generated JavaScript facade
/// (the 'sn' object exposed to Ultralight views).
///
/// Use this for methods that are only useful in the editor, require complex objects,
/// write to disk, or otherwise should not be callable from JavaScript at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class JSExcludeAttribute : Attribute
{
}
