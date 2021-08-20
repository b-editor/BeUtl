
using BEditor.Data;
using BEditor.Plugin;

namespace BEditor.Extensions.Svg
{
    public sealed class Plugin
    {
        public static void Register()
        {
            PluginBuilder.Configure<SvgPlugin>()
                .With(ObjectMetadata.Create<SvgImage>("Svg�摜", null))
                .Register();
        }
    }
}