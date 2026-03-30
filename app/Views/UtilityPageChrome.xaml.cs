using System.Windows;
using System.Windows.Controls;

namespace RegProbe.App.Views;

public partial class UtilityPageChrome : UserControl
{
    public static readonly DependencyProperty HeaderEyebrowProperty =
        DependencyProperty.Register(
            nameof(HeaderEyebrow),
            typeof(string),
            typeof(UtilityPageChrome),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeaderTitleProperty =
        DependencyProperty.Register(
            nameof(HeaderTitle),
            typeof(string),
            typeof(UtilityPageChrome),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeaderDescriptionProperty =
        DependencyProperty.Register(
            nameof(HeaderDescription),
            typeof(string),
            typeof(UtilityPageChrome),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BodyContentProperty =
        DependencyProperty.Register(
            nameof(BodyContent),
            typeof(object),
            typeof(UtilityPageChrome),
            new PropertyMetadata(null));

    public UtilityPageChrome()
    {
        InitializeComponent();
    }

    public string HeaderEyebrow
    {
        get => (string)GetValue(HeaderEyebrowProperty);
        set => SetValue(HeaderEyebrowProperty, value);
    }

    public string HeaderTitle
    {
        get => (string)GetValue(HeaderTitleProperty);
        set => SetValue(HeaderTitleProperty, value);
    }

    public string HeaderDescription
    {
        get => (string)GetValue(HeaderDescriptionProperty);
        set => SetValue(HeaderDescriptionProperty, value);
    }

    public object? BodyContent
    {
        get => GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }
}
