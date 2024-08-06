using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System;
using System.Linq;

namespace HotCompilerNamespace
{
    internal class HotCompiler
    {
        private static int CompilationCount = 0;
        public static void CompileIt()
        {
#if RELEASE
return;
#endif
            const string pathToHotReloadMain = "C:/Users/USER/Desktop/YourBepinexPluginCSharpProject/src/HotCompilerNamespace/HotReloadMain.cs";

            const BindingFlags allFlags = (BindingFlags)(-1);

            var ass = CompileString(
                File.ReadAllText(pathToHotReloadMain));
            var entryPoint = ass.GetTypes()
           .SelectMany(t => t.GetMethods(allFlags))
           .FirstOrDefault(m => m.Name == nameof(HotReloadMain.HotReloadEntryPoint));

            var res = entryPoint.Invoke(null, null);
            Log.Info($"CompilationCount: {CompilationCount++}");
        }

        public static Module CompileString(string code)
        {
#if RELEASE
return;
#endif
            CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);
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

            CSharpCompilation comp = CSharpCompilation.Create(Path.GetRandomFileName())
                .WithOptions(compilationOptions).
                AddReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location)))
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