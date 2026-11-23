using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Citron;
// Keep native CheckBox semantics, focus, colors and accessibility; replace only its animated mark.
internal sealed partial class SettingsCheckBox : CheckBox
{
    Path? mark;
    internal SettingsCheckBox()
    {
        IsThreeState = false;
        Checked += (_, _) => UpdateMark(); Unchecked += (_, _) => UpdateMark();
    }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        mark = null;
        if (GetTemplateChild("CheckGlyph") is AnimatedIcon animated && VisualTreeHelper.GetParent(animated) is Panel parent)
        {
            animated.Source = null; animated.FallbackIconSource = null; animated.Visibility = Visibility.Collapsed;
            var figure = new PathFigure { StartPoint = new Point(4, 10), IsClosed = false };
            figure.Segments.Add(new LineSegment { Point = new Point(8, 14) });
            figure.Segments.Add(new LineSegment { Point = new Point(16, 5) });
            var geometry = new PathGeometry(); geometry.Figures.Add(figure);
            mark = new Path { Data = geometry, Width = 20, Height = 20, StrokeThickness = 1.8,
                StrokeLineJoin = PenLineJoin.Round, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
            mark.SetBinding(Shape.StrokeProperty, new Binding { Source = animated, Path = new PropertyPath("Foreground") });
            parent.Children.Add(mark);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(mark, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        }
        UpdateMark();
    }
    void UpdateMark() { if (mark != null) mark.Visibility = IsChecked == true ? Visibility.Visible : Visibility.Collapsed; }
}
