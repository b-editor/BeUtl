﻿using Avalonia.Media;

using Beutl.Controls;
using Beutl.Framework;
using Beutl.ViewModels;

namespace Beutl.Services.PrimitiveImpls;

[PrimitiveImpl]
public sealed class OutputPageExtension : PageExtension
{
    public static readonly OutputPageExtension Instance = new();

    public override Geometry FilledIcon => FluentIconsFilled.Arrow_Export_LTR.GetGeometry();

    public override Geometry RegularIcon => FluentIconsRegular.Arrow_Export_LTR.GetGeometry();

    public override string Header => Strings.Output;

    public override Type Control => null!;

    public override Type Context => typeof(OutputPageViewModel);

    public override string Name => "OutputPage";

    public override string DisplayName => "OutputPage";
}