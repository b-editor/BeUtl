﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Xml.Linq;

using BEditor.Models;
using BEditor.ViewModels;
using BEditor.ViewModels.CustomControl;
using BEditor.ViewModels.PropertyControl;
using BEditor.Views;
using BEditor.Views.CustomControl;
using BEditor.Views.MessageContent;

using BEditor.Core.Data;
using BEditor.Core.Data.Property;
using BEditor.Core.Extensions.ViewCommand;

using MaterialDesignThemes.Wpf;

using Resources_ = BEditor.Core.Properties.Resources;
using System.Timers;
using BEditor.Models.Services;
using BEditor.Core.Service;
using BEditor.Core.Command;
using BEditor.Core.Data.Primitive.Properties;
using BEditor.Drawing;
using System.Globalization;
using System.Linq;

namespace BEditor
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            //CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            AppData.Current.Arguments = e.Args;


            #region ダークモード設定

            if (Settings.Default.UseDarkMode)
            {
                PaletteHelper paletteHelper = new PaletteHelper();
                ITheme theme = paletteHelper.GetTheme();

                theme.SetBaseTheme(Theme.Dark);

                paletteHelper.SetTheme(theme);
            }

            #endregion

            static void SetFont()
            {
                FontProperty.FontList.AddRange(
                    Settings.Default.IncludeFontDir
                        .Select(dir => Directory.GetFiles(dir))
                        .SelectMany(files => files)
                        .Where(file => Path.GetExtension(file) is ".ttf" or ".ttc" or ".otf")
                        .Select(file => new Font(file)));
            }

            static void SetColor()
            {
                var files = Directory.GetFiles(AppData.Current.Path + "\\user\\colors", "*.xml", SearchOption.AllDirectories);

                foreach (var file in files)
                {

                    // ファイルの読み込み
                    XDocument xml = XDocument.Load(file);


                    XElement xElement = xml.Root;
                    IEnumerable<XElement> cols = xElement.Elements("Color");

                    ObservableCollection<ColorListProperty> colors = new();

                    foreach (XElement col in cols)
                    {
                        string name = col.Attribute("Name")?.Value ?? "?";
                        byte red = byte.Parse(col.Attribute("Red")?.Value ?? "0");
                        byte green = byte.Parse(col.Attribute("Green")?.Value ?? "0");
                        byte blue = byte.Parse(col.Attribute("Blue")?.Value ?? "0");

                        colors.Add(new ColorListProperty(red, green, blue, name));
                    }

                    ColorPickerViewModel.ColorList.Add(new ColorList(colors, xElement.Attribute("Name")?.Value ?? "?"));
                }
            }

            SetFont();
            SetColor();

            Services.FileDialogService = new FileDialogService();

            Message.DialogFunc += (text, iconKind, types) =>
            {
                var control = new MessageUI(types, text, iconKind);
                var dialog = new NoneDialog(control);

                dialog.ShowDialog();

                return control.DialogResult;
            };
            Message.SnackberFunc += (text) => MainWindowViewModel.Current.MessageQueue.Enqueue(text);
        }

        public static (CustomTreeView, VirtualizingStackPanel) CreateTreeObject(ObjectElement obj)
        {
            CustomTreeView _expander = new CustomTreeView()
            {
                HeaderHeight = 35F
            };

            VirtualizingStackPanel stack = new VirtualizingStackPanel() { Margin = new Thickness(32.5, 0, 0, 0) };
            VirtualizingPanel.SetIsVirtualizing(stack, true);
            VirtualizingPanel.SetVirtualizationMode(stack, VirtualizationMode.Recycling);


            VirtualizingStackPanel header = new VirtualizingStackPanel() { Orientation = Orientation.Horizontal };
            VirtualizingPanel.SetIsVirtualizing(header, true);
            VirtualizingPanel.SetVirtualizationMode(header, VirtualizationMode.Recycling);

            _expander.Header = header;

            System.Windows.Controls.CheckBox checkBox = new System.Windows.Controls.CheckBox() { Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            var textBlock = new TextBlock() { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            header.Children.Add(checkBox);
            header.Children.Add(textBlock);


            #region コンテキストメニュー
            ContextMenu menuListBox = new ContextMenu();
            MenuItem Delete = new MenuItem();

            //削除項目の設定
            var menu = new VirtualizingStackPanel() { Orientation = Orientation.Horizontal };
            menu.Children.Add(new PackIcon() { Kind = PackIconKind.Delete, Margin = new Thickness(5, 0, 5, 0) });
            menu.Children.Add(new TextBlock() { Text = "削除", Margin = new Thickness(20, 0, 5, 0) });
            Delete.Header = menu;

            menuListBox.Items.Add(Delete);

            // 作成したコンテキストメニューをListBox1に設定
            _expander.ContextMenu = menuListBox;
            #endregion

            #region イベント
            checkBox.Click += (sender, e) =>
            {
                obj.CreateCheckCommand((bool)((CheckBox)sender).IsChecked).Execute();
            };

            #endregion

            #region Binding

            Binding isenablebinding = new Binding("IsEnabled") { Mode = BindingMode.OneWay, Source = obj };
            checkBox.SetBinding(System.Windows.Controls.CheckBox.IsCheckedProperty, isenablebinding);

            Binding textbinding = new Binding("Name") { Mode = BindingMode.OneTime, Source = obj };
            textBlock.SetBinding(TextBlock.TextProperty, textbinding);

            Binding isExpandedbinding = new Binding("IsExpanded") { Mode = BindingMode.TwoWay, Source = obj };
            _expander.SetBinding(CustomTreeView.IsExpandedProperty, isExpandedbinding);

            _expander.SetResourceReference(CustomTreeView.HeaderColorProperty, "MaterialDesignBody");

            #endregion

            _expander.Content = stack;

            return (_expander, stack);
        }
        public static (CustomTreeView, VirtualizingStackPanel) CreateTreeEffect(EffectElement effect)
        {
            var data = effect.Parent;

            CustomTreeView _expander = new CustomTreeView() { HeaderHeight = 35F };

            VirtualizingStackPanel stack = new VirtualizingStackPanel() { Margin = new Thickness(32, 0, 0, 0) };
            VirtualizingPanel.SetIsVirtualizing(stack, true);
            VirtualizingPanel.SetVirtualizationMode(stack, VirtualizationMode.Recycling);

            #region Header

            VirtualizingStackPanel header = new VirtualizingStackPanel() { Orientation = Orientation.Horizontal };
            VirtualizingPanel.SetIsVirtualizing(header, true);
            VirtualizingPanel.SetVirtualizationMode(header, VirtualizationMode.Recycling);

            _expander.Header = header;

            System.Windows.Controls.CheckBox checkBox = new System.Windows.Controls.CheckBox() { Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            Button upbutton = new Button() { Content = new PackIcon() { Kind = PackIconKind.ChevronUp }, Margin = new Thickness(5, 0, 0, 0), Background = null, BorderBrush = null, VerticalAlignment = VerticalAlignment.Center };
            Button downbutton = new Button() { Content = new PackIcon() { Kind = PackIconKind.ChevronDown }, Margin = new Thickness(0, 0, 5, 0), Background = null, BorderBrush = null, VerticalAlignment = VerticalAlignment.Center };
            TextBlock textBlock = new TextBlock() { Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            header.Children.Add(checkBox);
            header.Children.Add(upbutton);
            header.Children.Add(downbutton);
            header.Children.Add(textBlock);

            #endregion


            #region コンテキストメニュー
            ContextMenu menuListBox = new ContextMenu();
            MenuItem Delete = new MenuItem();

            //削除項目の設定
            var menu = new VirtualizingStackPanel() { Orientation = Orientation.Horizontal };
            menu.Children.Add(new PackIcon() { Kind = PackIconKind.Delete, Margin = new Thickness(5, 0, 5, 0) });
            menu.Children.Add(new TextBlock() { Text = "削除", Margin = new Thickness(20, 0, 5, 0) });
            Delete.Header = menu;

            menuListBox.Items.Add(Delete);

            // 作成したコンテキストメニューをListBox1に設定
            _expander.ContextMenu = menuListBox;
            #endregion

            #region イベント

            checkBox.Click += (sender, e) => effect.CreateCheckCommand((bool)((CheckBox)sender).IsChecked).Execute();

            upbutton.Click += (sender, e) => effect.CreateUpCommand().Execute();

            downbutton.Click += (sender, e) => effect.CreateDownCommand().Execute();

            Delete.Click += (sender, e) => effect.Parent.CreateRemoveCommand(effect).Execute();

            #endregion

            #region Binding

            Binding isenablebinding = new Binding("IsEnabled") { Mode = BindingMode.OneWay, Source = effect };
            checkBox.SetBinding(System.Windows.Controls.CheckBox.IsCheckedProperty, isenablebinding);

            Binding textbinding = new Binding("Name") { Mode = BindingMode.OneTime, Source = effect };
            textBlock.SetBinding(TextBlock.TextProperty, textbinding);

            Binding isExpandedbinding = new Binding("IsExpanded") { Mode = BindingMode.TwoWay, Source = effect };
            _expander.SetBinding(CustomTreeView.IsExpandedProperty, isExpandedbinding);

            _expander.SetResourceReference(CustomTreeView.HeaderColorProperty, "MaterialDesignBody");

            #endregion

            _expander.Content = stack;

            return (_expander, stack);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
