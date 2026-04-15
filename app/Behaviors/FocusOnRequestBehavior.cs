using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace RegProbe.App.Behaviors;

public sealed class FocusOnRequestBehavior : Behavior<Control>
{
    public static readonly DependencyProperty RequestTokenProperty =
        DependencyProperty.Register(
            nameof(RequestToken),
            typeof(int),
            typeof(FocusOnRequestBehavior),
            new PropertyMetadata(0, OnRequestTokenChanged));

    public int RequestToken
    {
        get => (int)GetValue(RequestTokenProperty);
        set => SetValue(RequestTokenProperty, value);
    }

    private static void OnRequestTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FocusOnRequestBehavior behavior)
        {
            behavior.RequestFocus();
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnAssociatedObjectLoaded;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
        }

        base.OnDetaching();
    }

    private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        if (RequestToken > 0)
        {
            RequestFocus();
        }
    }

    private void RequestFocus()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        AssociatedObject.Dispatcher.BeginInvoke(() =>
        {
            if (!AssociatedObject.IsVisible || !AssociatedObject.IsEnabled)
            {
                return;
            }

            AssociatedObject.Focus();
            Keyboard.Focus(AssociatedObject);

            if (AssociatedObject is TextBox textBox)
            {
                textBox.SelectAll();
            }
        });
    }
}
