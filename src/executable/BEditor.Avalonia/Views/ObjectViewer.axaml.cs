using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using BEditor.Data;
using BEditor.Models;
using BEditor.Properties;
using BEditor.ViewModels.DialogContent;
using BEditor.Views.DialogContent;

using static BEditor.IMessage;

namespace BEditor.Views
{
    public partial class ObjectViewer : UserControl
    {
        private static IMessage Message => AppModel.Current.Message;

        public ObjectViewer()
        {
            InitializeComponent();
        }

        public static IEnumerable<string> Empty { get; } = Enumerable.Empty<string>();

        public async void CopyID_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<TreeView>("TreeView").SelectedItem is IEditingObject obj)
            {
                await Application.Current.Clipboard.SetTextAsync(obj.Id.ToString());
            }
            else
            {
                await Message.DialogAsync(string.Format(Strings.ErrorObjectViewer2, nameof(IEditingObject)));
            }
        }

        private Scene? GetScene()
        {
            if (this.FindControl<TreeView>("TreeView").SelectedItem is IChild<object> obj) return obj.GetParent<Scene>();
            else return AppModel.Current.Project.CurrentScene;
        }

        private ClipElement? GetClip()
        {
            if (this.FindControl<TreeView>("TreeView").SelectedItem is IChild<object> obj) return obj.GetParent<ClipElement>();
            else return AppModel.Current.Project.CurrentScene.SelectItem;
        }

        private EffectElement? GetEffect()
        {
            if (this.FindControl<TreeView>("TreeView").SelectedItem is IChild<object> obj) return obj.GetParent<EffectElement>();
            else return null;
        }

        public async void DeleteScene(object sender, RoutedEventArgs e)
        {
            try
            {
                var scene = GetScene();
                if (scene is null) return;
                if (scene is { SceneName: "root" })
                {
                    Message.Snackbar("RootScene �͍폜���邱�Ƃ��ł��܂���", string.Empty);
                    return;
                }

                if (await Message.DialogAsync(
                    Strings.CommandQ1,
                    types: new ButtonType[] { ButtonType.Yes, ButtonType.No }) == ButtonType.Yes)
                {
                    scene.Parent!.CurrentScene = scene.Parent!.SceneList[0];
                    scene.Parent.SceneList.Remove(scene);
                    scene.Unload();

                    scene.ClearDisposable();
                }
            }
            catch (IndexOutOfRangeException)
            {
                Message.Snackbar(string.Format(Strings.ErrorObjectViewer1, nameof(Scene)), string.Empty, IconType.Error);
            }
        }

        public void RemoveClip(object sender, RoutedEventArgs e)
        {
            try
            {
                var clip = GetClip();
                if (clip is null) return;
                clip.Parent.RemoveClip(clip).Execute();
            }
            catch (IndexOutOfRangeException)
            {
                Message.Snackbar(string.Format(Strings.ErrorObjectViewer1, nameof(ClipElement)), string.Empty, IconType.Error);
            }
        }

        public void RemoveEffect(object sender, RoutedEventArgs e)
        {
            try
            {
                var effect = GetEffect();
                if (effect is null) return;
                effect.Parent!.RemoveEffect(effect).Execute();
            }
            catch (IndexOutOfRangeException)
            {
                Message.Snackbar(string.Format(Strings.ErrorObjectViewer1, nameof(EffectElement)), string.Empty, IconType.Error);
            }
        }

        public async void CreateScene(object s, RoutedEventArgs e)
        {
            if (VisualRoot is Window window)
            {
                var dialog = new CreateScene { DataContext = new CreateSceneViewModel() };
                await dialog.ShowDialog(window);
            }
        }

        public async void CreateClip(object s, RoutedEventArgs e)
        {
            if (VisualRoot is Window window)
            {
                var vm = new CreateClipViewModel();
                var guess = GetScene();
                if (guess is not null) vm.Scene.Value = guess;

                var dialog = new CreateClip { DataContext = vm };
                await dialog.ShowDialog(window);
            }
        }

        public async void AddEffect(object s, RoutedEventArgs e)
        {
            if (VisualRoot is Window window)
            {
                var vm = new AddEffectViewModel();
                var guess = GetClip();
                if (guess is not null) vm.ClipId.Value = guess.Id.ToString();

                var dialog = new AddEffect { DataContext = vm };
                await dialog.ShowDialog(window);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}