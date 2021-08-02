using System;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using BEditor.ViewModels.Dialogs;

namespace BEditor.Views.Dialogs
{
    public partial class OpenProjectPackage : FluentWindow
    {
        public OpenProjectPackage()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public void CloseClick(object s, RoutedEventArgs e)
        {
            Close(OpenProjectPackageViewModel.State.Close);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is OpenProjectPackageViewModel vm)
            {
                vm.Close.Subscribe(s => Dispatcher.UIThread.InvokeAsync(() => Close(s)));
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
