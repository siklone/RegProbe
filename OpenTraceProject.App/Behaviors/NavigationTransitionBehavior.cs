using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OpenTraceProject.App.Diagnostics;

namespace OpenTraceProject.App.Behaviors;

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
        BeginIncomingTransition();
    }

    private void OnContentChanged(object? sender, EventArgs e)
    {
        BeginIncomingTransition();
    }

    private void BeginIncomingTransition()
    {
        AssociatedObject.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                try
                {
                    PrepareHost();
                    AnimateIn();
                }
                catch (Exception ex)
                {
                    AppDiagnostics.LogException("NavigationTransitionBehavior.BeginIncomingTransition", ex);
                    AssociatedObject.Opacity = 1;
                    GetTranslateTransform().Y = 0;
                }
            }));
    }

    private void PrepareHost()
    {
        if (AssociatedObject.RenderTransform is TransformGroup existingGroup)
        {
            if (existingGroup.IsFrozen || existingGroup.Children.IsFrozen)
            {
                AssociatedObject.RenderTransform = existingGroup.CloneCurrentValue();
            }
        }
        else if (AssociatedObject.RenderTransform is Freezable freezable && freezable.IsFrozen)
        {
            AssociatedObject.RenderTransform = (Transform)freezable.CloneCurrentValue();
        }

        if (AssociatedObject.RenderTransform is not TransformGroup group)
        {
            group = new TransformGroup();
            group.Children.Add(new TranslateTransform());
            AssociatedObject.RenderTransform = group;
        }

        EnsureTranslateTransform(group);
        AssociatedObject.RenderTransformOrigin = new Point(0.5, 0.5);
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

    private TranslateTransform GetTranslateTransform()
    {
        PrepareHost();
        var group = (TransformGroup)AssociatedObject.RenderTransform;
        return EnsureTranslateTransform(group);
    }

    private void AnimateIn()
    {
        var storyboard = new Storyboard();

        try
        {
            var translate = GetTranslateTransform();
            AssociatedObject.BeginAnimation(UIElement.OpacityProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);

            if (Transition == TransitionType.Fade || Transition == TransitionType.FadeAndSlide)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(190),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeIn, AssociatedObject);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(fadeIn);
            }

            if (Transition == TransitionType.Slide || Transition == TransitionType.FadeAndSlide)
            {
                var slideIn = new DoubleAnimation
                {
                    From = 6,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(190),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(slideIn, translate);
                Storyboard.SetTargetProperty(slideIn, new PropertyPath(TranslateTransform.YProperty));
                storyboard.Children.Add(slideIn);
            }

            AssociatedObject.Opacity = Transition == TransitionType.Fade || Transition == TransitionType.FadeAndSlide ? 0 : 1;
            translate.Y = Transition == TransitionType.Slide || Transition == TransitionType.FadeAndSlide ? 6 : 0;
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("NavigationTransitionBehavior.AnimateIn", ex);

            try
            {
                AssociatedObject.Opacity = 1;
                GetTranslateTransform().Y = 0;
            }
            catch
            {
            }
        }
    }
}
