using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace net.puk06.SkinnedMeshBoneViewer
{
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    [CanEditMultipleObjects]
    public class SkinnedMeshBoneViewer : Editor
    {
        const string BonesPropertyName = "m_Bones";
        const string DefaultEditorTypeName = "UnityEditor.SkinnedMeshRendererEditor";

        private SerializedProperty bonesProperty;
        private Editor defaultEditor;
        private bool showBoneDependencies;

        private void OnEnable()
        {
            bonesProperty = serializedObject.FindProperty(BonesPropertyName);

            var defaultEditorType = FindDefaultEditorType();
            if (defaultEditorType != null) defaultEditor = CreateEditor(targets, defaultEditorType);
        }

        private void OnDisable()
        {
            if (defaultEditor != null)
            {
                DestroyImmediate(defaultEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            if (defaultEditor != null)
            {
                defaultEditor.OnInspectorGUI();
            }
            else
            {
                DrawDefaultInspector();
            }

            if (bonesProperty != null && bonesProperty.arraySize > 0)
            {
                DrawBoneSection();
            }
        }

        private void DrawBoneSection()
        {
            var usedBoneIndices = GetUsedBoneIndices();
            if (usedBoneIndices == null)
            {
                EditorGUILayout.HelpBox("No bone weights found in the mesh.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            showBoneDependencies = EditorGUILayout.Foldout(showBoneDependencies, "Used Bone Dependencies (sorted by influence)", true, EditorStyles.foldoutHeader);
            if (!showBoneDependencies) return;

            var countLabel = string.Format("Bones (used {0}/{1})", usedBoneIndices.Count, bonesProperty.arraySize);
            EditorGUILayout.LabelField(countLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.indentLevel++;
                for (int index = 0; index < bonesProperty.arraySize; index++)
                {
                    if (!usedBoneIndices.Contains(index)) continue;

                    var bone = bonesProperty.GetArrayElementAtIndex(index);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(bone.objectReferenceValue, typeof(Transform), true);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }

        private List<int> GetUsedBoneIndices()
        {
            var renderer = target as SkinnedMeshRenderer;
            if (renderer == null) return null;

            var mesh = renderer.sharedMesh;
            if (mesh == null || mesh.boneWeights == null || mesh.boneWeights.Length == 0)
            {
                return null;
            }

            var boneInfluences = new Dictionary<int, float>();
            var weights = mesh.boneWeights;
            for (int vertex = 0; vertex < weights.Length; vertex++)
            {
                var weight = weights[vertex];
                AddBoneInfluence(boneInfluences, weight.boneIndex0, weight.weight0);
                AddBoneInfluence(boneInfluences, weight.boneIndex1, weight.weight1);
                AddBoneInfluence(boneInfluences, weight.boneIndex2, weight.weight2);
                AddBoneInfluence(boneInfluences, weight.boneIndex3, weight.weight3);
            }

            var usedIndices = new List<int>(boneInfluences.Keys);
            usedIndices.Sort((left, right) =>
            {
                var influenceComparison = boneInfluences[right].CompareTo(boneInfluences[left]);
                return influenceComparison != 0 ? influenceComparison : left.CompareTo(right);
            });
            return usedIndices;
        }

        private static void AddBoneInfluence(Dictionary<int, float> boneInfluences, int boneIndex, float weight)
        {
            if (weight <= 0f) return;

            boneInfluences.TryGetValue(boneIndex, out float influence);
            boneInfluences[boneIndex] = influence + weight;
        }

        private static Type FindDefaultEditorType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var editorType = assembly.GetType(DefaultEditorTypeName);
                if (editorType != null && typeof(Editor).IsAssignableFrom(editorType))
                {
                    return editorType;
                }
            }

            return null;
        }
    }
}
