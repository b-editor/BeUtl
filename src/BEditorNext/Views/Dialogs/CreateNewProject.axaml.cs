using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

using BEditorNext.ViewModels.Dialogs;

using FluentAvalonia.UI.Controls;

namespace BEditorNext.Views.Dialogs;

public sealed partial class CreateNewProject : ContentDialog, IStyleable
{
    private IDisposable? _sBtnBinding;

    public CreateNewProject()
    {
        InitializeComponent();
    }

    Type IStyleable.StyleKey => typeof(ContentDialog);

    // 戻る
    protected override void OnPrimaryButtonClick(ContentDialogButtonClickEventArgs args)
    {
        base.OnPrimaryButtonClick(args);
        if (carousel.SelectedIndex == 1)
        {
            args.Cancel = true;
            // '戻る'を無効化
            IsPrimaryButtonEnabled = false;
            // IsSecondaryButtonEnabledのバインド解除
            _sBtnBinding?.Dispose();
            // '新規作成'を'次へ'に変更
            SecondaryButtonText = (string?)Application.Current.FindResource("NextString") ?? string.Empty;
            // '次へ'を有効化
            IsSecondaryButtonEnabled = true;
            carousel.Previous();
        }
    }

    // '次へ' or '新規作成'
    protected override void OnSecondaryButtonClick(ContentDialogButtonClickEventArgs args)
    {
        base.OnSecondaryButtonClick(args);
        if (DataContext is not CreateNewProjectViewModel vm) return;

        if (carousel.SelectedIndex == 1)
        {
            vm.Create.Execute();
        }
        else
        {
            args.Cancel = true;

            // '戻る'を表示
            IsPrimaryButtonEnabled = true;
            // IsSecondaryButtonEnabledとCanCreateをバインド
            _sBtnBinding = this.Bind(IsSecondaryButtonEnabledProperty, vm.CanCreate);
            // '次へ'を'新規作成に変更'
            SecondaryButtonText = (string?)Application.Current.FindResource("CreateNewString") ?? string.Empty;
            carousel.Next();
        }
    }

    // 場所を選択
    private async void PickLocation(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CreateNewProjectViewModel vm && VisualRoot is Window parent)
        {
            var picker = new OpenFolderDialog();

            string? result = await picker.ShowAsync(parent);

            if (result != null)
            {
                vm.Location.Value = result;
            }
        }
    }
}
