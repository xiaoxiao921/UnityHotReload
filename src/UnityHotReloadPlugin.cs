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
        public const string PluginVersion = "1.0.1";

        public void Awake()
        {
            Log.Init(Logger);

            ReloadCode();
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
            NewMethodTest3(4);
        }

        private static void NewMethodTest()
        {
            Log.Info("hello from NewMethodTest!");
        }

        private static void NewMethodTest3()
        {
            Log.Info("hello from NewMethodTest3 no bool");
        }

        private static void NewMethodTest3(bool yea)
        {
            Log.Info("hello from NewMethodTest3 with bool");
        }

        private static void NewMethodTest3(int bla)
        {
            Log.Info("hello from NewMethodTest3 with int");
        }

        private void ReloadCode()
        {
            UnityHotReload.LoadNewAssemblyVersion(typeof(UnityHotReloadPlugin).Assembly,
                "C:\\Users\\Quentin\\source\\repos\\UnityHotReload\\src\\bin\\Debug\\netstandard2.0\\UnityHotReload.dll");
        }
#endif
    }
}
