using System.Reflection;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace HotCompilerNamespace
{
    public class HotReloadMain
    {
        const BindingFlags allFlags = (BindingFlags)(-1);

        // Entry point method that will be called upon pressing the hotkey.
        public static void HotReloadEntryPoint()
        {
            // This is just for being able to call base.OnEnter() inside hooks.
            {
                new ILHook(typeof(HotReloadMain).GetMethod(nameof(BaseStateOnEnterCaller), allFlags), BaseStateOnEnterCallerMethodModifier);
            }

            {
                var methodToReload = typeof(Snipe).GetMethod(nameof(Snipe.OnEnter), allFlags);
                var newMethod = typeof(HotReloadMain).GetMethod(nameof(SnipeOnEnterHook), allFlags);
                new Hook(methodToReload, newMethod);
            }
        }

        // This is just for being able to call base.OnEnter() inside hooks.
        private static void BaseStateOnEnterCaller(BaseState self)
        {

        }

        // This is just for being able to call base.OnEnter() inside hooks.
        private static void BaseStateOnEnterCallerMethodModifier(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(BaseState).GetMethod(nameof(BaseState.OnEnter), allFlags));
        }

        private static void SnipeOnEnterHook(Snipe self)
        {
            // method hook that will be applied upon pressing the hotkey.
        }
    }
}
