using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace UnityHotReloadNS
{
    public static class UnityHotReload
    {
        const BindingFlags allFlags = (BindingFlags)(-1);

        internal static int CompilationCount = 0;

        private static List<ILHook> _activeILHooks = new List<ILHook>();

        /// <summary>
        /// Loads a new version of the specified assembly and replaces methods in the original assembly
        /// with those from the updated assembly.
        /// </summary>
        /// <param name="originalAss">The original assembly to be hot-reloaded.</param>
        /// <param name="path">The file path to the newly compiled assembly.</param>
        public static void LoadNewAssemblyVersion(Assembly originalAss, string pathOfTheNewAssembly)
        {
            LoadNewAssemblyVersionInternal(originalAss, pathOfTheNewAssembly);
        }

        private static void LoadNewAssemblyVersionInternal(Assembly originalAss, string path)
        {
            OriginalAss = originalAss;

            var defaultResolver = new DefaultAssemblyResolver();
            defaultResolver.AddSearchDirectory(Paths.ManagedPath);
            defaultResolver.AddSearchDirectory(Paths.BepInExAssemblyDirectory);

            Log.Debug(path);

            using (var dll = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
            {
                AssemblyResolver = defaultResolver,
                ReadSymbols = true
            }))
            {
                dll.Name.Name = $"{originalAss.GetName().Name}_HotReload_{++CompilationCount}";
                Assembly newAss;

                using var assStream = new MemoryStream();
                using var symbolStream = new MemoryStream();
                dll.Write(assStream, new WriterParameters
                {
                    WriteSymbols = true,
                    SymbolStream = symbolStream,
                    SymbolWriterProvider = new PortablePdbWriterProvider(),
                });

                newAss = Assembly.Load(assStream.ToArray(), assStream.ToArray());

                Log.Debug($"originalAss: {originalAss.GetName().Name}");
                Log.Debug($"newAss: {newAss.GetName().Name}");

                foreach (var type in GetTypesSafe(newAss))
                {
                    foreach (var method in type.GetMethods(allFlags))
                    {
                        var originalType = originalAss.GetType(type.FullName);
                        var originalMethod = GetMethodResolveAmbiguous(method, originalType);
                        if (originalMethod == null)
                        {
                            // We still need to fix up references for a new, never seen method,
                            // as it may refer to existing types/methods/fields of the original assembly in its body.
                            originalMethod = method;
                        }

                        try
                        {
                            _activeILHooks.Add(new ILHook(originalMethod,
                                GatherOriginalReferences));

                            _activeILHooks.Add(new ILHook(method,
                                GatherInstructionsFromNewMethodAndMakeThemReferenceOriginalAssembly));

                            if (originalMethod != method)
                            {
                                // Redirect the old method to the new one version of that same method

                                _activeILHooks.Add(new ILHook(originalMethod, il =>
                                {
                                    var c = new ILCursor(il);

                                    for (var i = 0; i < il.Method.Parameters.Count; i++)
                                    {
                                        c.Emit(OpCodes.Ldarg, i);
                                    }

                                    c.Emit(OpCodes.Call, method);
                                    c.Emit(OpCodes.Ret);
                                }));
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Failed to hot reload method: {type.FullName}.{method.Name}\n{e}");
                        }
                    }
                }
            }
        }

        private static MethodInfo GetMethodResolveAmbiguous(MethodReference methodRef, Type originalType)
        {
            var res = originalType.GetMethods(allFlags).Where(m => m.Name == methodRef.Name).ToList();
            if (res.Count == 1)
                return res[0];
            if (res.Count > 1)
            {
                foreach (var candidate in res)
                {
                    var params1 = candidate.GetParameters();
                    var params2 = methodRef.Parameters;
                    if (params1.Length != params2.Count)
                        continue;
                    var allMatch = true;
                    for (var i = 0; i < params1.Length; i++)
                    {
                        if (params1[i].ParameterType.FullName != params2[i].ParameterType.FullName)
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch)
                    {
                        Log.Debug($"Resolved ambiguous method: {originalType.FullName}.{methodRef.Name}");
                        return candidate;
                    }
                }
            }   
            return null;
        }

        private static MethodInfo GetMethodResolveAmbiguous(MethodInfo method, Type originalType)
        {
            var res = originalType.GetMethods(allFlags).Where(m => m.Name == method.Name).ToList();

            if (res.Count == 1)
                return res[0];

            if (res.Count > 1)
            {
                foreach (var candidate in res)
                {
                    var params1 = candidate.GetParameters();
                    var params2 = method.GetParameters();

                    if (params1.Length != params2.Length)
                        continue;

                    var allMatch = true;
                    for (var i = 0; i < params1.Length; i++)
                    {
                        // ToString cause of 
                        //    Name: List`1
                        //    Full Name: System.Collections.Generic.List`1[[System.String, mscorlib, Version=4
                        //    .0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
                        //    ToString:  System.Collections.Generic.List`1[System.String]
                        //    Assembly Qualified Name: System.Collections.Generic.List`1[[System.String, mscor
                        //    lib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorl
                        //    ib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
                        if (params1[i].ParameterType.ToString() != params2[i].ParameterType.ToString())
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    if (allMatch)
                    {
                        Log.Debug($"Resolved ambiguous method: {originalType.FullName}.{method.Name}");
                        return candidate;
                    }
                }
            }   

            return null;
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly ass)
        {
            try
            {
                return ass.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var sbMessage = new StringBuilder();
                sbMessage.AppendLine("\r\n-- LoaderExceptions --");
                foreach (var l in ex.LoaderExceptions)
                    sbMessage.AppendLine(l.ToString());
                sbMessage.AppendLine("\r\n-- StackTrace --");
                sbMessage.AppendLine(ex.StackTrace);
                Log.Error(sbMessage.ToString());
                return ex.Types.Where(x => x != null);
            }
        }

        private static Assembly OriginalAss = null;
        private static Dictionary<string, TypeReference> OriginalAssTypeRefs = new Dictionary<string, TypeReference>();
        private static Dictionary<string, MethodReference> OriginalAssMethodRefs = new Dictionary<string, MethodReference>();
        private static Dictionary<string, FieldReference> OriginalAssFieldRefs = new Dictionary<string, FieldReference>();
        private static Dictionary<string, CallSite> OriginalAssCallSites = new Dictionary<string, CallSite>();

        private static void GatherOriginalReferences(ILContext il)
        {
            foreach (var instr in il.Instrs)
            {
                if (instr.Operand is TypeReference typeRef)
                {
                    if (!OriginalAssTypeRefs.ContainsKey(typeRef.FullName))
                        OriginalAssTypeRefs.Add(typeRef.FullName, typeRef);
                }
                else if (instr.Operand is MethodReference methodRef)
                {
                    if (!OriginalAssMethodRefs.ContainsKey(methodRef.FullName))
                        OriginalAssMethodRefs.Add(methodRef.FullName, methodRef);
                }
                else if (instr.Operand is FieldReference fieldRef)
                {
                    if (!OriginalAssFieldRefs.ContainsKey(fieldRef.FullName))
                        OriginalAssFieldRefs.Add(fieldRef.FullName, fieldRef);
                }
                else if (instr.Operand is CallSite callSite)
                {
                    if (!OriginalAssCallSites.ContainsKey(callSite.FullName))
                        OriginalAssCallSites.Add(callSite.FullName, callSite);
                }
            }
        }

        private static void GatherInstructionsFromNewMethodAndMakeThemReferenceOriginalAssembly(ILContext il)
        {
            foreach (var instr in il.Instrs)
            {
                var correctedOperand = instr.Operand;

                if (instr.Operand is TypeReference typeRef)
                {
                    if (!OriginalAssTypeRefs.ContainsKey(typeRef.FullName))
                    {
                        var originalType = OriginalAss.GetType(typeRef.FullName);
                        if (originalType != null)
                        {
                            // Happens when the original assembly defined this type but never used it,
                            // and now the new assembly is using it for the first time.

                            Log.Debug($"Adding missing type ref: {typeRef.FullName} / {il.Method.Module.Name}");
                            OriginalAssTypeRefs.Add(typeRef.FullName, il.Import(originalType));
                        }
                    }

                    if (OriginalAssTypeRefs.TryGetValue(typeRef.FullName, out var existingTypeRef))
                    {
                        correctedOperand = existingTypeRef;
                    }
                }
                else if (instr.Operand is MethodReference methodRef)
                {
                    if (!OriginalAssMethodRefs.ContainsKey(methodRef.FullName))
                    {
                        var originalType = OriginalAss.GetType(methodRef.DeclaringType.FullName);
                        if (originalType != null)
                        {
                            var originalMethod = GetMethodResolveAmbiguous(methodRef, originalType);
                            if (originalMethod != null)
                            {
                                Log.Debug($"Adding missing method ref: {methodRef.FullName} / {il.Method.Module.Name}");
                                OriginalAssMethodRefs.Add(methodRef.FullName, il.Import(originalMethod));
                            }
                        }
                    }

                    if (OriginalAssMethodRefs.TryGetValue(methodRef.FullName, out var existingMethodRef))
                    {
                        correctedOperand = existingMethodRef;
                    }
                }
                else if (instr.Operand is FieldReference fieldRef)
                {
                    if (!OriginalAssFieldRefs.ContainsKey(fieldRef.FullName))
                    {
                        var originalType = OriginalAss.GetType(fieldRef.DeclaringType.FullName);
                        var originalField = originalType?.GetField(fieldRef.Name, allFlags);
                        if (originalField != null)
                        {
                            Log.Debug($"Adding missing field ref: {fieldRef.FullName} / {il.Method.Module.Name}");
                            OriginalAssFieldRefs.Add(fieldRef.FullName, il.Import(originalField));
                        }
                    }

                    if (OriginalAssFieldRefs.TryGetValue(fieldRef.FullName, out var existingFieldRef))
                    {
                        correctedOperand = existingFieldRef;
                    }
                }
                else if (instr.Operand is CallSite callSite)
                {
                    // probably need the same logic as above for callsites that are missing

                    if (OriginalAssCallSites.TryGetValue(callSite.FullName, out var existingCallSite))
                    {
                        correctedOperand = existingCallSite;
                    }
                }

                instr.Operand = correctedOperand;
            }
        }
    }
}
