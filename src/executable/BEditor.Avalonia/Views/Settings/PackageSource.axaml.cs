using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BEditor.Views.Settings
{
    public sealed class PackageSource : UserControl
    {
        public PackageSource()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}