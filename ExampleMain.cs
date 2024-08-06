using BepInEx;
using UnityEngine;
using HotCompilerNamespace;

namespace ExamplePluginNameSpace
{
    public class ExamplePlugin : BaseUnityPlugin
    {
        public void Awake()
        {
            HotCompiler.CompileIt();
        }

        public void Update()
        {
            if (Input.GetKeyUp("m"))
            {
                HotCompiler.CompileIt();
            }
        }
    }
}
