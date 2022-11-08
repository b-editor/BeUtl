﻿using Avalonia.Controls;
using Avalonia.Media;

using Beutl.ViewModels.Editors;

using FluentAvalonia.UI.Controls;

namespace Beutl.Views.Editors;

public sealed partial class ColorEditor : UserControl
{
    public ColorEditor()
    {
        InitializeComponent();
        colorPicker.FlyoutConfirmed += ColorPicker_ColorChanged;
    }

    private void ColorPicker_ColorChanged(ColorPickerButton sender, ColorButtonColorChangedEventArgs e)
    {
        Color? newColor = e.NewColor;
        if (DataContext is ColorEditorViewModel vm && newColor.HasValue)
        {
            vm.SetValue(vm.WrappedProperty.GetValue(), newColor.Value.ToMedia());
        }
    }
}