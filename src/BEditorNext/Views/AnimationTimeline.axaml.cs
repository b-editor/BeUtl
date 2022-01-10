using System.Numerics;

using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;

using BEditorNext.Animation;
using BEditorNext.Animation.Easings;
using BEditorNext.ProjectSystem;
using BEditorNext.Services;
using BEditorNext.ViewModels;

using static BEditorNext.Views.Timeline;

namespace BEditorNext.Views;

public partial class AnimationTimeline : UserControl
{
    internal MouseFlags _seekbarMouseFlag = MouseFlags.MouseUp;
    private TimeSpan _clickedFrame;
    internal TimeSpan _pointerFrame;
    private AnimationTimelineViewModel? _viewModel;
    private bool _isFirst = true;

    public AnimationTimeline()
    {
        Resources["AnimationToViewModelConverter"] =
            new FuncValueConverter<IAnimation, object?>(a => a == null ? null : PropertyEditorService.CreateAnimationEditorViewModel(ViewModel.EditorViewModel, a));

        InitializeComponent();
        ContentScroll.ScrollChanged += ContentScroll_ScrollChanged;
        ContentScroll.AddHandler(PointerWheelChangedEvent, ContentScroll_PointerWheelChanged, RoutingStrategies.Tunnel);
        ScaleScroll.AddHandler(PointerWheelChangedEvent, ContentScroll_PointerWheelChanged, RoutingStrategies.Tunnel);
        TimelinePanel.AddHandler(DragDrop.DragOverEvent, TimelinePanel_DragOver);
        TimelinePanel.AddHandler(DragDrop.DropEvent, TimelinePanel_Drop);
    }

    internal AnimationTimelineViewModel ViewModel => _viewModel!;

    // DataContext���ύX���ꂽ
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is AnimationTimelineViewModel vm)
        {
            _viewModel = vm;
        }
    }

    // ContentScroll���X�N���[�����ꂽ
    private void ContentScroll_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        Scene scene = ViewModel.Scene;
        if (_isFirst)
        {
            ContentScroll.Offset = new(scene.TimelineOptions.Offset.X, scene.TimelineOptions.Offset.Y);

            _isFirst = false;
        }

        scene.TimelineOptions = scene.TimelineOptions with
        {
            Offset = new Vector2((float)ContentScroll.Offset.X, (float)ContentScroll.Offset.Y)
        };

        ScaleScroll.Offset = new(ContentScroll.Offset.X, 0);
    }

    // �}�E�X�z�C�[����������
    private void ContentScroll_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        Scene scene = ViewModel.Scene;
        Avalonia.Vector offset = ContentScroll.Offset;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            // �ڐ���̃X�P�[����ύX
            float scale = scene.TimelineOptions.Scale;
            var ts = offset.X.ToTimeSpan(scale);
            float deltaScale = (float)(e.Delta.Y / 120) * 10 * scale;
            scene.TimelineOptions = scene.TimelineOptions with
            {
                Scale = deltaScale + scale,
            };

            offset = offset.WithX(ts.ToPixel(scene.TimelineOptions.Scale));
        }
        else if (e.KeyModifiers == KeyModifiers.Shift)
        {
            // �I�t�Z�b�g(Y) ���X�N���[��
            offset = offset.WithY(offset.Y - (e.Delta.Y * 50));
        }
        else
        {
            // �I�t�Z�b�g(X) ���X�N���[��
            offset = offset.WithX(offset.X - (e.Delta.Y * 50));
        }

        ContentScroll.Offset = offset;
        e.Handled = true;
    }

    // �|�C���^�[�ړ�
    private void TimelinePanel_PointerMoved(object? sender, PointerEventArgs e)
    {
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);
        _pointerFrame = pointerPt.Position.X.ToTimeSpan(ViewModel.Scene.TimelineOptions.Scale);

        if (_seekbarMouseFlag == MouseFlags.MouseDown)
        {
            ViewModel.Scene.CurrentFrame = _pointerFrame;
        }
    }

    // �|�C���^�[�������ꂽ
    private void TimelinePanel_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);

        if (pointerPt.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
        {
            _seekbarMouseFlag = MouseFlags.MouseUp;
        }
    }

    // �|�C���^�[�������ꂽ
    private void TimelinePanel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint pointerPt = e.GetCurrentPoint(TimelinePanel);
        _clickedFrame = pointerPt.Position.X.ToTimeSpan(ViewModel.Scene.TimelineOptions.Scale);

        if (pointerPt.Properties.IsLeftButtonPressed)
        {
            _seekbarMouseFlag = MouseFlags.MouseDown;
            ViewModel.Scene.CurrentFrame = _clickedFrame;
        }
    }

    // �|�C���^�[�����ꂽ
    private void TimelinePanel_PointerLeave(object? sender, PointerEventArgs e)
    {
        _seekbarMouseFlag = MouseFlags.MouseUp;
    }

    private void TimelinePanel_Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Get("Easing") is Easing easing)
        {
            ViewModel.AddAnimation(easing);
            e.Handled = true;
        }
    }

    private void TimelinePanel_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("Easing"))
        {
            e.DragEffects = DragDropEffects.Copy | DragDropEffects.Link;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
}
