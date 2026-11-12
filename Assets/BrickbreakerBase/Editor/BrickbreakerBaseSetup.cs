using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrickbreakerBase.EditorTools
{
    // Runs when the package is imported (and after every script reload) to do the two things a
    // scripts-only package can't carry as assets:
    //   1. make sure the "Bouncy" tag exists -- balls only bounce off colliders carrying it;
    //   2. offer to import the TextMesh Pro essentials, which the HUD and brick numbers need.
    [InitializeOnLoad]
    internal static class BrickbreakerBaseSetup
    {
        private const string RequiredTag = "Bouncy";
        private const string TmpCheckedThisSessionKey = "BrickbreakerBase.TmpChecked";
        private const string TmpNeverAskKey = "BrickbreakerBase.TmpNeverAsk";

        static BrickbreakerBaseSetup()
        {
            // delayCall: the AssetDatabase isn't safe to touch during the static constructor.
            EditorApplication.delayCall += RunSetup;
        }

        private static void RunSetup()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;

            EnsureBouncyTag();
            CheckTextMeshProEssentials(false);
        }

        [MenuItem("Tools/Brickbreaker Base/Run Setup")]
        private static void RunSetupFromMenu()
        {
            EnsureBouncyTag();
            CheckTextMeshProEssentials(true);
        }

        [MenuItem("GameObject/Brickbreaker Base/Game Bootstrap", false, 10)]
        private static void CreateBootstrap(MenuCommand command)
        {
            GameObject go = new GameObject("Brickbreaker Bootstrap");
            go.AddComponent<BrickbreakerBootstrap>();

            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Brickbreaker Bootstrap");
            Selection.activeObject = go;
        }

        private static void EnsureBouncyTag()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == RequiredTag) return;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = RequiredTag;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log("[Brickbreaker Base] Added the '" + RequiredTag + "' tag (balls bounce off colliders that carry it).");
        }

        // The essentials land as a TMP Settings asset -- if there isn't one, nothing using
        // TextMesh Pro can render text yet.
        private static bool TextMeshProEssentialsPresent()
        {
            return AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;
        }

        private static void CheckTextMeshProEssentials(bool fromMenu)
        {
            if (TextMeshProEssentialsPresent())
            {
                if (fromMenu)
                {
                    EditorUtility.DisplayDialog("Brickbreaker Base", "TextMesh Pro essentials are already imported. You're set.", "OK");
                }
                return;
            }

            if (!fromMenu)
            {
                if (EditorPrefs.GetBool(TmpNeverAskKey, false)) return;

                // Once per editor session, so a script reload doesn't nag again.
                if (SessionState.GetBool(TmpCheckedThisSessionKey, false)) return;
                SessionState.SetBool(TmpCheckedThisSessionKey, true);
            }

            int choice = EditorUtility.DisplayDialogComplex(
                "Brickbreaker Base",
                "Brickbreaker Base shows its HUD and brick numbers with TextMesh Pro, and this project doesn't have the TMP essential resources yet.\n\nImport them now?",
                "Import now",
                "Later",
                "Don't ask again");

            if (choice == 0)
            {
                ImportTextMeshProEssentials();
            }
            else if (choice == 2)
            {
                EditorPrefs.SetBool(TmpNeverAskKey, true);
            }
        }

        private static void ImportTextMeshProEssentials()
        {
            // Same command as Window > TextMeshPro > Import TMP Essential Resources.
            if (EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources")) return;

            // Fallback: import the package file shipped inside the UI package directly.
            foreach (string packageName in new[] { "com.unity.ugui", "com.unity.textmeshpro" })
            {
                UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + packageName);
                if (info == null) continue;

                string path = info.resolvedPath + "/Package Resources/TMP Essential Resources.unitypackage";
                if (File.Exists(path))
                {
                    AssetDatabase.ImportPackage(path, true);
                    return;
                }
            }

            EditorUtility.DisplayDialog(
                "Brickbreaker Base",
                "Couldn't find the TextMesh Pro importer automatically.\n\nUse Window > TextMeshPro > Import TMP Essential Resources.",
                "OK");
        }
    }
}
