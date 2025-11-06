using BepInEx;
#if DEBUG
using UnityEngine;
#endif

namespace UnityHotReloadNS
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class UnityHotReloadPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "iDeathHD";
        public const string PluginName = "UnityHotReload";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            Log.Init(Logger);
        }

#if DEBUG
        private static int Data = 0;
        private static int Data2 = 42;
        private static int Data3 = 69;
        private static int Data4 = 699;

        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.F2))
            {
                ReloadCode();
            }

            if (Input.GetKeyUp(KeyCode.F2))
            {
                Log.Warning("Data2: " + Data2);
            }

            //Log.Warning("Data3: " + Data4);

            NewMethodTest3();
        }

        private static void NewMethodTest()
        {
            Log.Info("hello from NewMethodTest!");
        }

        private static void NewMethodTest3()
        {
            Log.Info("hello from NewMethodTest!4");
        }

        private void ReloadCode()
        {
            UnityHotReload.LoadNewAssemblyVersion(typeof(UnityHotReloadPlugin).Assembly,
                "C:\\Users\\Quentin\\Desktop\\banger\\UnityHotReload\\src\\bin\\Debug\\netstandard2.0\\UnityHotReload.dll");
        }
#endif
    }
}
