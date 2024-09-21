#if DEBUG
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using BepInEx;

namespace HotCompilerNamespace
{
    internal class HotCompiler
    {
        private static int CompilationCount = 0;
        public static void CompileIt()
        {
            const string pathToHotReloadMain = "C:/Users/USER/Desktop/YourBepinexPluginCSharpProject/src/HotCompilerNamespace/HotReloadMain.cs";

            const BindingFlags allFlags = (BindingFlags)(-1);

            var ass = CompileString(
                File.ReadAllText(pathToHotReloadMain));

            if (ass == null)
            {
                Log.Error($"Failed hot compiling assembly");
                return;
            }

            var entryPoint = ass.GetTypes()
           .SelectMany(t => t.GetMethods(allFlags))
           .FirstOrDefault(m => m.Name == nameof(HotReloadMain.HotReloadEntryPoint));

            if (entryPoint == null)
            {
                Log.Error($"Failed getting entrypoint");
                return;
            }

            var res = entryPoint.Invoke(null, null);

            Log.Info($"CompilationCount: {CompilationCount++}");
        }

        public static Module CompileString(string code)
        {
            CSharpParseOptions options = CSharpParseOptions.Default.
                WithLanguageVersion(LanguageVersion.Latest).
                WithPreprocessorSymbols("DEBUG");
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code, options);
        
            CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                metadataImportOptions: MetadataImportOptions.All)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Microsoft.CodeAnalysis.Platform.AnyCpu /* Keep The Microsoft.CodeAnalysis part. */)
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                .WithAllowUnsafe(true)
                .WithWarningLevel(0);
        
            // bypass access checks
            var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags",
                BindingFlags.Instance | BindingFlags.NonPublic);
            topLevelBinderFlagsProperty.SetValue(compilationOptions, (uint)1 << 22);
        
            static bool FilterUnwantedAssemblies(string path)
            {
                // Fix an edge case where Mono.Cecil assembly Opcodes type may conflict with the one from Unity.Cecil
                // Change this whenever you get similar conflicts.
                if (path.Contains("Unity.Cecil"))
                {
                    return false;
                }
        
                return true;
            }
        
            var assLocations = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                    !a.IsDynamic &&
                    !string.IsNullOrWhiteSpace(a.Location) &&
                    FilterUnwantedAssemblies(a.Location))
                .Select(a => a.Location)
                .ToHashSet();
        
            // Handle the case where targeted and referenced assemblies won't have a proper .Location field.
            // Happens when the assembly is loaded from a byte array and that whatever loaded it didn't fill up the Location field.
            // This is because we load MetadataReference From File (code below),
            // a better solution would be to dump the current loaded assembly byte arrays from the current process and load the
            // metadatareference from that instead.
            foreach (var assLocation in Directory.GetFiles(Paths.ManagedPath, "*.dll", SearchOption.AllDirectories))
            {
                if (FilterUnwantedAssemblies(assLocation))
                {
                    assLocations.Add(assLocation);
                }
            }
        
            CSharpCompilation comp = CSharpCompilation.Create(Path.GetRandomFileName())
                .WithOptions(compilationOptions)
                .AddReferences(assLocations.Select(a => MetadataReference.CreateFromFile(a)))
                .AddSyntaxTrees(tree);
        
            using (MemoryStream stream = new MemoryStream())
            {
                EmitResult res = comp.Emit(stream);
        
                if (!res.Success)
                {
                    foreach (Diagnostic diag in res.Diagnostics)
                    {
                        Log.Error(diag);
                    }
        
                    return null;
                }
        
                stream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(stream.ToArray()).GetModules()[0];
            }
        }
    }
}
#endif
