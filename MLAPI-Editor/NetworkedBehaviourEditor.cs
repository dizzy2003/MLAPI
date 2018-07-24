﻿using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NetworkedBehaviour), true)]
    [CanEditMultipleObjects]
    public class NetworkedBehaviourEditor : Editor
    {
        private bool initialized;
        private List<string> networkedVarNames = new List<string>();
        private Dictionary<string, FieldInfo> networkedVarFields = new Dictionary<string, FieldInfo>();
        private Dictionary<string, object> networkedVarObjects = new Dictionary<string, object>();
        
        private GUIContent networkedVarLabelGuiContent;

        private void Init(MonoScript script)
        {
            initialized = true;
            
            networkedVarNames.Clear();
            networkedVarFields.Clear();
            networkedVarObjects.Clear();

            networkedVarLabelGuiContent = new GUIContent("NetworkedVar", "This variable is a NetworkedVar. It can not be serialized and can only be changed during runtime.");

            FieldInfo[] fields = script.GetClass().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                Type ft = fields[i].FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(NetworkedVar<>))
                {
                    networkedVarNames.Add(fields[i].Name);
                    networkedVarFields.Add(fields[i].Name, fields[i]);
                }
            }
        }

        void RenderNetworkedVar(int index)
        {
            if (!networkedVarFields.ContainsKey(networkedVarNames[index]))
            {
                serializedObject.Update();
                SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty == null)
                    return;

                MonoScript targetScript = scriptProperty.objectReferenceValue as MonoScript;
                Init(targetScript);
            }
            Type type = networkedVarFields[networkedVarNames[index]].GetValue(target).GetType();
            Type genericType = type.GetGenericArguments()[0];

            EditorGUILayout.BeginHorizontal();
            if (genericType == typeof(string))
            {
                NetworkedVar<string> var = (NetworkedVar<string>)networkedVarFields[networkedVarNames[index]].GetValue(target);
                var.Value = EditorGUILayout.TextField(networkedVarNames[index], var.Value);
            }
            else if (genericType.IsValueType)
            {
                MethodInfo method = typeof(NetworkedBehaviourEditor).GetMethod("RenderNetworkedVarValueType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                MethodInfo genericMethod = method.MakeGenericMethod(genericType);
                genericMethod.Invoke(this, new object[] { (object)index });
            }
            else
            {
                EditorGUILayout.LabelField("Type not renderable");
            }
            GUILayout.Label(networkedVarLabelGuiContent, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(networkedVarLabelGuiContent).x));
            EditorGUILayout.EndHorizontal();
        }

        void RenderNetworkedVarValueType<T>(int index) where T : struct
        {
            NetworkedVar<T> var = (NetworkedVar<T>)networkedVarFields[networkedVarNames[index]].GetValue(target);
            Type type = typeof(T);
            object val = var.Value;
            string name = networkedVarNames[index];
            if (type == typeof(int))
                val = EditorGUILayout.IntField(name, (int)val);
            else if (type == typeof(uint))
                val = EditorGUILayout.LongField(name, (long)val);
            else if (type == typeof(short))
                val = EditorGUILayout.IntField(name, (int)val);
            else if (type == typeof(ushort))
                val = EditorGUILayout.IntField(name, (int)val);
            else if (type == typeof(sbyte))
                val = EditorGUILayout.IntField(name, (int)val);
            else if (type == typeof(byte))
                val = EditorGUILayout.IntField(name, (int)val);
            else if (type == typeof(long))
                val = EditorGUILayout.LongField(name, (long)val);
            else if (type == typeof(ulong))
                val = EditorGUILayout.LongField(name, (long)val);
            else if (type == typeof(bool))
                val = EditorGUILayout.Toggle(name, (bool)val);
            else if (type == typeof(string))
                val = EditorGUILayout.TextField(name, (string)val);
            else
                EditorGUILayout.LabelField("Type not renderable");

            var.Value = (T)val;
        }

        public override void OnInspectorGUI()
        {
            if (!initialized)
            {
                serializedObject.Update();
                SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty == null)
                    return;

                MonoScript targetScript = scriptProperty.objectReferenceValue as MonoScript;
                Init(targetScript);
            }

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            for (int i = 0; i < networkedVarNames.Count; i++)
                RenderNetworkedVar(i);

            SerializedProperty property = serializedObject.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.name == "m_Script")
                        EditorGUI.BeginDisabledGroup(true);

                    EditorGUILayout.PropertyField(property, true);
                                   
                    if (property.name == "m_Script")
                        EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(property, true);
                    EditorGUILayout.EndHorizontal();
                }
                expanded = false;
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
    }
}
