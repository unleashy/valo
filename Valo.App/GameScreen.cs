using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;

namespace Valo.App;

public sealed class GameScreen : Control
{
    private CompositionCustomVisual? _visual;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
        if (compositor == null) return;

        _visual = compositor.CreateCustomVisual(new Handler());
        ElementComposition.SetElementChildVisual(this, _visual);
        _visual.Size = new Vector(Bounds.Size.Width, Bounds.Size.Height);

        LayoutUpdated += OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        _visual?.Size = new Vector(Bounds.Size.Width, Bounds.Size.Height);
    }

    public void AcceptKeyDown(KeyEventArgs e)
    {
        Console.WriteLine(e.Key);
    }
}

file sealed class Handler : CompositionCustomVisualHandler
{
    public override void OnRender(ImmediateDrawingContext ctx)
    {
        ctx.FillRectangle(
            new ImmutableSolidColorBrush(Colors.Black),
            GetRenderBounds()
        );
    }

    public override void OnAnimationFrameUpdate()
    {
        Invalidate();
        RegisterForNextAnimationFrameUpdate();
    }
}
