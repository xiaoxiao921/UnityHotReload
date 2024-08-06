# UnityHotReload

Tool for hot reloading your code inside your BepInEx environment.

## Instructions

Download `HotCompiler_netstandard20.7z` located in this repo.

Copy the `HotCompiler` folder from the archive and paste it inside your `BepInEx/plugins` folder.

Copy the `HotCompilerNamespace` folder to your BepInEx Plugin source project.

Make sure the path [here](https://github.com/xiaoxiao921/UnityHotReload/blob/main/HotCompilerNamespace/HotCompiler.cs#L19) is right and point somewhere inside your BepInEx Plugin source project.

Inside your `BaseUnityPlugin` class, call `HotCompiler.CompileIt()`, you can check [an actual example here](https://github.com/xiaoxiao921/UnityHotReload/blob/main/ExampleMain.cs).
