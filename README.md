<h1>
    <img align="left" src="https://github.com/bezo97/IFSRenderer/raw/master/Assets/icon_128.png">
    <p align="center"><samp>IFSRenderer</samp>&emsp;&emsp;&emsp;&emsp;&emsp;</p>
</h1>
<br/>
<h3 align="center">
	3D IFS fractal renderer and editor
</h3>
<p align="center">
	<strong>
		<a href="https://www.youtube.com/watch?v=R5YWiZQUadE">Demo</a>
		•
		<a href="https://github.com/bezo97/IFSRenderer/wiki">Wiki</a>
		•
		<a href="https://github.com/bezo97/IFSRenderer/releases/latest">Download</a>
	</strong>
</p>
<div align="center" markdown="1">

[![license](https://img.shields.io/github/license/bezo97/IFSRenderer)](/LICENSE)
[![release](https://img.shields.io/github/v/release/bezo97/IFSRenderer?include_prereleases&sort=semver)](https://github.com/bezo97/IFSRenderer/releases/latest)
[![library](https://img.shields.io/nuget/vpre/IFSEngine?label=library)](https://www.nuget.org/packages/IFSEngine/)
[![help](https://img.shields.io/github/labels/bezo97/IFSRenderer/help-wanted)](https://github.com/bezo97/IFSRenderer/issues)

</div>

IFSRenderer started as a weekend project to help me understand how the fractal flame algorithm works. 
My initial goal was to just implement it in 3D, but it has grown into a passion project and then into my master's thesis. 
I'm releasing it as an open-source project in the hope that it will be useful to the fractal artist community.

## 🗸 Features
- [x] Render 3D IFS (Iterated Function System) fractals
- [x] Real-time interaction
- [x] Node-based editor
- [x] Mutation-style generator 
- [x] Extendable with Plug-Ins

Planned:
- [ ] Animations
- [ ] More intuitive coloring methods
- [ ] Crossplatform CLI (Command-line interface)
- [ ] A better name & logo
- [ ] [*Add Your Ideas*](https://github.com/bezo97/IFSRenderer/discussions/categories/ideas)

## 📀 Installation

### Requirements
- Windows 10
- .NET 5.0 (included in installer)
- OpenGL 4.5 capable graphics card

### Downloads
Get the latest installer or portable version **[HERE](https://github.com/bezo97/IFSRenderer/releases/latest)**.  
Previous versions can be found on the [Releases](https://github.com/bezo97/IFSRenderer/releases) tab.

## 🕹️ Usage

### Using the editor

Beginners should start with the *[Getting Started Guide](https://github.com/bezo97/IFSRenderer/wiki)*. See the [Wiki](https://github.com/bezo97/IFSRenderer/wiki) for more.

### Using the library
Add the [latest NuGet package](https://www.nuget.org/packages/IFSEngine/) to your project. Here are some getting-started snippets.
<details>
<summary><b>Show snippets</b></summary>

Generate a random fractal:

```csharp
//Initialize
using RendererGL renderer = new(graphicsContext);
renderer.Initialize(loadedTransforms);
Generator generator = new(loadedTransforms);
//Generate fractal
IFS fractal = generator.GenerateOne(new GeneratorOptions{ });
fractal.ImageResolution = new Size(1920, 1080);
//Render
renderer.LoadParams(fractal);
renderer.DispatchCompute();
renderer.RenderImage();
//Save HDR image
var histogramData = await renderer.ReadHistogramData();
using var fstream = File.Create(path);
OpenEXR.WriteStream(fstream, histogramData);

```

Modify a fractal programmatically:
```csharp
//Load from file
IFS myFractal1 = IfsSerializer.LoadJson("myFractal1.ifsjson", loadedTransforms, true);
//Change params
Iterator selected = myFractal1.Iterators.First(i => i.Opacity == 0);
Iterator duplicated = myFractal1.DuplicateIterator(selected);
duplicated.Opacity = 1;
duplicated.TransformVariables["Strength"] = 10.0;
//Save to file
IfsSerializer.SaveJson(myFractal1, "myFractal1.ifsjson");
```

Render images:
```csharp
for (double i = 0.0; i <= 1.0; i += 0.1)
{
    selectedIterator.TransformVariables["weight"] = i;
    renderer.InvalidateParams();
    renderer.DispatchCompute();
    renderer.RenderImage();
    var image = await renderer.ReadPixelData();
    myRenderedImages.Add(image);
}
```
Alternatively, image data can be written directly to a bitmap:
```csharp
await renderer.CopyPixelDataToBitmap(myBitmapPtr);
```
</details>

## ❔ Support
- Browse the [Wiki](https://github.com/bezo97/IFSRenderer/wiki) pages
- [Report a bug](https://github.com/bezo97/IFSRenderer/issues/new?assignees=&labels=&template=bug_report.md)
- Discuss issues on the [Forum](https://github.com/bezo97/IFSRenderer/discussions)

---

## ⚖️ License
Copyright (C) 2021 Dócs Zoltán & contributors  
IFSRenderer is licensed under [**GPLv3**](/LICENSE).

### Contributors

[bezo97](https://github.com/bezo97) (Creator & Maintainer)  
Contributors: [AliBee](https://github.com/BenjaminBako), [Sekkmer](https://github.com/TiborDravecz), [*Add Your Name*](https://github.com/bezo97/IFSRenderer/fork)