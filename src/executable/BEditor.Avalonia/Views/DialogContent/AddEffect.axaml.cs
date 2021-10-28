using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using BEditor.Controls;

namespace BEditor.Views.DialogContent
{
    public sealed class AddEffect : FluentWindow
    {
        public AddEffect()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public void CloseClick(object s, RoutedEventArgs e)
        {
            Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}