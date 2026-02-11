・Objectのプロパティの文字が二重に表示されるのを修正  
・Sprite2Dコンポーネントを追加。座標はTransformのPosを使うように修正。TransformのZが大きい方が奥に表示されます。   
  
・ScriptEditerで日本語が表示されるようにした  
・scriptをprojectから右クリでopenする様にした。既に開いてるときは出ない  
<img width="340" height="249" alt="無題" src="https://github.com/user-attachments/assets/367e9b4a-f594-4e21-b855-d040bfc06396" />  
  
・Music Playerをlisten.moeに変更。あとタイトルとアーティスト名が出るようにした。もちろん日本語名対応。~~ただこのサイトやや重いです~~ こちらのせいでした、修正しました。    
・バックグラウンド時、曲のタイトルは変わるのに音楽が止まってしまう問題を修正した。自動再接続ループの導入、ストリーム終了の検知、初期バッファ待ちの最適化など  
・曲が変わる時引っかかるような感じになるのを修正。jsonの読み込みをスレッドにして曲の変り初めは少ないバッファで回すようにした。こういう細かい所はAIでは無理なので人間様の出番w  
<img width="331" height="171" alt="無題" src="https://github.com/user-attachments/assets/c5a72f09-daf9-4087-befb-ed2731ecb5f0" />  
・musicを止めると高確率で落ちるのを修正。人が折角直したのAIに上書きされてた。これだからAIは┐(´∀｀)┌ﾔﾚﾔﾚ  

※注意点  
プロパティが大文字でもluaで使う時は先頭が小文字になるので注意  
例)Rotation → rotation  
<img width="1273" height="870" alt="無題" src="https://github.com/user-attachments/assets/2a478bee-a1ba-4e0f-9eda-a7b5c7a177fb" />  




# Scythe

Scythe is a lightweight, C#-based game engine focused on modifiability and rapid iteration using [Raylib](https://github.com/raysan5/raylib).
<br/>
It combines a modern rendering pipeline, a fully integrated editor, and a runtime into a single codebase.

> [!NOTE]  
> Scythe is a hobby project. It is developed by me alone, for personal enjoyment. Do not expect official support, and relying on it for commercial projects is entirely your own responsibility and is not recommended.

> [!CAUTION]  
> Scythe is still in an early stage of development and is not even in an Alpha or Beta phase. Existing features may be removed or changed at any time. It is not unexpected for sample projects to be incomplete, for some existing features to be temporarily unavailable, or for default settings to change with each commit.

https://github.com/user-attachments/assets/0c301833-79d5-4ac3-ab05-dfff61f48d2b

## 🎯 Who is this for?

- Indie developers who want full control over engine internals.
- Developers interested in moddable, scriptable game architectures.
- Learning-oriented projects *(rendering, physics, and engine tooling)*.
- Developers who prefer code-first workflows over black-box engines.

## 🛠️ License

Scythe is licensed under the [MIT license](./LICENSE).

This project uses:

- [C# bindings](https://github.com/raylib-cs/raylib-cs?tab=Zlib-1-ov-file#readme) of [Raylib](https://github.com/raysan5/raylib?tab=Zlib-1-ov-file#readme) and [C# bindings](https://github.com/raylib-extras/rlImGui-cs?tab=Zlib-1-ov-file#readme) of [rlImGui](https://github.com/raylib-extras/rlImGui?tab=Zlib-1-ov-file#readme), all licensed under zlib/libpng.

- [ImGui](https://github.com/ocornut/imgui?tab=MIT-1-ov-file#readme), [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json?tab=MIT-1-ov-file#readme), [LuaLS (lua-language-server)](https://github.com/LuaLS/lua-language-server?tab=MIT-1-ov-file#readme), [Jitter Physics 2 *(Jitter2)*](https://github.com/notgiven688/jitterphysics2?tab=MIT-1-ov-file#readme), [NLayer](https://github.com/naudio/NLayer?tab=MIT-1-ov-file#readme) and [AssimpNet](https://bitbucket.org/Starnick/assimpnet/src/master/License.txt) all licensed under MIT.

- [MoonSharp](https://github.com/moonsharp-devs/moonsharp/?tab=License-1-ov-file#readme) licensed under the BSD 3-Clause.

## 🧱 Architecture Overview

- Component-based object model *(not ECS)*.
- Shared runtime between editor and game execution.

## ✨ Features

### Core

- **Modern .NET:** High-performance architecture built on the latest .NET 10 runtime.
- **Hybrid Runtime/Editor:** Combines a scene editor with a fast game runtime in a single environment.
- **Asset Management:** Centralized asset manager with hot-reloading via filesystem watching.
    - **Models:** .fbx, .obj, .gltf
    - **Textures:** .png, .jpg, .jpeg, .tga, .bmp
    - **Shaders:** .vs, .fs

### Graphics

- **PBR Rendering:** Shader-based Physically Based Rendering *(PBR)* support for realistic material appearance.
- **Advanced Lighting & Shadows:** Dynamic light sources with real-time Shadow Mapping.
- **Transparent Render Queue:** Proper depth-sorted rendering of transparent objects.
- **Skybox:** Skybox rendering with cubemaps.
- **Post-Processing:**
    - SSAO *(Screen Space Ambient Occlusion)*
    - FXAA, SMAA, TAA *(with jitter)*
    - Bloom, Blur, Pixellizer, Cross Hatching, Cross Stitching, Sobel edge detection, Scanlines, Fisheye, Dream Vision, Predator, Grayscale, Posterization

### Animation
- **Skeletal Animation:** Mesh skinning and bone animation.
- **Animation Blending:** Smooth transitions between animations.

### Physics

- **Pure C# Physics:** Jitter2 integration written entirely in C#.
- Support for dynamic and static objects.

### Editor

- **Docking UI:** ImGui-based interface with freely dockable windows.
- **Advanced Script Editor:**
    - Lua Language Server Protocol *(LSP)* support.
    - Code completion *(IntelliSense)*.
    - Error diagnostics.
    - Tabbed multi-file editing.
    - Syntax highlighting.
- **Level Browser:** Hierarchical object management with a layered structure.
- **Object Browser:** Real-time panel for editing object properties.
- **Undo / Redo System:** Action history tracking across editor operations.
- A built-in music player! 😎

### Scripting

- **Lua Integration:** Fast and easy modding/scripting via MoonSharp.
- **Dynamic Input:** Simple management of mouse and keyboard input for both the editor and scripting layers.

## ❌ What this is not

- Not a full-scale AA/AAA engine.
- Not an ECS-first architecture.
- Not focused on visual scripting.

## 👷 Building

> [!NOTE]  
> Make sure you have the .NET SDK 10.0+ packages installed.

```bash
git clone https://github.com/fkerimk/scythe.git
cd scythe
dotnet build
```

## 🏹 Running

```ps
dotnet run -- editor
```

- Opens the editor
- Press F5 to run the play mode

## 🙏 Attributions

Many post-processing shaders and various other shaders were taken from, or based on, [Raylib Examples](https://github.com/raysan5/raylib/tree/master/examples). Thanks, Raylib!

#### Fonts

[Font Awesome](http://fontawesome.io) by Dave Gandy.
<br/>
[Montserrat](https://fonts.google.com/specimen/Montserrat/license) by Julieta Ulanovsky, Sol Matas, Juan Pablo del Peral, Jacques Le Bailly.
<br/>
[Cascadia Code](https://fonts.google.com/specimen/Cascadia+Code/license) by Aaron Bell, Mohamad Dakak, Viktoriya Grabowska, Liron Lavi Turkenich.

#### Samples

[Wooden Alphabet Blocks](https://skfb.ly/oRnRU) by Cherryvania, licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/).
<br/>
[The Green Wizard Gnome N64 Style](https://skfb.ly/oXSLR) by Drillimpact, licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/).
<br/>
[Animated FPS Pistol](https://skfb.ly/psqCp) by Levraicoincoin, DJMaesen, licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/).

#### Early Samples


[Bear Man PSX](https://skfb.ly/p9SUZ) by Bonvikt, licensed under [CC BY 4.0](http://creativecommons.org/licenses/by/4.0/).












