﻿using Beutl.Audio;
using Beutl.Audio.Effects;
using Beutl.Graphics;
using Beutl.Graphics.Effects;
using Beutl.Graphics.Transformation;
using Beutl.Language;
using Beutl.Media;
using Beutl.NodeTree.Nodes.Transform;
using Beutl.Operation;
using Beutl.Services;

namespace Beutl.Operators;

public static class LibraryRegistrar
{
    public static void RegisterAll()
    {
        LibraryService.Current
            .AddMultiple("Scene3D", m => m
                .BindSourceOperator<Source.Scene3DOperator>()
            );

        LibraryService.Current
            .AddMultiple(Strings.Ellipse, m => m
                .BindSourceOperator<Source.EllipseOperator>()
                .BindDrawable<Graphics.Shapes.EllipseShape>()
                .BindNode<NodeTree.Nodes.Geometry.EllipseGeometryNode>()
                .BindGeometry<EllipseGeometry>()
            );

        LibraryService.Current
            .AddMultiple(Strings.Rectangle, m => m
                .BindSourceOperator<Source.RectOperator>()
                .BindDrawable<Graphics.Shapes.RectShape>()
                .BindNode<NodeTree.Nodes.Geometry.RectGeometryNode>()
                .BindGeometry<RectGeometry>()
            );

        LibraryService.Current
            .AddMultiple(Strings.RoundedRect, m => m
                .BindSourceOperator<Source.RoundedRectOperator>()
                .BindDrawable<Graphics.Shapes.RoundedRectShape>()
                .BindNode<NodeTree.Nodes.Geometry.RoundedRectGeometryNode>()
                .BindGeometry<RoundedRectGeometry>()
            );

        LibraryService.Current
            .AddMultiple(Strings.GeometryShape, m => m
                .BindSourceOperator<Source.GeometryOperator>()
                .BindDrawable<Graphics.Shapes.GeometryShape>()
            );

        LibraryService.Current
            .AddMultiple(Strings.Text, m => m
                .BindSourceOperator<Source.TextBlockOperator>()
                .BindDrawable<Graphics.Shapes.TextBlock>()
            );

        LibraryService.Current
            .AddMultiple(Strings.Video, m => m
                .BindSourceOperator<Source.SourceVideoOperator>()
                .BindDrawable<SourceVideo>()
            );

        LibraryService.Current
            .AddMultiple(Strings.Image, m => m
                .BindSourceOperator<Source.SourceImageOperator>()
                .BindDrawable<SourceImage>()
            );

        LibraryService.Current
            .AddMultiple(Strings.Backdrop, m => m
                .BindSourceOperator<Source.SourceBackdropOperator>()
                .BindDrawable<SourceBackdrop>()
            );

        LibraryService.Current
            .AddMultiple(Strings.Sound, m => m
                .BindSourceOperator<Source.SourceSoundOperator>()
                .BindSound<SourceSound>()
            );

        LibraryService.Current
            .AddMultiple("下位N個の要素を取得", m => m
                .BindSourceOperator<TakeAfterOperator>()
            );

        LibraryService.Current
            .AddMultiple("デコレーター", m => m
                .BindSourceOperator<DecorateOperator>()
            );

        LibraryService.Current
            .AddMultiple("グループ", m => m
                .BindSourceOperator<GroupOperator>()
            );

        LibraryService.Current
            .RegisterGroup(Strings.Transform, g => g
                .AddMultiple(Strings.Translate, m => m
                    .BindTransform<TranslateTransform>()
                    .BindNode<TranslateTransformNode>()
                )

                .AddMultiple(Strings.Skew, m => m
                    .BindTransform<SkewTransform>()
                    .BindNode<SkewTransformNode>()
                )

                .AddMultiple(Strings.Scale, m => m
                    .BindTransform<ScaleTransform>()
                    .BindNode<ScaleTransformNode>()
                )

                .AddMultiple(Strings.Rotation, m => m
                    .BindTransform<RotationTransform>()
                    .BindNode<RotationTransformNode>()
                )

                .AddMultiple(Strings.Rotation3D, m => m
                    .BindTransform<Rotation3DTransform>()
                    .BindNode<Rotation3DTransformNode>()
                )
            );

        LibraryService.Current
            .RegisterGroup(Strings.FilterEffect, g => g
                .AddFilterEffect<Blur>(Strings.Blur)

                .AddFilterEffect<DropShadow>(Strings.DropShadow)

                .AddFilterEffect<InnerShadow>(Strings.InnerShadow)

                .AddFilterEffect<FlatShadow>(Strings.FlatShadow)

                .AddFilterEffect<Border>($"{Strings.Border} (deprecated)")

                .AddFilterEffect<StrokeEffect>(Strings.StrokeEffect)

                .AddFilterEffect<Clipping>(Strings.Clipping)

                .AddFilterEffect<Dilate>(Strings.Dilate)

                .AddFilterEffect<Erode>(Strings.Erode)

                .AddFilterEffect<HighContrast>(Strings.HighContrast)

                .AddFilterEffect<HueRotate>(Strings.HueRotate)

                .AddFilterEffect<Lighting>(Strings.Lighting)

                .AddFilterEffect<LumaColor>(Strings.LumaColor)

                .AddFilterEffect<Saturate>(Strings.Saturate)

                .AddFilterEffect<Threshold>(Strings.Threshold)

                .AddFilterEffect<Brightness>(Strings.Brightness)

                .AddFilterEffect<Gamma>(Strings.Gamma)

                .AddFilterEffect<Invert>(Strings.Invert)

                .AddFilterEffect<LutEffect>(Strings.LUT_Cube_File)

                .AddFilterEffect<BlendEffect>(Strings.BlendEffect)

                .AddFilterEffect<Negaposi>(Strings.Negaposi)

                .AddFilterEffect<ChromaKey>(Strings.ChromaKey)

                .AddFilterEffect<ColorKey>(Strings.ColorKey)

                .AddFilterEffect<SplitEffect>(Strings.SplitEquallyEffect)

                .AddFilterEffect<PartsSplitEffect>(Strings.SplitByPartsEffect)

                .AddFilterEffect<TransformEffect>(Strings.Transform)

                .AddFilterEffect<Mosaic>(Strings.Mosaic)

                .AddFilterEffect<ColorShift>(Strings.ColorShift)

                .AddFilterEffect<ShakeEffect>(Strings.ShakeEffect)

                .AddGroup("OpenCV", gg => gg
                    .AddFilterEffect<Graphics.Effects.OpenCv.Blur>("CvBlur")
                    .AddFilterEffect<Graphics.Effects.OpenCv.GaussianBlur>("CvGaussianBlur")
                    .AddFilterEffect<Graphics.Effects.OpenCv.MedianBlur>("CvMedianBlur")
                )
            );

        LibraryService.Current
            .RegisterGroup("SoundEffect", g => g
                .AddSoundEffect<Delay>("Delay")
            );
    }
}
