using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

#if UNITY_EDITOR
[CustomEditor(typeof(MaskableGraphic), true)]
public class ProceduralImageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Only show custom UI for ProceduralImage (by type name) to avoid referencing runtime type directly
        var targetType = target.GetType();
        if (targetType.Name != "ProceduralImage")
        {
            base.OnInspectorGUI();
            return;
        }

        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        var colorProp = serializedObject.FindProperty("m_Color");
        var radiusProp = serializedObject.FindProperty("m_Radius");
        var segProp = serializedObject.FindProperty("m_Segments");
        var spriteProp = serializedObject.FindProperty("m_Sprite");
        var rTL = serializedObject.FindProperty("m_RadiusTopLeft");
        var rTR = serializedObject.FindProperty("m_RadiusTopRight");
        var rBL = serializedObject.FindProperty("m_RadiusBottomLeft");
        var rBR = serializedObject.FindProperty("m_RadiusBottomRight");
        var raycastProp = serializedObject.FindProperty("m_RaycastTargetLocal");
        var maskableProp = serializedObject.FindProperty("m_MaskableLocal");
        var saltProp = serializedObject.FindProperty("m_CustomSalt");

        if (colorProp != null) EditorGUILayout.PropertyField(colorProp);
        if (segProp != null) EditorGUILayout.PropertyField(segProp, new GUIContent("Segments"));

        EditorGUILayout.LabelField("Per-corner radii", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rTL, new GUIContent("Top Left"));
        EditorGUILayout.PropertyField(rTR, new GUIContent("Top Right"));
        EditorGUILayout.PropertyField(rBL, new GUIContent("Bottom Left"));
        EditorGUILayout.PropertyField(rBR, new GUIContent("Bottom Right"));
        GUILayout.Space(6);
        if (raycastProp != null) EditorGUILayout.PropertyField(raycastProp, new GUIContent("Raycast Target"));
        if (maskableProp != null) EditorGUILayout.PropertyField(maskableProp, new GUIContent("Maskable"));

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            // Call SetVerticesDirty / SetMaterialDirty via reflection to avoid direct type dependency
            var setVerts = targetType.GetMethod("SetVerticesDirty", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var setMat = targetType.GetMethod("SetMaterialDirty", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            setVerts?.Invoke(target, null);
            setMat?.Invoke(target, null);
            EditorUtility.SetDirty(target);
        }

        
    }

    [MenuItem("GameObject/UI/Procedural Image", false, 2000)]
    public static void CreateProceduralImageMenu()
    {
        CreateProceduralImage();
    }

    private static void CreateProceduralImage()
    {
        // Create a GameObject with ProceduralImage via AddComponent to avoid compile dependency here
        GameObject go = new GameObject("Procedural Image", typeof(RectTransform), typeof(CanvasRenderer));
        Canvas canvas = null;
#if UNITY_2022_2_OR_NEWER
        canvas = FindAnyObjectByType<Canvas>();
#else
        canvas = Object.FindObjectOfType<Canvas>();
#endif
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Procedural Image");
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 30);

        // Add ProceduralImage component by name
        System.Type compType = System.Type.GetType("ProceduralImage");
        if (compType == null)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                System.Type[] types = null;
                try { types = asm.GetTypes(); } catch { }
                if (types == null) continue;
                foreach (var t in types)
                {
                    if (t.Name == "ProceduralImage") { compType = t; break; }
                }
                if (compType != null) break;
            }
        }

        if (compType != null)
        {
            Undo.AddComponent(go, compType);
        }

        Selection.activeGameObject = go;
    }
}
#endif
