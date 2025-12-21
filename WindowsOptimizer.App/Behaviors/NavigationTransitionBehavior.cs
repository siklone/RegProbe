using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WindowsOptimizer.App.Behaviors;

public enum TransitionType
{
    Fade,
    Slide,
    FadeAndSlide
}

public sealed class NavigationTransitionBehavior : Behavior<ContentControl>
{
    public static readonly DependencyProperty TransitionProperty =
        DependencyProperty.Register(
            nameof(Transition),
            typeof(TransitionType),
            typeof(NavigationTransitionBehavior),
            new PropertyMetadata(TransitionType.FadeAndSlide));

    public TransitionType Transition
    {
        get => (TransitionType)GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    private FrameworkElement? _currentElement;
    private DependencyPropertyDescriptor? _contentPropertyDescriptor;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        if (_contentPropertyDescriptor != null)
        {
            _contentPropertyDescriptor.RemoveValueChanged(AssociatedObject, OnContentChanged);
        }
        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _contentPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(
            ContentControl.ContentProperty,
            typeof(ContentControl));
        _contentPropertyDescriptor?.AddValueChanged(AssociatedObject, OnContentChanged);

        // Initialize first view
        if (AssociatedObject.Content is FrameworkElement element)
        {
            _currentElement = element;
            PrepareElement(element);
            AnimateIn(element);
        }
    }

    private void OnContentChanged(object? sender, EventArgs e)
    {
        var oldElement = _currentElement;
        var newContent = AssociatedObject.Content;

        if (oldElement != null && newContent is FrameworkElement newElement)
        {
            AnimateOut(oldElement, () =>
            {
                // After old element fades out, bring in new element
                _currentElement = newElement;
                PrepareElement(newElement);
                AnimateIn(newElement);
            });
        }
        else if (newContent is FrameworkElement newElement)
        {
            _currentElement = newElement;
            PrepareElement(newElement);
            AnimateIn(newElement);
        }
    }

    private void PrepareElement(FrameworkElement element)
    {
        // Ensure element has transform groups
        if (element.RenderTransform is not TransformGroup)
        {
            element.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new ScaleTransform(),
                    new TranslateTransform()
                }
            };
        }
        element.RenderTransformOrigin = new Point(0.5, 0.5);
    }

    private void AnimateOut(FrameworkElement element, Action? onCompleted = null)
    {
        var storyboard = new Storyboard();

        if (Transition == TransitionType.Fade || Transition == TransitionType.FadeAndSlide)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fadeOut, element);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeOut);
        }

        if (Transition == TransitionType.Slide || Transition == TransitionType.FadeAndSlide)
        {
            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = -50,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(slideOut, element);
            Storyboard.SetTargetProperty(slideOut, new PropertyPath("RenderTransform.Children[1].X"));
            storyboard.Children.Add(slideOut);
        }

        if (onCompleted != null)
        {
            storyboard.Completed += (s, e) => onCompleted();
        }

        storyboard.Begin();
    }

    private void AnimateIn(FrameworkElement element)
    {
        var storyboard = new Storyboard();

        if (Transition == TransitionType.Fade || Transition == TransitionType.FadeAndSlide)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, element);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Children.Add(fadeIn);
        }

        if (Transition == TransitionType.Slide || Transition == TransitionType.FadeAndSlide)
        {
            var slideIn = new DoubleAnimation
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slideIn, element);
            Storyboard.SetTargetProperty(slideIn, new PropertyPath("RenderTransform.Children[1].X"));
            storyboard.Children.Add(slideIn);
        }

        element.Opacity = Transition == TransitionType.Fade || Transition == TransitionType.FadeAndSlide ? 0 : 1;
        storyboard.Begin();
    }
}
