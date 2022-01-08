﻿using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BEditorNext.ProjectSystem;

// Todo: IResourceProviderを実装
public class Project : Element, ITopLevel, IStorable
{
    public static readonly CoreProperty<Scene?> SelectedSceneProperty;
    public static readonly CoreProperty<Version> AppVersionProperty;
    public static readonly CoreProperty<Version> MinimumAppVersionProperty;
    public static readonly CoreProperty<int> FrameRateProperty;
    public static readonly CoreProperty<int> SampleRateProperty;
    private string? _rootDirectory;
    private string? _fileName;
    private Scene? _selectedScene;
    private int _frameRate = 30;
    private int _sampleRate = 44100;

    static Project()
    {
        SelectedSceneProperty = ConfigureProperty<Scene?, Project>(nameof(SelectedScene))
            .Accessor(o => o.SelectedScene, (o, v) => o.SelectedScene = v)
            .Observability(PropertyObservability.ChangingAndChanged)
            .Register();

        AppVersionProperty = ConfigureProperty<Version, Project>(nameof(AppVersion))
            .Accessor(o => o.AppVersion)
            .Register();

        MinimumAppVersionProperty = ConfigureProperty<Version, Project>(nameof(MinimumAppVersion))
            .Accessor(o => o.MinimumAppVersion)
            .DefaultValue(new Version(0, 3))
            .Register();

        FrameRateProperty = ConfigureProperty<int, Project>(nameof(FrameRate))
            .Accessor(o => o.FrameRate, (o, v) => o.FrameRate = v)
            .Observability(PropertyObservability.ChangingAndChanged)
            .DefaultValue(30)
            .JsonName("framerate")
            .Register();

        SampleRateProperty = ConfigureProperty<int, Project>(nameof(SampleRate))
            .Accessor(o => o.SampleRate, (o, v) => o.SampleRate = v)
            .Observability(PropertyObservability.ChangingAndChanged)
            .DefaultValue(44100)
            .JsonName("samplerate")
            .Register();
    }

    public Project()
    {
        MinimumAppVersion = new Version(0, 3);
    }

    public Project(int framerate, int samplerate)
        : this()
    {
        FrameRate = framerate;
        SampleRate = samplerate;
    }

    public Scene? SelectedScene
    {
        get => _selectedScene;
        set => SetAndRaise(SelectedSceneProperty, ref _selectedScene, value);
    }

    public IEnumerable<Scene> Scenes => Children.OfType<Scene>();

    public string RootDirectory => _rootDirectory ?? throw new Exception("The file name is not set.");

    public string FileName => _fileName ?? throw new Exception("The file name is not set.");

    public Version AppVersion { get; private set; } = Assembly.GetEntryAssembly()!.GetName().Version ?? new Version();

    public Version MinimumAppVersion { get; private set; }

    public DateTime LastSavedTime { get; private set; }

    public int FrameRate
    {
        get => _frameRate;
        private set => SetAndRaise(FrameRateProperty, ref _frameRate, value);
    }

    public int SampleRate
    {
        get => _sampleRate;
        private set => SetAndRaise(SampleRateProperty, ref _sampleRate, value);
    }

    public void Restore(string filename)
    {
        _fileName = filename;
        _rootDirectory = Path.GetDirectoryName(filename);
        LastSavedTime = DateTime.Now;

        using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        var node = JsonNode.Parse(stream);

        if (node != null)
        {
            FromJson(node);
        }
    }

    public void Save(string filename)
    {
        _fileName = filename;
        _rootDirectory = Path.GetDirectoryName(filename);
        LastSavedTime = DateTime.Now;

        if (_rootDirectory != null && !Directory.Exists(_rootDirectory))
        {
            Directory.CreateDirectory(_rootDirectory);
        }

        using var stream = new FileStream(filename, FileMode.Create);
        using var writer = new Utf8JsonWriter(stream, JsonHelper.WriterOptions);

        ToJson().WriteTo(writer, JsonHelper.SerializerOptions);
    }

    public override void FromJson(JsonNode json)
    {
        base.FromJson(json);

        if (json is JsonObject jobject)
        {
            if (jobject.TryGetPropertyValue("appVersion", out JsonNode? versionNode) &&
                versionNode!.AsValue().TryGetValue(out Version? version))
            {
                AppVersion = version;
            }

            if (jobject.TryGetPropertyValue("minAppVersion", out JsonNode? minVersionNode) &&
                minVersionNode!.AsValue().TryGetValue(out Version? minVersion))
            {
                MinimumAppVersion = minVersion;
            }

            if (jobject.TryGetPropertyValue("scenes", out JsonNode? scenesNode))
            {
                SyncronizeScenes(scenesNode!.AsArray()
                    .Select(i => (string)i!));
            }

            //選択されているシーン
            if (jobject.TryGetPropertyValue("selectedScene", out JsonNode? selectedSceneNode))
            {
                string? selectedScene = (string?)selectedSceneNode;

                if (selectedScene != null)
                {
                    selectedScene = Path.GetFullPath(selectedScene, RootDirectory);
                    foreach (Scene item in Scenes)
                    {
                        if (item.FileName == selectedScene)
                        {
                            SelectedScene = item;
                        }
                    }
                }
            }
        }
    }

    public override JsonNode ToJson()
    {
        JsonNode node = base.ToJson();

        if (node is JsonObject jobject)
        {
            jobject["appVersion"] = JsonValue.Create(AppVersion);
            jobject["minAppVersion"] = JsonValue.Create(MinimumAppVersion);

            var scenes = new JsonArray();
            foreach (Scene item in Scenes)
            {
                string path = Path.GetRelativePath(RootDirectory, item.FileName).Replace('\\', '/');
                var value = JsonValue.Create(path);
                scenes.Add(value);

                if (SelectedScene == item)
                {
                    jobject["selectedScene"] = value;
                }
            }

            jobject["scenes"] = scenes;
        }

        return node;
    }

    private void SyncronizeScenes(IEnumerable<string> pathToScene)
    {
        pathToScene = pathToScene.Select(x => Path.GetFullPath(x, RootDirectory)).ToArray();

        // 削除するシーン
        IEnumerable<Scene> toRemoveScenes = Scenes.ExceptBy(pathToScene, x => x.FileName);
        // 追加するシーン
        IEnumerable<string> toAddScenes = pathToScene.Except(Scenes.Select(x => x.FileName));

        foreach (Scene item in toRemoveScenes)
        {
            Children.Remove(item);
        }

        foreach (string item in toAddScenes)
        {
            var scn = new Scene();
            scn.Restore(item);

            Children.Add(scn);
        }
    }
}
