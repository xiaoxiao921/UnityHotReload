# UnityHotReload

Tool for hot reloading your code inside your BepInEx environment.

## Setup

Your BepInEx plugin project needs a reference to `UnityHotReload.dll`.
You can add this reference in several ways, one of them is to download the latest release from Thunderstore, extract the zip, and reference the DLL from there.

Once your plugin is built and running, you can trigger a hot reload right after recompiling your code in your IDE by calling:

```csharp
void Update()
{
    if (Input.GetKeyUp(KeyCode.F2))
    {
        UnityHotReload.LoadNewAssemblyVersion(
            typeof(ExamplePlugin).Assembly, // The currently loaded assembly to replace.
            "C:/dev/MyPlugin/MyPlugin.dll"  // The path to the newly compiled DLL.
        );
    }
}
```

## Limitations

Treat all types (classes, structs, etc.) as having a fixed set of fields. You should not add, remove, or change fields, or convert fields to properties (and vice versa). This limitation exists because UnityHotReload preserves the existing runtime state by redirecting all active references to the original type definitions.

You can freely modify method bodies, add or remove methods, reorganize code, add whole new classes or structs, but the structure and identity of existing fields themselves must remain consistent.