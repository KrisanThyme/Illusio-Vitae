using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IVPlugin.UI.Helpers
{
    public class GUIMethods
    {
        public static float CalcContrastRatio(uint backgroundColor, uint foregroundColor)
        {
            // real code https://www.w3.org/TR/WCAG20/#relativeluminancedef
            /*const auto colBG = ImGui::ColorConvertU32ToFloat4(backgroundColor);
            const auto colFG = ImGui::ColorConvertU32ToFloat4(foreGroundColor);
            float lumBG = 0.2126 * colBG.x + 0.7152 * colBG.y + 0.0722 * colBG.z;
            float lumFG = 0.2126 * colFG.x + 0.7152 * colFG.y + 0.0722 * colFG.z;
            return (ImMax(lumBG, lumFG) + 0.05) / (ImMin(lumBG, lumFG) + 0.05);*/

            float sa0 = ((backgroundColor >> 24) & 0xFF);
            float sa1 = ((foregroundColor >> 24) & 0xFF);
            float sr = 0.2126f / 255.0f;
            float sg = 0.7152f / 255.0f;
            float sb = 0.0722f / 255.0f;
            float contrastRatio =
                (sr * sa0 * ((backgroundColor >> 16) & 0xFF) +
                    sg * sa0 * ((backgroundColor >> 8) & 0xFF) +
                    sb * sa0 * ((backgroundColor >> 0) & 0xFF) + 0.05f) /
                (sr * sa1 * ((foregroundColor >> 16) & 0xFF) +
                    sg * sa1 * ((foregroundColor >> 8) & 0xFF) +
                    sb * sa1 * ((foregroundColor >> 0) & 0xFF) + 0.05f);
            if (contrastRatio < 1.0f)
                return 1.0f / contrastRatio;
            return contrastRatio;
        }

        public static float CalculateLuminance(uint color)
        {
            float r = ((color & 0xFF0000) >> 16) / 255.0f;
            float g = ((color & 0x00FF00) >> 8) / 255.0f;
            float b = (color & 0x0000FF) / 255.0f;
            float luminance = 0.299f * r + 0.587f * g + 0.114f * b;
            return luminance;
        }

        public static uint ARGBToABGR(uint argbColor) => ((argbColor >> 24) & 0xFF) | ((argbColor & 0xFF) << 16) | ((argbColor & 0xFF00) & 0xFF00) | ((argbColor >> 16) & 0xFF);

        public static unsafe Vector4 ColorConvertU32ToFloat4(uint @in)
        {
            Vector4 result = default(Vector4);
            ImGuiNative.igColorConvertU32ToFloat4(&result, @in);
            return result;
        }
    }
}
