using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs;
using static System.Text.Encoding;
using static ImGuiNET.ImGui;
using static ImGuiNET.ImGuiNative;
using static Raylib_cs.Raylib;
using static rlImGui_cs.rlImGui;

internal static class Fonts {

  private const int SmallSize = 11, NormalSize = 16, LargeSize = 32;

  public static ImFontPtr ImMontserratRegular, ImFontAwesomeSmall,
      ImFontAwesomeNormal, ImFontAwesomeLarge;

  public static Font RlMontserratRegular, RlCascadiaCode, RlIpafont;

  private static ImFontConfigPtr _imFontConfigPtr;
  private static IntPtr _iconRanges;

  public static unsafe void Init() {

    if (CommandLine.Editor) {

      _iconRanges =
          GCHandle
              .Alloc(new ushort[] { 0xE000, 0xF8FF, 0 }, GCHandleType.Pinned)
              .AddrOfPinnedObject();

      _imFontConfigPtr = ImFontConfig_ImFontConfig();
      _imFontConfigPtr.OversampleH = 3;
      _imFontConfigPtr.OversampleV = 3;

      ImMontserratRegular = LoadFont<ImFontPtr>("Fonts/montserrat-regular.otf");
      ImFontAwesomeSmall =
          LoadFont<ImFontPtr>("Fonts/fa7-free-solid.otf", SmallSize, true);
      ImFontAwesomeNormal =
          LoadFont<ImFontPtr>("Fonts/fa7-free-solid.otf", NormalSize, true);
      ImFontAwesomeLarge =
          LoadFont<ImFontPtr>("Fonts/fa7-free-solid.otf", LargeSize, true);

      ReloadFonts();
    }

    RlMontserratRegular = LoadFont<Font>("Fonts/montserrat-regular.otf");
    RlCascadiaCode = LoadFont<Font>("Fonts/CascadiaCode-Regular.ttf");
    RlIpafont = LoadFont<Font>("Fonts/ipam.ttf");
  }

  public static void UnloadRlFonts() {

    UnloadFont(RlMontserratRegular);
    UnloadFont(RlCascadiaCode);
    UnloadFont(RlIpafont);
  }

  private static unsafe T LoadFont<T>(string path, int size = NormalSize,
                                      bool useIconRanges = false) {

    if (!PathUtil.GetPath(path, out var fontPath))
      throw new FileNotFoundException($"Font {path} not found");

    if (typeof(T) == typeof(Font)) {

      var codepoints = new List<int>();
      for (var i = 32; i < 127; i++)
        codepoints.Add(i); // Basic ASCII

      int[] turkishChars = [
        0x00c7, 0x00e7, 0x011e, 0x011f, 0x0130, 0x0131, 0x00d6, 0x00f6, 0x015e,
        0x015f, 0x00dc, 0x00fc
      ];

      codepoints.AddRange(turkishChars);
      // ここから
      //  日本語の範囲を追加
      //  ひらがな・カタカナ・句読点など (0x3000 - 0x30FF)
      for (var i = 0x3000; i <= 0x30FF; i++)
        codepoints.Add(i);
      // 漢字 (CJK Unified Ideographs: 0x4E00 - 0x9FFF)
      // ※全部入れるとメモリを食うので注意。主要なものだけに絞るか、一旦全部入れる。
      for (var i = 0x4E00; i <= 0x9FFF; i++)
        codepoints.Add(i);
      // 全角英数・記号 (0xFF00 - 0xFFEF)
      for (var i = 0xFF00; i <= 0xFFEF; i++)
        codepoints.Add(i);
      // ここまで

      fixed(int *pCodepoints = codepoints.ToArray()) {

        var bytes = UTF8.GetBytes(fontPath);

        fixed(byte *pBytes = bytes) {

          var font =
              LoadFontEx((sbyte *)pBytes, 96, pCodepoints, codepoints.Count);
          SetTextureFilter(font.Texture, TextureFilter.Bilinear);

          return (T)(object)font;
        }
      }
    }

    if (typeof(T) == typeof(ImFontPtr)) {
      var io = GetIO();
      var ranges =
          useIconRanges ? _iconRanges : io.Fonts.GetGlyphRangesJapanese();
      return (T)(object)io.Fonts.AddFontFromFileTTF(fontPath, size,
                                                    _imFontConfigPtr, ranges);
    }

    throw new NotSupportedException($"Unsupported font type: {typeof(T)}");
  }
}