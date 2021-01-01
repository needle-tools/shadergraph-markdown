
using UnityEditor.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class MarkdownHDLitGUI : LitShaderGraphGUI
    {
        public MarkdownHDLitGUI() : base()
        {
            // remove default ShaderGraphUI block because we're rendering this ourselves
            uiBlocks.Remove(uiBlocks.FetchUIBlock<ShaderGraphUIBlock>());
        }
        
        public T GetBlock<T>() where T: MaterialUIBlock
        {
            return uiBlocks.FetchUIBlock<T>();
        }

        public MaterialUIBlockList blocks => uiBlocks;
    }

    public static class MarkdownHDExtensions
    {
        public static void DrawBlock(ShaderGUI shaderGUI)
        {
            switch (shaderGUI)
            {
                case MarkdownHDLitGUI markdownHdLitGUI:
                    break;
            }
        }
    }
}