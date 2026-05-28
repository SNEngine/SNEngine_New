using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using TrippyGL;

namespace SNEngine.Core.Rendering;

internal readonly struct DrawCommand
{
    public readonly Texture2D Texture;
    public readonly Vector2 Position;
    public readonly Rectangle? SourceRect;
    public readonly Color4b Color;
    public readonly float ScaleX;
    public readonly float Rotation;
    public readonly Vector2 Origin;
    public readonly RenderLayer Layer;

    public DrawCommand(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRect,
        Color4b color,
        float scaleX,
        float rotation,
        Vector2 origin,
        RenderLayer layer)
    {
        Texture = texture;
        Position = position;
        SourceRect = sourceRect;
        Color = color;
        ScaleX = scaleX;
        Rotation = rotation;
        Origin = origin;
        Layer = layer;
    }
}

/// <summary>
/// Collects draw commands during the frame and executes them in layer order.
/// Extracted from Renderer to reduce its responsibilities.
/// </summary>
internal sealed class DrawCommandBuffer
{
    private readonly List<DrawCommand> _commands = new();

    public void Clear() => _commands.Clear();

    public void Add(DrawCommand command) => _commands.Add(command);

    public bool HasCommands => _commands.Count > 0;

    public void SortByLayer()
    {
        _commands.Sort((a, b) => a.Layer.CompareTo(b.Layer));
    }

    public void Execute(TextureBatcher batcher)
    {
        if (batcher == null || _commands.Count == 0)
            return;

        batcher.Begin(BatcherBeginMode.Deferred);

        foreach (var cmd in _commands)
        {
            batcher.Draw(
                cmd.Texture,
                cmd.Position,
                cmd.SourceRect,
                cmd.Color,
                cmd.ScaleX,
                cmd.Rotation,
                cmd.Origin);
        }

        batcher.End();
    }
}
