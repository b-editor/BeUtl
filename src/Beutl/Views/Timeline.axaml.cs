﻿using System.Numerics;
using System.Text.Json.Nodes;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;

using Beutl.Media;
using Beutl.Models;
using Beutl.ProjectSystem;
using Beutl.Services;
using Beutl.ViewModels;
using Beutl.ViewModels.Tools;
using Beutl.ViewModels.Dialogs;
using Beutl.Views.Dialogs;

using FluentAvalonia.UI.Controls;

namespace Beutl.Views;

public sealed partial class Timeline : UserControl
{
    internal enum MouseFlags
    {
        Free,
        SeekBarPressed,
        RangeSelectionPressed
    }

    internal MouseFlags _mouseFlag = MouseFlags.Free;
    internal TimeSpan _pointerFrame;
    private TimelineViewModel? _viewModel;
    private readonly CompositeDisposable _disposables = new();
    private ElementView? _selectedLayer;
    private readonly List<(ElementViewModel Layer, bool IsSelectedOriginal)> _rangeSelection = new();

    public Timeline()
    {
        InitializeComponent();

        gridSplitter.DragDelta += GridSplitter_DragDelta;

        Scale.AddHandler(PointerWheelChangedEvent, ContentScroll_PointerWheelChanged, RoutingStrategies.Tunnel);
        ContentScroll.AddHandler(PointerWheelChangedEvent, ContentScroll_PointerWheelChanged, RoutingStrategies.Tunnel);

        TimelinePanel.AddHandler(DragDrop.DragOverEvent, TimelinePanel_DragOver);
        TimelinePanel.AddHandler(DragDrop.DropEvent, TimelinePanel_Drop);
        DragDrop.SetAllowDrop(TimelinePanel, true);

        this.SubscribeDataContextChange<TimelineViewModel>(OnDataContextAttached, OnDataContextDetached);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (DataContext is TimelineViewModel viewModel)
        {
            // KeyBindingsは変更してはならない。
            foreach (KeyBinding binding in viewModel.KeyBindings)
            {
                if (e.Handled)
                    break;
                binding.TryHandle(e);
            }
        }
    }

    private void OnDataContextDetached(TimelineViewModel obj)
    {
        _viewModel = null;

        TimelinePanel.Children.RemoveRange(2, TimelinePanel.Children.Count - 2);
        _selectedLayer = null;
        _rangeSelection.Clear();

        _disposables.Clear();
    }

    private void OnDataContextAttached(TimelineViewModel vm)
    {
        _viewModel = vm;

        vm.Layers.ForEachItem(
            AddElement,
            RemoveElement,
            () => { })
            .DisposeWith(_disposables);

        vm.Inlines.ForEachItem(
            OnAddedInline,
            OnRemovedInline,
            () => { })
            .DisposeWith(_disposables);

        ViewModel.Paste.Subscribe(async () =>
            {
                if (TopLevel.GetTopLevel(this) is { Clipboard: IClipboard clipboard })
                {
                    string[] formats = await clipboard.GetFormatsAsync();

                    if (formats.AsSpan().Contains(Constants.Element))
                    {
                        string? json = await clipboard.GetTextAsync();
                        if (json != null)
                        {
                            var layer = new Element();
                            layer.ReadFromJson(JsonNode.Parse(json)!.AsObject());
                            layer.Start = ViewModel.ClickedFrame;
                            layer.ZIndex = ViewModel.CalculateClickedLayer();

                            layer.Save(RandomFileNameGenerator.Generate(Path.GetDirectoryName(ViewModel.Scene.FileName)!, Constants.ElementFileExtension));

                            ViewModel.Scene.AddChild(layer).DoAndRecord(CommandRecorder.Default);
                        }
                    }
                }
            })
            .DisposeWith(_disposables);

        ViewModel.EditorContext.SelectedObject.Subscribe(e =>
            {
                if (_selectedLayer != null)
                {
                    foreach (ElementViewModel item in ViewModel.Layers.GetMarshal().Value)
                    {
                        item.IsSelected.Value = false;
                    }

                    _selectedLayer = null;
                }

                if (e is Element layer && FindLayerView(layer) is ElementView { DataContext: ElementViewModel viewModel } newView)
                {
                    viewModel.IsSelected.Value = true;
                    _selectedLayer = newView;
                }
            })
            .DisposeWith(_disposables);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        ViewModel.EditorContext.Options.Subscribe(options =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Vector2 offset = options.Offset;
                    ContentScroll.Offset = new(offset.X, offset.Y);
                    PaneScroll.Offset = new(0, offset.Y);
                }, DispatcherPriority.MaxValue);
            })
            .DisposeWith(_disposables);

        ContentScroll.ScrollChanged += ContentScroll_ScrollChanged;
    }

    internal TimelineViewModel ViewModel => _viewModel!;

    private void GridSplitter_DragDelta(object? sender, VectorEventArgs e)
    {
        ColumnDefinition def = grid.ColumnDefinitions[0];
        double last = def.ActualWidth + e.Vector.X;

        if (last is < 395 and > 385)
        {
            def.MaxWidth = 390;
            def.MinWidth = 390;
        }
        else
        {
            def.MaxWidth = double.PositiveInfinity;
            def.MinWidth = 200;
        }
    }

    // PaneScrollがスクロールされた
    private void PaneScroll_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        ContentScroll.Offset = ContentScroll.Offset.WithY(PaneScroll.Offset.Y);
    }

    // PaneScrollがスクロールされた
    private void ContentScroll_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        TimelineViewModel viewModel = ViewModel;
        Avalonia.Vector aOffset = ContentScroll.Offset;
        var offset = new Vector2((float)aOffset.X, (float)aOffset.Y);

        viewModel.Options.Value = viewModel.Options.Value with
        {
            Offset = offset
        };
    }

    private void UpdateZoom(PointerWheelEventArgs e, ref float scale, ref Vector2 offset)
    {
        float oldScale = scale;
        Point pointerPos = e.GetCurrentPoint(TimelinePanel).Position;
        double deltaLeft = pointerPos.X - offset.X;

        const float ZoomSpeed = 1.2f;
        float delta = (float)e.Delta.Y;
        float realDelta = MathF.Sign(delta) * MathF.Abs(delta);

        scale = MathF.Pow(ZoomSpeed, realDelta) * scale;
        scale = Math.Min(scale, 2);

        offset.X = (float)((pointerPos.X / oldScale * scale) - deltaLeft);
    }

    // マウスホイールが動いた
    private void ContentScroll_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        TimelineViewModel viewModel = ViewModel;
        Avalonia.Vector aOffset = ContentScroll.Offset;
        float scale = viewModel.Options.Value.Scale;
        var offset = new Vector2((float)aOffset.X, (float)aOffset.Y);

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            // 目盛りのスケールを変更
            UpdateZoom(e, ref scale, ref offset);
        }
        else if (e.KeyModifiers == KeyModifiers.Shift)
        {
            // オフセット(Y) をスクロール
            offset.Y -= (float)(e.Delta.Y * 50);
        }
        else
        {
            // オフセット(X) をスクロール
            offset.X -= (float)(e.Delta.Y * 50);
        }

        viewModel.Options.Value = viewModel.Options.Value with
        {
            Scale = scale,
            Offset = offset
        };

        e.Handled = true;
    }

    // ポインター移動
    private void TimelinePanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        TimelineViewModel viewModel = ViewModel;
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);
        _pointerFrame = pointerPt.Position.X.ToTimeSpan(viewModel.Options.Value.Scale)
            .RoundToRate(viewModel.Scene.FindHierarchicalParent<Project>() is { } proj ? proj.GetFrameRate() : 30);

        if (_mouseFlag == MouseFlags.SeekBarPressed)
        {
            viewModel.Scene.CurrentFrame = _pointerFrame;
        }
        else if (_mouseFlag == MouseFlags.RangeSelectionPressed)
        {
            Rect rect = overlay.SelectionRange;
            overlay.SelectionRange = new(rect.Position, pointerPt.Position);
            UpdateRangeSelection();
        }
    }

    // ポインターが放された
    private void TimelinePanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);

        if (pointerPt.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
        {
            if (_mouseFlag == MouseFlags.RangeSelectionPressed)
            {
                overlay.SelectionRange = default;
                _rangeSelection.Clear();
            }

            _mouseFlag = MouseFlags.Free;
        }
    }

    private void UpdateRangeSelection()
    {
        TimelineViewModel viewModel = ViewModel;
        foreach ((ElementViewModel layer, bool isSelectedOriginal) in _rangeSelection)
        {
            layer.IsSelected.Value = isSelectedOriginal;
        }

        _rangeSelection.Clear();
        Rect rect = overlay.SelectionRange.Normalize();
        var startTime = rect.Left.ToTimeSpan(viewModel.Options.Value.Scale);
        var endTime = rect.Right.ToTimeSpan(viewModel.Options.Value.Scale);
        var timeRange = TimeRange.FromRange(startTime, endTime);

        int startLayer = viewModel.ToLayerNumber(rect.Top);
        int endLayer = viewModel.ToLayerNumber(rect.Bottom);

        foreach (ElementViewModel item in viewModel.Layers)
        {
            if (timeRange.Intersects(item.Model.Range)
                && startLayer <= item.Model.ZIndex && item.Model.ZIndex <= endLayer)
            {
                _rangeSelection.Add((item, item.IsSelected.Value));
                item.IsSelected.Value = true;
            }
        }
    }

    // ポインターが押された
    private void TimelinePanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TimelineViewModel viewModel = ViewModel;
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);
        viewModel.ClickedFrame = pointerPt.Position.X.ToTimeSpan(viewModel.Options.Value.Scale)
            .RoundToRate(viewModel.Scene.FindHierarchicalParent<Project>() is { } proj ? proj.GetFrameRate() : 30);

        viewModel.ClickedPosition = pointerPt.Position;

        TimelinePanel.Focus();

        if (pointerPt.Properties.IsLeftButtonPressed)
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                _mouseFlag = MouseFlags.RangeSelectionPressed;
                overlay.SelectionRange = new(pointerPt.Position, default(Size));
            }
            else
            {
                _mouseFlag = MouseFlags.SeekBarPressed;
                viewModel.Scene.CurrentFrame = viewModel.ClickedFrame;
            }
        }
    }

    // ポインターが離れた
    private void TimelinePanel_PointerExited(object? sender, PointerEventArgs e)
    {
        _mouseFlag = MouseFlags.Free;
    }

    // ドロップされた
    private async void TimelinePanel_Drop(object? sender, DragEventArgs e)
    {
        TimelinePanel.Cursor = Cursors.Arrow;
        TimelineViewModel viewModel = ViewModel;
        Scene scene = ViewModel.Scene;
        Point pt = e.GetPosition(TimelinePanel);

        viewModel.ClickedFrame = pt.X.ToTimeSpan(viewModel.Options.Value.Scale)
            .RoundToRate(viewModel.Scene.FindHierarchicalParent<Project>() is { } proj ? proj.GetFrameRate() : 30);
        viewModel.ClickedPosition = pt;

        if (e.Data.Get(KnownLibraryItemFormats.SourceOperator) is Type type)
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                var dialog = new AddElementDialog
                {
                    DataContext = new AddElementDialogViewModel(
                        scene,
                        new ElementDescription(
                            viewModel.ClickedFrame,
                            TimeSpan.FromSeconds(5),
                            viewModel.CalculateClickedLayer(),
                            InitialOperator: type))
                };
                await dialog.ShowAsync();
            }
            else
            {
                viewModel.AddLayer.Execute(new ElementDescription(
                    viewModel.ClickedFrame, TimeSpan.FromSeconds(5), viewModel.CalculateClickedLayer(), InitialOperator: type));
            }
        }
    }

    private void TimelinePanel_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(KnownLibraryItemFormats.SourceOperator)
            || (e.Data.GetFiles()?.Any() ?? false))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    // 要素を追加
    private async void AddElementClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AddElementDialog
        {
            DataContext = new AddElementDialogViewModel(ViewModel.Scene,
                new ElementDescription(ViewModel.ClickedFrame, TimeSpan.FromSeconds(5), ViewModel.CalculateClickedLayer()))
        };
        await dialog.ShowAsync();
    }

    private void ShowSceneSettings(object? sender, RoutedEventArgs e)
    {
        EditViewModel editorContext = ViewModel.EditorContext;
        SceneSettingsTabViewModel? tab = editorContext.FindToolTab<SceneSettingsTabViewModel>();
        if (tab != null)
        {
            tab.IsSelected.Value = true;
        }
        else
        {
            editorContext.OpenToolTab(new SceneSettingsTabViewModel(editorContext));
        }
    }

    // 要素を追加
    private void AddElement(int index, ElementViewModel viewModel)
    {
        var view = new ElementView
        {
            DataContext = viewModel
        };
        var scopeView = new ElementScopeView
        {
            DataContext = viewModel.Scope
        };

        TimelinePanel.Children.Add(view);
        TimelinePanel.Children.Add(scopeView);
    }

    // 要素を削除
    private void RemoveElement(int index, ElementViewModel viewModel)
    {
        Element elm = viewModel.Model;

        for (int i = 0; i < TimelinePanel.Children.Count; i++)
        {
            Control item = TimelinePanel.Children[i];
            if ((item.DataContext is ElementViewModel vm1 && vm1.Model == elm)
                || (item.DataContext is ElementScopeViewModel vm2 && vm2.Model == elm))
            {
                TimelinePanel.Children.RemoveAt(i);
                break;
            }
        }
    }

    private void OnAddedInline(InlineAnimationLayerViewModel viewModel)
    {
        var view = new InlineAnimationLayer
        {
            DataContext = viewModel
        };

        TimelinePanel.Children.Add(view);
    }

    private void OnRemovedInline(InlineAnimationLayerViewModel viewModel)
    {
        IAbstractAnimatableProperty prop = viewModel.Property;
        for (int i = 0; i < TimelinePanel.Children.Count; i++)
        {
            Control item = TimelinePanel.Children[i];
            if (item.DataContext is InlineAnimationLayerViewModel vm && vm.Property == prop)
            {
                TimelinePanel.Children.RemoveAt(i);
                break;
            }
        }
    }

    private ElementView? FindLayerView(Element layer)
    {
        return TimelinePanel.Children.FirstOrDefault(ctr => ctr.DataContext is ElementViewModel vm && vm.Model == layer) as ElementView;
    }

    private void ZoomClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is MenuFlyoutItem menuItem)
        {
            float zoom;
            switch (menuItem.CommandParameter)
            {
                case string str:
                    if (!float.TryParse(str, out zoom))
                    {
                        return;
                    }
                    break;
                case double zoom1:
                    zoom = (float)zoom1;
                    break;
                case float zoom2:
                    zoom = zoom2;
                    break;
                default:
                    return;
            }

            ViewModel.Options.Value = ViewModel.Options.Value with
            {
                Scale = zoom,
            };
        }
    }
}
