using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using UnityEditor;
using UnityEngine;

public class CustomCreateObject
{
    [MenuItem("GameObject/UI/MARIO_FIXED/InputField (TMP)", false, 10)]
    private static void CrearInputFieldTMP()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/InputField (TMP).prefab");
    }

    private static void IntancePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            if (Selection.activeTransform != null)
            {
                instance.transform.SetParent(Selection.activeTransform);
                instance.transform.localPosition = Vector3.zero;
            }

            instance.transform.localScale = Vector3.one;

            Undo.RegisterCreatedObjectUndo(instance, "Create " + instance.name);
            Selection.activeGameObject = instance;
        }
        else
        {
            Debug.LogError("No se encontró el prefab en la ruta especificada.");
        }
    }
}