using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;

namespace Valo.App;

public sealed class GameScreen : Control
{
    public static readonly DirectProperty<GameScreen, AvaloniaLcd?> SourceProperty =
        AvaloniaProperty.RegisterDirect<GameScreen, AvaloniaLcd?>(
            nameof(Source),
            o => o.Source,
            (o, v) => o.Source = v
        );

    public AvaloniaLcd? Source {
        get;
        set => SetAndRaise(SourceProperty, ref field, value);
    }

    private CompositionCustomVisual? _visual;

    private static Size ScreenSize => new(ILcd.Width, ILcd.Height);

    static GameScreen()
    {
        AffectsRender<GameScreen>(SourceProperty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        Initialise();
        LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        LayoutUpdated -= OnLayoutUpdated;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty) {
            Initialise();
        }
    }

    private static int IntegralScale(Size available) =>
        Math.Max(
            1,
            (int)Math.Min(available.Width / ScreenSize.Width, available.Height / ScreenSize.Height)
        );

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = IntegralScale(availableSize);
        return new Size(ScreenSize.Width * scale, ScreenSize.Height * scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = IntegralScale(finalSize);
        return new Size(ScreenSize.Width * scale, ScreenSize.Height * scale);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public void AcceptKeyDown(KeyEventArgs e)
    {
        Console.WriteLine(e.Key);
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        _visual?.Size = new Vector(Bounds.Size.Width, Bounds.Size.Height);
    }

    private void Initialise()
    {
        var compositor = ElementComposition.GetElementVisual(this)?.Compositor;
        if (compositor == null) return;
        if (Source == null) return;

        _visual = compositor.CreateCustomVisual(Source);
        ElementComposition.SetElementChildVisual(this, _visual);

        _visual.Size = new Vector(Bounds.Size.Width, Bounds.Size.Height);
        _visual.RenderOptions = new RenderOptions {
            BitmapInterpolationMode = BitmapInterpolationMode.None,
            EdgeMode = EdgeMode.Aliased,
            RequiresFullOpacityHandling = false,
        };

        _visual.SendHandlerMessage("start");
        InvalidateVisual();
    }
}
