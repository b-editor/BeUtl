﻿using System.Reactive.Subjects;
using Beutl.Media;
using Beutl.Media.Pixel;
using Beutl.ProjectSystem;
using Beutl.Rendering;

namespace Beutl.Models;

public class FrameProviderImpl(Scene scene, Rational rate, SceneRenderer renderer, Subject<TimeSpan> progress)
    : IFrameProvider
{
    public long FrameCount => (long)(scene.Duration.TotalSeconds * rate.ToDouble());

    public Rational FrameRate => rate;

    private Bitmap<Bgra8888> RenderCore(TimeSpan time)
    {
        int retry = 0;
    Retry:
        if (renderer.Render(time))
        {
            return renderer.Snapshot();
        }

        if (retry > 3)
            throw new Exception("Renderer.RenderがFalseでした。他にこのシーンを使用していないか確認してください。");

        retry++;
        goto Retry;
    }

    public async ValueTask<Bitmap<Bgra8888>> RenderFrame(long frame)
    {
        var time = TimeSpan.FromSeconds(frame / rate.ToDouble());
        try
        {
            if (RenderThread.Dispatcher.CheckAccess())
            {
                return RenderCore(time);
            }
            else
            {
                return await RenderThread.Dispatcher.InvokeAsync(() => RenderCore(time));
            }
        }
        finally
        {
            progress.OnNext(time);
        }
    }
}
