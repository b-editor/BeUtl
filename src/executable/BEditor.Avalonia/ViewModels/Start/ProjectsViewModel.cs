﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using BEditor.Data;
using BEditor.Models;
using BEditor.Models.Start;
using BEditor.Properties;

using Microsoft.Extensions.Logging;

using Reactive.Bindings;

namespace BEditor.ViewModels.Start
{
    public sealed class ProjectsViewModel
    {
        private readonly BEditor.Settings _settings = BEditor.Settings.Default;

        public ProjectsViewModel()
        {
            Projects = new(_settings.RecentFiles
                .Where(i => Path.GetExtension(i) is ".bedit")
                .Select(i => new ProjectModel(
                    Path.GetFileNameWithoutExtension(i),
                    Path.Combine(Directory.GetParent(i)!.FullName, "thumbnail.png"),
                    i)));

            RemoveItem.Subscribe(item =>
            {
                Projects.Remove(item);
                _settings.RecentFiles.Remove(item.FileName);
                UpdateIsEmpty();
            });

            OpenItem.Subscribe(async item =>
            {
                if (IsLoading.Value) return;

                IsLoading.Value = true;
                var filename = item.FileName;

                try
                {
                    await Task.Run(() =>
                    {
                        var app = AppModel.Current;
                        app.Project?.Unload();
                        var project = Project.FromFile(filename, app);

                        if (project is null) return;

                        project.Load();

                        app.Project = project;
                        app.AppStatus = Status.Edit;

                        _settings.RecentFiles.Remove(filename);
                        _settings.RecentFiles.Add(filename);
                    });
                }
                catch (Exception e)
                {
                    var app = AppModel.Current;
                    app.Project = null;
                    app.AppStatus = Status.Idle;
                    ServicesLocator.Current.Logger.LogError("Failed to load project.", e);
                    await AppModel.Current.Message.DialogAsync(string.Format(Strings.FailedToLoad, Strings.Project), IMessage.IconType.Error);
                }

                IsLoading.Value = false;
            });

            AddToList.Subscribe(async () =>
            {
                var dialog = new OpenFileRecord
                {
                    Filters =
                    {
                        new(Strings.ProjectFile, new[]
                        {
                            "bedit"
                        })
                    }
                };

                if (await AppModel.Current.FileDialog.ShowOpenFileDialogAsync(dialog))
                {
                    _settings.RecentFiles.Remove(dialog.FileName);
                    _settings.RecentFiles.Add(dialog.FileName);
                    Projects.Add(new(
                        Path.GetFileNameWithoutExtension(dialog.FileName),
                        Path.Combine(Directory.GetParent(dialog.FileName)!.FullName, "thumbnail.png"),
                        dialog.FileName));

                    UpdateIsEmpty();
                }
            });

            UpdateIsEmpty();
        }

        public ReactivePropertySlim<bool> IsEmpty { get; } = new();

        public ReactivePropertySlim<bool> IsLoading { get; } = new();

        public AsyncReactiveCommand<ProjectModel> OpenItem { get; } = new();

        public ReactiveCommand AddToList { get; } = new();

        public ReactiveCommand<ProjectModel> RemoveItem { get; } = new();

        public ObservableCollection<ProjectModel> Projects { get; }

        private void UpdateIsEmpty()
        {
            IsEmpty.Value = Projects.Count is 0;
        }
    }
}