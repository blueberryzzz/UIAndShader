using System.Linq;
using UnityEngine;
using UnityEditor.AnimatedValues;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ChunkDisappearImageUI), true)]
public class ChunkDisappearImageUIEditor : ImageEditor
{
    SerializedProperty m_Sprite;

    SerializedProperty m_Speed;
    SerializedProperty m_TargetX;
    SerializedProperty m_TargetY;
    SerializedProperty m_SubRectX;
    SerializedProperty m_SubRectY;
    SerializedProperty m_Interval;
    SerializedProperty m_Acceleration;

    protected override void OnEnable()
    {
        base.OnEnable();

        m_Sprite = serializedObject.FindProperty("m_Sprite");
        m_Speed = serializedObject.FindProperty("Speed");
        m_TargetX = serializedObject.FindProperty("TargetX");
        m_TargetY = serializedObject.FindProperty("TargetY");
        m_SubRectX = serializedObject.FindProperty("SubRectX");
        m_SubRectY = serializedObject.FindProperty("SubRectY");
        m_Interval = serializedObject.FindProperty("Interval");
        m_Acceleration = serializedObject.FindProperty("Acceleration");
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SpriteGUI();
        AppearanceControlsGUI();
        RaycastControlsGUI();
        bool showNativeSize = m_Sprite.objectReferenceValue != null;
        m_ShowNativeSize.target = showNativeSize;
        NativeSizeButtonGUI();
        EditorGUILayout.PropertyField(m_Speed);
        EditorGUILayout.PropertyField(m_TargetX);
        EditorGUILayout.PropertyField(m_TargetY);
        EditorGUILayout.PropertyField(m_SubRectX);
        EditorGUILayout.PropertyField(m_SubRectY);
        EditorGUILayout.PropertyField(m_Interval);
        EditorGUILayout.PropertyField(m_Acceleration);
        this.serializedObject.ApplyModifiedProperties();
    }
}
