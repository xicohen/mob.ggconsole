using UnityEditor;
using UnityEditor.Build;

namespace Mob404.Console.Editor
{
    /// <summary>
    /// Tu dong them define HAS_MOB404_CONSOLE khi package duoc cai dat
    /// </summary>
    [InitializeOnLoad]
    static class ConsoleDefineInstaller
    {
        const string Define = "HAS_MOB404_CONSOLE";

        static ConsoleDefineInstaller()
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (target == BuildTargetGroup.Unknown) return;

            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(target);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out string[] defines);

            foreach (var d in defines)
            {
                if (d == Define) return;
            }

            var newDefines = new string[defines.Length + 1];
            defines.CopyTo(newDefines, 0);
            newDefines[defines.Length] = Define;
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);
        }
    }
}
