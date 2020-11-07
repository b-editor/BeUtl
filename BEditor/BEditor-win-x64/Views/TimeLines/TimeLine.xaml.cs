﻿using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

using BEditor.Models.Extension;
using BEditor.Models.Settings;
using BEditor.ViewModels;
using BEditor.ViewModels.TimeLines;

using BEditor.Core.Data.ObjectData;
using BEditor.Core.Data.ProjectData;

using MaterialDesignThemes.Wpf;

using Microsoft.Xaml.Behaviors;

using Resource = BEditor.Core.Properties.Resources;

namespace BEditor.Views.TimeLines {
    /// <summary>
    /// TimeLine.xaml の相互作用ロジック
    /// </summary>
    public partial class TimeLine : UserControl {
        private readonly Scene Scene;
        private readonly TimeLineViewModel TimeLineViewModel;

        public TimeLine(Scene scene) {
            Scene = scene;
            this.DataContext = this.TimeLineViewModel = scene.GetCreateTimeLineViewModel();

            InitializeComponent();

            ContextMenu CreateMenu(int layer) {
                ContextMenu contextMenu = new ContextMenu();

                #region 削除

                MenuItem Delete = new MenuItem();

                var deletemenu = new VirtualizingStackPanel() { Orientation = Orientation.Horizontal };
                deletemenu.Children.Add(new PackIcon() { Kind = PackIconKind.Delete, Margin = new Thickness(5, 0, 5, 0) });
                deletemenu.Children.Add(new TextBlock() { Text = Resource.Remove, Margin = new Thickness(20, 0, 5, 0) });
                Delete.Header = deletemenu;

                contextMenu.Items.Add(Delete);
                #endregion

                #region
                contextMenu.Items.Add(new Separator());
                #endregion

                #region 非表示
                MenuItem Hidden = new MenuItem();

                var hidemenu = new VirtualizingStackPanel() {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                hidemenu.Children.Add(new TextBlock() { Text = Resource.Hide, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center });
                var toggle = new ToggleButton() { IsChecked = true };
                hidemenu.Children.Add(toggle);
                hidemenu.Children.Add(new TextBlock() { Text = Resource.Show, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });

                Hidden.Header = hidemenu;

                toggle.Click += (_, _) => {
                    if ((bool)toggle.IsChecked) {
                        Scene.HideLayer.Remove(layer);
                    }
                    else {
                        Scene.HideLayer.Add(layer);
                    }
                };

                if (Scene.HideLayer.Exists(x => x == layer)) {
                    toggle.IsChecked = false;
                }

                contextMenu.Items.Add(Hidden);

                #endregion

                return contextMenu;
            }


            //レイヤー名追加for
            for (int l = 1; l < 100; l++) {
                Binding binding2 = new Binding("TrackHeight");

                Grid track = new Grid();

                Grid grid = new Grid() {
                    Margin = new Thickness(0, 1, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    AllowDrop = true,
                    VerticalAlignment = VerticalAlignment.Top
                };

                grid.SetValue(AttachmentProperty.IntProperty, l);
                grid.SetBinding(WidthProperty, new Binding("TrackWidth.Value") { Mode = BindingMode.OneWay });
                grid.SetResourceReference(BackgroundProperty, "MaterialDesignCardBackground");

                #region Eventの設定

                var triggers = Interaction.GetTriggers(grid);

                //MouseDown
                triggers.Add(CommandTool.CreateEvent("MouseDown", TimeLineViewModel.LayerSelectCommand, grid));

                //PreviewDrop
                triggers.Add(CommandTool.CreateEvent("PreviewDrop", TimeLineViewModel.LayerDropCommand, EventArgsConverter.Converter, grid));

                //MouseMove
                triggers.Add(CommandTool.CreateEvent("MouseMove", TimeLineViewModel.LayerMoveCommand, grid));

                //PreviewDragOver
                triggers.Add(CommandTool.CreateEvent("PreviewDragOver", TimeLineViewModel.LayerDragOverCommand, EventArgsConverter.Converter, grid));

                //MouseLeftButtonDown
                //triggers.Add(CommandTool.CreateEvent("MouseLeftButtonDown", TimeLineViewModel.TimeLineMouseLeftDownCommand, new MousePositionConverter(), grid));

                #endregion

                grid.SetBinding(HeightProperty, binding2);


                track.Children.Add(grid);
                Layer.Children.Add(track);


                Binding binding = new Binding("ActualHeight") {
                    Source = track
                };

                #region Labelの追加

                Grid row2 = new Grid();

                row2.ContextMenu = CreateMenu(l);

                row2.SetBinding(Grid.HeightProperty, binding);

                Label textBlock = new() {
                    VerticalAlignment = VerticalAlignment.Top,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 14,
                    Content = l.ToString(),
                    Padding = new Thickness(0, 0, 0, 0)
                };
                textBlock.SetBinding(HeightProperty, binding2);

                row2.Children.Add(textBlock);

                LayerLabel.Children.Add(row2);

                #endregion
            }

            ScrollLabel.ScrollToVerticalOffset(Scene.TimeLineVerticalOffset);
            ScrollLine.ScrollToVerticalOffset(Scene.TimeLineVerticalOffset);
            ScrollLine.ScrollToHorizontalOffset(Scene.TimeLineHorizonOffset);

            var linetrigger = Interaction.GetTriggers(ScrollLine);
            linetrigger.Add(CommandTool.CreateEvent("ScrollChanged", TimeLineViewModel.ScrollLineCommand));

            var labeltrigger = Interaction.GetTriggers(ScrollLabel);
            labeltrigger.Add(CommandTool.CreateEvent("ScrollChanged", TimeLineViewModel.ScrollLabelCommand));

            TimeLineViewModel.ResetScale = (zoom, max, rate) => AddScale(zoom, max, rate);
            TimeLineViewModel.ClipLayerMoveCommand = (data, layer) => {
                var vm = data.GetCreateClipViewModel();
                var from = vm.Row;
                vm.Row = layer;

                App.Current?.Dispatcher?.Invoke(() => {
                    Grid fromgrid = (Grid)Layer.Children[from];//移動もと

                    Grid togrid = (Grid)Layer.Children[layer];


                    fromgrid.Children.Remove(data.GetCreateClipView());
                    togrid.Children.Add(data.GetCreateClipView());
                });
            };
            TimeLineViewModel.GetLayerMousePosition = () => Mouse.GetPosition(Layer).ToMedia();

            TimeLineViewModel.ViewLoaded = true;
            TimeLineViewModel.TimeLineLoaded(list => {
                for (int index = 0; index < list.Count; index++) {
                    var info = list[index];


                    Grid grid = (Grid)Layer.Children[info.Layer];

                    grid.Children.Add(info.GetCreateClipView());
                }
                Layer.Focus();
            });

            Scene.Datas.CollectionChanged += (s, e) => {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add) {
                    var info = Scene.Datas[e.NewStartingIndex];

                    Grid grid = (Grid)Layer.Children[info.Layer];

                    grid.Children.Add(info.GetCreateClipView());
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove) {
                    ClipData info = (ClipData)e.OldItems[e.OldStartingIndex];

                    Grid grid = (Grid)Layer.Children[info.Layer];
                                        
                    grid.Children.Remove(info.GetCreateClipView());
                }
            };
        }


        #region Scrollbarの移動量を変更

        private void ScrollLine_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {

            if (Keyboard.IsKeyDown(Key.LeftCtrl)) {
                if (!(Scene.TimeLineZoom > 100 || Scene.TimeLineZoom < 1)) {
                    Scene.TimeLineZoom += (e.Delta / 120) * 5;
                }
            }
            else {
                ScrollViewer scrollviewer = (ScrollViewer)sender;
                if (e.Delta > 0) {
                    for (int i = 0; i < 4; i++) {
                        scrollviewer.LineLeft();
                    }
                }
                else {
                    for (int i = 0; i < 4; i++) {
                        scrollviewer.LineRight();
                    }
                }
            }

            e.Handled = true;
        }

        #endregion

        /// <summary>
        /// 目盛りを追加するメソッド
        /// </summary>
        /// <param name="zoom">拡大率 1 - 100</param>
        /// <param name="max">最大フレーム</param>
        /// <param name="rate">フレームレート</param>
        private void AddScale(float zoom, int max, int rate) => App.Current?.Dispatcher?.Invoke(() => {
            double ToPixel(int frame) => Setting.WidthOf1Frame * (zoom / 100) * frame;

            scale.Children.Clear();

            //iは秒数
            for (int i = 0; i < (max / rate); i++) {
                Border border = new Border {
                    Width = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,

                    Margin = new Thickness(ToPixel(i * rate - 1), 5, 0, 0)
                };
                border.SetResourceReference(BackgroundProperty, "MaterialDesignBody");
                scale.Children.Add(border);

                if (zoom <= 100 && zoom >= 75) {
                    for (int m = 1; m < rate; m++) {
                        Border border2 = new Border {
                            Width = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,

                            Margin = new Thickness(ToPixel(i * rate - 1 + m), 15, 0, 0)
                        };

                        border2.SetResourceReference(BackgroundProperty, "MaterialDesignBodyLight");
                        scale.Children.Add(border2);
                    }
                }
                else if (zoom < 75 && zoom >= 50) {
                    for (int m = 1; m < rate / 2; m++) {
                        Border border2 = new Border {
                            Width = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,

                            Margin = new Thickness(ToPixel(i * rate - 1 + m * 2), 15, 0, 0)
                        };

                        border2.SetResourceReference(BackgroundProperty, "MaterialDesignBodyLight");
                        scale.Children.Add(border2);
                    }
                }
                else if (zoom < 50 && zoom >= 25) {
                    for (int m = 1; m < rate / 4; m++) {
                        Border border2 = new Border {
                            Width = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,

                            Margin = new Thickness(ToPixel(i * rate - 1 + m * 4), 15, 0, 0)
                        };

                        border2.SetResourceReference(BackgroundProperty, "MaterialDesignBodyLight");
                        scale.Children.Add(border2);
                    }
                }
                else {
                    for (int m = 1; m < rate / 10; m++) {
                        Border border2 = new Border {
                            Width = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,

                            Margin = new Thickness(ToPixel(i * rate - 1 + m * 10), 15, 0, 0)
                        };

                        border2.SetResourceReference(BackgroundProperty, "MaterialDesignBodyLight");
                        scale.Children.Add(border2);
                    }
                }
            }
        });

        private void ScrollLine_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            Task.Run(() => {
                Scene.TimeLineHorizonOffset = ScrollLine.HorizontalOffset;
                Scene.TimeLineVerticalOffset = ScrollLine.VerticalOffset;
            });
        }
    }
}
