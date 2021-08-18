using System;
using System.Linq;
using System.Reactive.Disposables;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

using BEditor.Data;
using BEditor.Data.Property;
using BEditor.Extensions;
using BEditor.Properties;
using BEditor.ViewModels.Timelines;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BEditor.Views.Timelines
{
    public class KeyframeView : UserControl
    {
        private readonly Grid _grid;
        private readonly TextBlock _text;
        private readonly CompositeDisposable _disposable = new();
        private readonly Animation _anm = new()
        {
            Duration = TimeSpan.FromSeconds(0.15),
            Children =
            {
                new()
                {
                    Cue = new(0),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1d)
                    }
                },
                new()
                {
                    Cue = new(1),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0d)
                    }
                }
            }
        };
        private Media.Frame _startpos;
        private Shape? _select;
        private Size _recentSize;

#pragma warning disable CS8618
        public KeyframeView()
#pragma warning restore CS8618
        {
            InitializeComponent();
        }

        public KeyframeView(IKeyframeProperty property)
        {
            var viewmodel = new KeyframeViewModel(property);

            DataContext = viewmodel;
            InitializeComponent();
            _grid = this.FindControl<Grid>("grid");
            _text = this.FindControl<TextBlock>("text");

            _grid.AddHandler(PointerPressedEvent, Grid_PointerLeftPressedTunnel, RoutingStrategies.Tunnel);
            _grid.AddHandler(PointerMovedEvent, Grid_PointerMovedTunnel, RoutingStrategies.Tunnel);
            _grid.AddHandler(PointerPressedEvent, Grid_PointerRightPressedTunnel, RoutingStrategies.Tunnel);
            _grid.AddHandler(PointerReleasedEvent, Grid_PointerReleasedTunnel, RoutingStrategies.Tunnel);

            viewmodel.AddKeyFrameIcon = pos =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var index = Property.IndexOf(pos);
                    index--;
                    var length = Property.GetRequiredParent<ClipElement>().Length;
                    var x = Scene.ToPixel((Media.Frame)(pos.GetAbsolutePosition(length)));
                    var icon = new Rectangle
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(x, 0, 0, 0),
                        Width = 8,
                        Height = 8,
                        RenderTransform = new RotateTransform { Angle = 45 },
                        Fill = (IBrush?)Application.Current.FindResource("TextControlForeground"),
                        Tag = pos,
                    };

                    Add_Handler_Icon(icon);

                    //icon.ContextMenu = new ContextMenu
                    //{
                    //    Items = new MenuItem[] { CreateMenu() }
                    //};

                    icon.ContextMenu = CreateContextMenu(pos);

                    _grid.Children.Insert(index, icon);
                });
            };
            viewmodel.RemoveKeyFrameIcon = (pos) => Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in _grid.Children)
                {
                    if (item is Shape shape && shape.Tag is PositionInfo pi && pi == pos)
                    {
                        _grid.Children.Remove(item);
                        break;
                    }
                }
            });
            viewmodel.MoveKeyFrameIcon = (from, to) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var tag = Property.Enumerate().ElementAt(to);
                    from--;
                    to--;
                    var icon = (Shape)_grid.Children[from];
                    icon.Tag = tag;

                    _grid.Children.RemoveAt(from);
                    _grid.Children.Insert(to, icon);
                });
            };

            _grid.Children.Clear();

            if (Property is IKeyframeProperty<float> f)
            {
                for (var index = 1; index < f.Pairs.Count - 1; index++)
                {
                    viewmodel.AddKeyFrameIcon(f.Pairs[index].Position);
                }
            }
            else if (Property is IKeyframeProperty<Drawing.Color> c)
            {
                for (var index = 1; index < c.Pairs.Count - 1; index++)
                {
                    viewmodel.AddKeyFrameIcon(c.Pairs[index].Position);
                }
            }

            // StoryBoard��ݒ�
            {
                PointerEnter += async (_, _) =>
                {
                    _anm.PlaybackDirection = PlaybackDirection.Normal;
                    await _anm.RunAsync(_text);

                    _text.Opacity = 0;
                };
                PointerLeave += async (_, _) =>
                {
                    _anm.PlaybackDirection = PlaybackDirection.Reverse;
                    await _anm.RunAsync(_text);

                    _text.Opacity = 1;
                };
            }
        }

        private Scene Scene => Property.GetParent<Scene>()!;
        private KeyframeViewModel ViewModel => (KeyframeViewModel)DataContext!;
        private IKeyframeProperty Property => ViewModel.Property;

        // �T�C�Y�ύX
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_recentSize != availableSize)
            {
                if (Property is IKeyframeProperty<float> f)
                {
                    var length = Scene.ToFrame(availableSize.Width);
                    for (var frame = 0; frame < f.Pairs.Count - 2; frame++)
                    {
                        if (_grid.Children.Count <= frame) break;

                        if (_grid.Children[frame] is Shape icon)
                        {
                            icon.Margin = new Thickness(Scene.ToPixel((Media.Frame)f.Pairs[frame + 1].Position.GetAbsolutePosition(length)), 0, 0, 0);
                        }
                    }
                }
                else if (Property is IKeyframeProperty<Drawing.Color> c)
                {
                    var length = Scene.ToFrame(availableSize.Width);
                    for (var frame = 0; frame < c.Pairs.Count - 2; frame++)
                    {
                        if (_grid.Children.Count <= frame) break;

                        if (_grid.Children[frame] is Shape icon)
                        {
                            icon.Margin = new Thickness(Scene.ToPixel((Media.Frame)c.Pairs[frame + 1].Position.GetAbsolutePosition(length)), 0, 0, 0);
                        }
                    }
                }
                _recentSize = availableSize;
            }

            return base.MeasureOverride(availableSize);
        }

        // icon�̃C�x���g��ǉ�
        private void Add_Handler_Icon(Shape icon)
        {
            icon.PointerPressed += Icon_PointerPressed;
            icon.PointerReleased += Icon_PointerReleased;
            icon.PointerMoved += Icon_PointerMoved;
            icon.PointerLeave += Icon_PointerLeave;
        }

        // icon�̃C�x���g���폜
        private void Remove_Handler_Icon(Shape icon)
        {
            icon.PointerPressed -= Icon_PointerPressed;
            icon.PointerReleased -= Icon_PointerReleased;
            icon.PointerMoved -= Icon_PointerMoved;
            icon.PointerLeave -= Icon_PointerLeave;
        }

        // �L�[�t���[����ǉ�
        public void Add_Frame(object sender, RoutedEventArgs e)
        {
            ViewModel.AddKeyFrameCommand.Execute(new(_startpos / (float)Property.GetRequiredParent<ClipElement>().Length, PositionType.Percentage));
        }

        // Icon��PointerPressed�C�x���g
        // �ړ��J�n
        private void Icon_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _startpos = Scene.ToFrame(e.GetPosition(_grid).X);

            _select = (Shape)sender!;

            // �J�[�\���̐ݒ�
            if (_select.Cursor == Cursors.SizeWestEast)
            {
                _grid.Cursor = Cursors.SizeWestEast;
            }

            // �C�x���g�̍폜
            foreach (var icon in _grid.Children.OfType<Shape>().Where(i => i != _select))
            {
                Remove_Handler_Icon(icon);
            }
        }

        // Icon��PointerReleased�C�x���g
        // �ړ��I��
        private void Icon_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // �J�[�\���̐ݒ�
            _grid.Cursor = Cursors.Arrow;
            if (_select is not null)
            {
                _select.Cursor = Cursors.Arrow;
            }

            // �C�x���g�̒ǉ�
            foreach (var icon in _grid.Children.OfType<Shape>().Where(i => i != _select))
            {
                Add_Handler_Icon(icon);
            }

            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                Icon_PointerLeftReleased(sender, e);
            }
        }

        // Icon��PointerMoved�C�x���g
        private void Icon_PointerMoved(object? sender, PointerEventArgs e)
        {
            _select = (Shape)sender!;

            // �J�[�\���̐ݒ�
            _select.Cursor = Cursors.SizeWestEast;

            // Timeline�̈ꕔ�̑���𖳌���
            Scene.GetCreateTimelineViewModel().KeyframeToggle = false;
        }

        // Icon��PointerLeave�C�x���g
        private void Icon_PointerLeave(object? sender, PointerEventArgs e)
        {
            var senderIcon = (Shape)sender!;

            // �J�[�\���̐ݒ�
            senderIcon.Cursor = Cursors.Arrow;

            // Timeline�̈ꕔ�̑����L����
            Scene.GetCreateTimelineViewModel().KeyframeToggle = true;

            // �C�x���g�̍Đݒ�
            foreach (var icon in _grid.Children.OfType<Shape>().Where(i => i != senderIcon))
            {
                Remove_Handler_Icon(icon);
                Add_Handler_Icon(icon);
            }
        }

        // Icon��PointerLeftReleased�C�x���g
        // �ړ��I��, �ۑ�
        private void Icon_PointerLeftReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_select is not null)
            {
                // �C���f�b�N�X
                var idx = _grid.Children.IndexOf(_select);
                // �N���b�v����̃t���[��
                var frame = Scene.ToFrame(_select.Margin.Left) / (float)Property.GetRequiredParent<ClipElement>().Length;

                if (frame > 0 && frame < 1)
                {
                    ViewModel.MoveKeyFrameCommand.Execute((idx + 1, frame));
                }
            }
        }

        // grid��PointerMoved�C�x���g (Tunnel)
        // icon��ui��margin��ݒ�
        private void Grid_PointerMovedTunnel(object? sender, PointerEventArgs e)
        {
            if (!(_select is null) && _grid.Cursor == Cursors.SizeWestEast)
            {
                // ���݂̃}�E�X�̈ʒu (frame)
                var now = Scene.ToFrame(e.GetPosition(_grid).X);

                if (now > 0 && now < Property.GetRequiredParent<ClipElement>().Length)
                {
                    _select.Margin = new Thickness(Scene.ToPixel(now), 0, 0, 0);
                    _startpos = now;
                }
            }
        }

        // grid��PointerRightPressed�C�x���g (Tunnel)
        private void Grid_PointerRightPressedTunnel(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint((Avalonia.VisualTree.IVisual?)sender).Properties.IsRightButtonPressed) return;

            // �E�N���b�N -> ���j���[ ->�u�L�[�t���[����ǉ��v�Ȃ̂�
            // ���݈ʒu��ۑ� (frame)
            //_nowframe = Scene.GetCreateTimeLineViewModel().ToFrame(e.GetPosition(grid).X);
            _startpos = Scene.ToFrame(e.GetPosition(_grid).X);
        }

        // grid��PointerReleased�C�x���g (Tunnel)
        private void Grid_PointerReleasedTunnel(object? sender, PointerReleasedEventArgs e)
        {
            // �J�[�\���̐ݒ�
            _grid.Cursor = Cursors.Arrow;
            if (_select is null) return;

            if (_select.Cursor == Cursors.SizeWestEast)
            {
                _grid.Cursor = Cursors.SizeWestEast;
            }
        }

        // grid��PointerLeftPressed�C�x���g (Tunnel)
        private void Grid_PointerLeftPressedTunnel(object? sender, PointerPressedEventArgs e)
        {
            if (_select is null || !e.GetCurrentPoint((Avalonia.VisualTree.IVisual?)sender).Properties.IsLeftButtonPressed) return;

            if (_select.Cursor == Cursors.SizeWestEast)
            {
                // ���݈ʒu��ۑ�
                _startpos = Scene.ToFrame(e.GetPosition(_grid).X);
            }
        }

        // grid��PointerLeave�C�x���g
        public void Grid_PointerLeave(object sender, PointerEventArgs e)
        {
            // �J�[�\���̐ݒ�
            _grid.Cursor = Cursors.Arrow;
            if (_select is null) return;

            if (_select.Cursor == Cursors.SizeWestEast)
            {
                _grid.Cursor = Cursors.SizeWestEast;
            }
        }

        // Icon�̃��j���[���쐬
        private ContextMenu CreateContextMenu(PositionInfo position)
        {
            var context = new ContextMenu();

            var removeMenu = new MenuItem
            {
                Icon = new FluentAvalonia.UI.Controls.SymbolIcon
                {
                    Symbol = FluentAvalonia.UI.Controls.Symbol.Delete,
                    FontSize = 20,
                },
                Header = Strings.Remove
            };

            removeMenu.Click += Remove_Click;

            var saveAsFrameNumberMenu = new MenuItem
            {
                Header = Strings.SavePositionAsFrameNumber
            };

            saveAsFrameNumberMenu.Click += SaveAsFrameNumber_Click;

            var saveAsPercentageMenu = new MenuItem
            {
                Header = Strings.SavePositionAsPercentage
            };

            saveAsPercentageMenu.Click += SaveAsPercentage_Click;

            var icon = new FluentAvalonia.UI.Controls.PathIcon
            {
                Data = StreamGeometry.Parse("M0,2a2,2 0 1,0 4,0a2,2 0 1,0 -4,0"),
                UseLayoutRounding = false,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            if (position.Type == PositionType.Percentage)
            {
                saveAsPercentageMenu.Icon = icon;
            }
            else
            {
                saveAsFrameNumberMenu.Icon = icon;
            }

            saveAsPercentageMenu.Tag = saveAsFrameNumberMenu;
            saveAsFrameNumberMenu.Tag = saveAsPercentageMenu;

            context.Items = new object[]
            {
                removeMenu,
                saveAsFrameNumberMenu,
                saveAsPercentageMenu
            };

            return context;
        }

        // PositionType��Percentage�ɕύX
        private void SaveAsPercentage_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu && menu.Tag is MenuItem menu1)
            {
                var shape = menu.FindLogicalAncestorOfType<Shape>();
                if (shape.Tag is PositionInfo pos && pos.Type != PositionType.Percentage)
                {
                    var index = Property.IndexOf(pos);
                    pos = pos.WithType(PositionType.Percentage, Property.GetRequiredParent<ClipElement>().Length);
                    Property.UpdatePositionInfo(index, pos).Execute();
                    shape.Tag = pos;

                    // �A�C�R���ύX
                    var icon = menu1.Icon;
                    menu1.Icon = null!;
                    menu.Icon = icon;
                }
            }
        }

        // PositionType��Abs�ɕύX
        private void SaveAsFrameNumber_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu && menu.Tag is MenuItem menu1)
            {
                var shape = menu.FindLogicalAncestorOfType<Shape>();
                if (shape.Tag is PositionInfo pos && pos.Type != PositionType.Absolute)
                {
                    var index = Property.IndexOf(pos);
                    pos = pos.WithType(PositionType.Absolute, Property.GetRequiredParent<ClipElement>().Length);
                    Property.UpdatePositionInfo(index, pos).Execute();
                    shape.Tag = pos;

                    // �A�C�R���ύX
                    var icon = menu1.Icon;
                    menu1.Icon = null!;
                    menu.Icon = icon;
                }
            }
        }

        // �L�[�t���[�����폜
        private void Remove_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu)
            {
                var shape = menu.FindLogicalAncestorOfType<Shape>();
                if (shape.Tag is PositionInfo pos)
                {
                    ViewModel.RemoveKeyFrameCommand.Execute(pos);
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}