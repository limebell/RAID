using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace Raid.EditorTools
{
    [InitializeOnLoad]
    internal static class PlayModeFrameSelectedGuard
    {
        private static readonly string[] ShortcutIds =
        {
            "Scene View/Frame Selected",
            "Scene View/Frame Selected with Lock",
            "Main Menu/Edit/Frame Selected"
        };

        private static readonly Dictionary<string, ShortcutBinding> Originals = new();

        static PlayModeFrameSelectedGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Unbind();
                return;
            }

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Restore();
            }
        }

        private static void Unbind()
        {
            Restore();
            foreach (var id in ShortcutIds)
            {
                try
                {
                    Originals[id] = ShortcutManager.instance.GetShortcutBinding(id);
                    ShortcutManager.instance.RebindShortcut(id, ShortcutBinding.empty);
                }
                catch (System.Exception)
                {
                }
            }
        }

        private static void Restore()
        {
            foreach (var pair in Originals)
            {
                try
                {
                    ShortcutManager.instance.RebindShortcut(pair.Key, pair.Value);
                }
                catch (System.Exception)
                {
                }
            }

            Originals.Clear();
        }
    }
}
