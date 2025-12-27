using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WindowsOptimizer.App.Diagnostics;

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

        if (AssociatedObject.Content is not FrameworkElement newElement)
        {
            return;
        }

        if (oldElement != null)
        {
            AnimateOut(oldElement, () =>
            {
                // After old element fades out, bring in new element
                _currentElement = newElement;
                PrepareElement(newElement);
                AnimateIn(newElement);
            });
        }
        else
        {
            _currentElement = newElement;
            PrepareElement(newElement);
            AnimateIn(newElement);
        }
    }

    private void PrepareElement(FrameworkElement element)
    {
        // Ensure element has a mutable transform we can animate.
        // WPF can freeze Freezables coming from resources/templates; animating a frozen transform throws:
        // "Cannot animate '(0).(1)' on an immutable object instance."
        if (element.RenderTransform is TransformGroup existingGroup)
        {
            if (existingGroup.IsFrozen || existingGroup.Children.IsFrozen)
            {
                element.RenderTransform = existingGroup.CloneCurrentValue();
            }
        }
        else if (element.RenderTransform is Freezable freezable && freezable.IsFrozen)
        {
            element.RenderTransform = (Transform)freezable.CloneCurrentValue();
        }

        if (element.RenderTransform is not TransformGroup group)
        {
            group = new TransformGroup();
            group.Children.Add(new ScaleTransform());
            group.Children.Add(new TranslateTransform());
            element.RenderTransform = group;
        }

        EnsureTranslateTransform(group);
        element.RenderTransformOrigin = new Point(0.5, 0.5);
    }

    private static TranslateTransform EnsureTranslateTransform(TransformGroup group)
    {
        for (var i = 0; i < group.Children.Count; i++)
        {
            if (group.Children[i] is TranslateTransform existing)
            {
                if (existing.IsFrozen)
                {
                    var clone = existing.CloneCurrentValue();
                    group.Children[i] = clone;
                    return clone;
                }

                return existing;
            }
        }

        var translate = new TranslateTransform();
        group.Children.Add(translate);
        return translate;
    }

    private TranslateTransform GetTranslateTransform(FrameworkElement element)
    {
        PrepareElement(element);
        var group = (TransformGroup)element.RenderTransform;
        return EnsureTranslateTransform(group);
    }

    private void AnimateOut(FrameworkElement element, Action? onCompleted = null)
    {
        var storyboard = new Storyboard();

        try
        {
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
                var translate = GetTranslateTransform(element);
                var slideOut = new DoubleAnimation
                {
                    From = 0,
                    To = -50,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(slideOut, translate);
                Storyboard.SetTargetProperty(slideOut, new PropertyPath(TranslateTransform.XProperty));
                storyboard.Children.Add(slideOut);
            }

            if (onCompleted != null)
            {
                storyboard.Completed += (s, e) => onCompleted();
            }

            storyboard.Begin();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("NavigationTransitionBehavior.AnimateOut", ex);

            try
            {
                element.Opacity = 0;
                GetTranslateTransform(element).X = -50;
            }
            catch
            {
            }

            onCompleted?.Invoke();
        }
    }

    private void AnimateIn(FrameworkElement element)
    {
        var storyboard = new Storyboard();

        try
        {
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
                var translate = GetTranslateTransform(element);
                var slideIn = new DoubleAnimation
                {
                    From = 50,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(slideIn, translate);
                Storyboard.SetTargetProperty(slideIn, new PropertyPath(TranslateTransform.XProperty));
                storyboard.Children.Add(slideIn);
            }

            element.Opacity = Transition == TransitionType.Fade || Transition == TransitionType.FadeAndSlide ? 0 : 1;
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("NavigationTransitionBehavior.AnimateIn", ex);

            try
            {
                element.Opacity = 1;
                GetTranslateTransform(element).X = 0;
            }
            catch
            {
            }
        }
    }
}
