using System.Collections;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace ReDocking;

public class EdgeBar : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable> TopToolsSourceProperty =
        AvaloniaProperty.Register<EdgeBar, IEnumerable>(nameof(TopToolsSource));

    public static readonly StyledProperty<IEnumerable> ToolsSourceProperty = 
        AvaloniaProperty.Register<EdgeBar, IEnumerable>(nameof(ToolsSource));

    public static readonly StyledProperty<IEnumerable> BottomToolsSourceProperty = 
        AvaloniaProperty.Register<EdgeBar, IEnumerable>(nameof(BottomToolsSource));

    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        AvaloniaProperty.Register<EdgeBar, IDataTemplate>(nameof(ItemTemplate));

    public IDataTemplate ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }
    
    public IEnumerable TopToolsSource
    {
        get => GetValue(TopToolsSourceProperty);
        set => SetValue(TopToolsSourceProperty, value);
    }

    public IEnumerable ToolsSource
    {
        get => GetValue(ToolsSourceProperty);
        set => SetValue(ToolsSourceProperty, value);
    }
    
    public IEnumerable BottomToolsSource
    {
        get => GetValue(BottomToolsSourceProperty);
        set => SetValue(BottomToolsSourceProperty, value);
    }
}