# UnityHotReload

Tool for hot reloading your code inside your BepInEx environment.

## Setup

Download `HotCompiler_netstandard20.7z` located in this repo.

Copy the `HotCompiler` folder from the archive and paste it inside your `BepInEx/plugins` folder.

Copy the `HotCompilerNamespace` folder to your BepInEx Plugin source project.

Your BepInEx Plugin source project should contains a reference to `<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />`

Make sure the path [here](https://github.com/xiaoxiao921/UnityHotReload/blob/main/HotCompilerNamespace/HotCompiler.cs#L19) is right and point somewhere inside your BepInEx Plugin source project.

Inside your `BaseUnityPlugin` class, call `HotCompiler.CompileIt()`, you can check [an actual example here](https://github.com/xiaoxiao921/UnityHotReload/blob/main/ExampleMain.cs).

## Use Case

This can be very useful for doing extremely rapid changes for about any gameplay logic you might think of.

A good example is editing any part of a [State from RoR2](https://github.com/xiaoxiao921/UnityHotReload/blob/main/HotCompilerNamespace/HotReloadMain.cs), you can also do this with the methods of your own mod, that are sitting in the same source project!
