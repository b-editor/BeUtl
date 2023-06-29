﻿using System.Collections.Specialized;
using System.Text.Json.Nodes;

using Beutl.Collections;
using Beutl.Framework;
using Beutl.Models;
using Beutl.ProjectSystem;
using Beutl.Services.PrimitiveImpls;
using Beutl.Operation;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Beutl.ViewModels.Tools;

public sealed class SourceOperatorsTabViewModel : IToolContext
{
    private readonly IDisposable _disposable0;
    private EditViewModel _editViewModel;
    private IDisposable? _disposable1;
    private Element? _oldLayer;

    public SourceOperatorsTabViewModel(EditViewModel editViewModel)
    {
        _editViewModel = editViewModel;
        Layer = editViewModel.SelectedObject
            .Select(x => x as Element)
            .ToReactiveProperty();

        _disposable0 = Layer.Subscribe(layer =>
        {
            if (_oldLayer != null)
            {
                SaveState(_oldLayer);
            }
            _oldLayer = layer;

            ClearItems();
            if (layer != null)
            {
                _disposable1?.Dispose();

                Items.AddRange(layer.Operation.Children.Select(x => new SourceOperatorViewModel(x, this)));
                _disposable1 = layer.Operation.Children.CollectionChangedAsObservable()
                    .Subscribe(e =>
                    {
                        static void RemoveItems(CoreList<SourceOperatorViewModel> items, int index, int count)
                        {
                            foreach (SourceOperatorViewModel item in items.GetMarshal().Value.Slice(index, count))
                            {
                                item?.Dispose();
                            }
                            items.RemoveRange(index, count);
                        }

                        switch (e.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                                Items.InsertRange(e.NewStartingIndex, e.NewItems!
                                    .Cast<SourceOperator>()
                                    .Select(x => new SourceOperatorViewModel(x, this)));
                                break;

                            case NotifyCollectionChangedAction.Move:
                                int newIndex = e.NewStartingIndex;
                                if (newIndex > e.OldStartingIndex)
                                {
                                    newIndex += e.OldItems!.Count;
                                }

                                Items.MoveRange(e.OldStartingIndex, e.OldItems!.Count, newIndex);
                                break;

                            case NotifyCollectionChangedAction.Replace:
                                RemoveItems(Items, e.OldStartingIndex, e.OldItems!.Count);
                                newIndex = e.NewStartingIndex;
                                if (newIndex > e.OldStartingIndex)
                                {
                                    newIndex -= e.OldItems!.Count;
                                }

                                Items.InsertRange(newIndex, e.NewItems!
                                    .Cast<SourceOperator>()
                                    .Select(x => new SourceOperatorViewModel(x, this)));
                                break;

                            case NotifyCollectionChangedAction.Remove:
                                RemoveItems(Items, e.OldStartingIndex, e.OldItems!.Count);
                                break;

                            case NotifyCollectionChangedAction.Reset:
                                ClearItems();
                                break;
                        }
                    });
                //_disposable1 = layer.Operators.ForEachItem(
                //    (idx, item) => Items.Insert(idx, new StreamOperatorViewModel(item)),
                //    (idx, _) =>
                //    {
                //        Items[idx].Dispose();
                //        Items.RemoveAt(idx);
                //    },
                //    () => ClearItems());

                RestoreState(layer);
            }
        });
    }

    public string Header => Strings.SourceOperators;

    public Action<SourceOperator>? RequestScroll { get; set; }

    public ReactiveProperty<Element?> Layer { get; }

    public CoreList<SourceOperatorViewModel> Items { get; } = new();

    public ToolTabExtension Extension => SourceOperatorsTabExtension.Instance;

    public IReactiveProperty<bool> IsSelected { get; } = new ReactivePropertySlim<bool>();

    public ToolTabExtension.TabPlacement Placement => ToolTabExtension.TabPlacement.Right;

    public void ScrollTo(SourceOperator obj)
    {
        RequestScroll?.Invoke(obj);
    }

    public void Dispose()
    {
        if (Layer.Value != null)
        {
            SaveState(Layer.Value);
            Layer.Value = null;
        }
        _disposable0.Dispose();
        _disposable1?.Dispose();

        Layer.Dispose();
        _editViewModel = null!;
        RequestScroll = null;
    }

    private static string ViewStateDirectory(Element layer)
    {
        string directory = Path.GetDirectoryName(layer.FileName)!;

        directory = Path.Combine(directory, Constants.BeutlFolder, Constants.ViewStateFolder);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return directory;
    }

    private void SaveState(Element layer)
    {
        string viewStateDir = ViewStateDirectory(layer);
        var json = new JsonArray();
        foreach (SourceOperatorViewModel? item in Items.GetMarshal().Value)
        {
            json.Add(item?.SaveState());
        }

        json.JsonSave(Path.Combine(viewStateDir, $"{Path.GetFileNameWithoutExtension(layer.FileName)}.operators.config"));
    }

    private void RestoreState(Element layer)
    {
        string viewStateDir = ViewStateDirectory(layer);
        string viewStateFile = Path.Combine(viewStateDir, $"{Path.GetFileNameWithoutExtension(layer.FileName)}.operators.config");

        if (File.Exists(viewStateFile))
        {
            using var stream = new FileStream(viewStateFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var json = JsonNode.Parse(stream);
            if (json is JsonArray array)
            {
                foreach ((JsonNode? item, SourceOperatorViewModel? op) in array.Zip(Items))
                {
                    if (item != null && op != null)
                    {
                        op.RestoreState(item);
                    }
                }
            }
        }
    }

    private void ClearItems()
    {
        foreach (SourceOperatorViewModel? item in Items.GetMarshal().Value)
        {
            item?.Dispose();
        }
        Items.Clear();
    }

    public void ReadFromJson(JsonObject json)
    {
        if (Layer.Value != null)
        {
            RestoreState(Layer.Value);
        }
    }

    public void WriteToJson(JsonObject json)
    {
        if (Layer.Value != null)
        {
            SaveState(Layer.Value);
        }
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(Element))
            return Layer.Value;

        return _editViewModel.GetService(serviceType);
    }
}
