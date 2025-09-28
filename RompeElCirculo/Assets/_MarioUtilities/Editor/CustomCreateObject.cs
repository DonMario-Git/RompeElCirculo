using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using UnityEditor;
using UnityEngine;

public class CustomCreateObject
{
    [MenuItem("GameObject/UI/MARIO_FIXED/Input field", false, 10)]
    private static void CrearInputFieldTMP()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/InputField (TMP).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/Text/Default", false, 10)]
    private static void CrearTextoTMP()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/NewText (TMP).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/Text/Encabezado", false, 10)]
    private static void CrearEncabezadoTMP()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/Encabezado (Text).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/Text/Titulo", false, 10)]
    private static void CrearTituloTMP()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/Titulo (TMP).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/Text/Parrafo", false, 10)]
    private static void CrearParrafoTMP()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/Parrafo (TMP).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/Text/Mensaje", false, 10)]
    private static void CrearMensajeTMP()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/Mensaje (TMP).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/Button/Button 1", false, 10)]
    private static void CrearBoton1()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/Button 1 (TMP).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/Button/Button 2", false, 10)]
    private static void CrearBoton2()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/Button 2 (TMP).prefab");
    }

    [MenuItem("GameObject/UI/MARIO_FIXED/SeleccionMultiple", false, 10)]
    private static void CrearSeleccionMultiple()
    {
        IntancePrefab("Assets/_MarioUtilities/Prefabs/PreguntaSelMultiple.prefab");
    }

    private static void IntancePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            // si se quiere desempaquetar los prefabs
            //PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

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