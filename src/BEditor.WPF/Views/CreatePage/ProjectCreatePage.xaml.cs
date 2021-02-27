﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace BEditor.Views.CreatePage
{
    /// <summary>
    /// CreateProjectPage.xaml の相互作用ロジック
    /// </summary>
    public sealed partial class ProjectCreatePage : UserControl
    {
        public ProjectCreatePage()
        {
            InitializeComponent();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Window.GetWindow(this)?.Close();
        }
    }
}
