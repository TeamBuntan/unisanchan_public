using UnityEditor;
using UnityEngine;
using unisanchan;

public static class UnisanchanProjectSettingsProvider
{
    private const string ScenarioRegistryPath =
        "Assets/unisanchan/Data/ScenarioRegistry.asset";
    
    private const string VariableRegistryPath =
        "Assets/unisanchan/Data/VariableRegistry.asset";

    private static SerializedObject serializedRegistry;
    private static SerializedProperty scenariosProperty;

    private static SerializedObject serializedVariableRegistry;
    private static SerializedProperty variableSettingsProperty;
    private static SerializedProperty variablesProperty;

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        var provider = new SettingsProvider(
            "Project/unisanchan/Scenarios",
            SettingsScope.Project)
        {
            label = "Scenarios",

            guiHandler = _ =>
            {
                ScenarioRegistry registry =
                    AssetDatabase.LoadAssetAtPath<ScenarioRegistry>(
                        ScenarioRegistryPath);

                if (registry == null)
                {
                    EditorGUILayout.HelpBox(
                        "ScenarioRegistry.asset が見つかりません。",
                        MessageType.Error);

                    return;
                }

                if (serializedRegistry == null ||
                    serializedRegistry.targetObject != registry)
                {
                    serializedRegistry = new SerializedObject(registry);
                    scenariosProperty =
                        serializedRegistry.FindProperty("scenarios");
                }

                
                serializedRegistry.Update();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", GUILayout.Width(180));
                EditorGUILayout.LabelField("Source");
                GUILayout.Space(70);
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < scenariosProperty.arraySize; i++)
                {
                    SerializedProperty entry =
                        scenariosProperty.GetArrayElementAtIndex(i);

                    SerializedProperty nameProperty =
                        entry.FindPropertyRelative("Name");

                    SerializedProperty sourceProperty =
                        entry.FindPropertyRelative("Source");

                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(
                        nameProperty,
                        GUIContent.none,
                        GUILayout.Width(180));

                    EditorGUILayout.PropertyField(
                        sourceProperty,
                        GUIContent.none);

                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        scenariosProperty.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    
                    string currentName = nameProperty.stringValue?.Trim() ?? "";

                    if (string.IsNullOrEmpty(currentName))
                    {
                        EditorGUILayout.HelpBox(
                            "Scenario Name が空欄です。",
                            MessageType.Warning);
                    }
                    else
                    {
                        for (int j = 0; j < scenariosProperty.arraySize; j++)
                        {
                            if (j == i)
                                continue;

                            SerializedProperty otherEntry =
                                scenariosProperty.GetArrayElementAtIndex(j);

                            SerializedProperty otherNameProperty =
                                otherEntry.FindPropertyRelative("Name");

                            string otherName =
                                otherNameProperty.stringValue?.Trim() ?? "";

                            if (currentName == otherName)
                            {
                                EditorGUILayout.HelpBox(
                                    $"Scenario Name「{currentName}」が重複しています。",
                                    MessageType.Error);

                                break;
                            }
                        }
                    }
                    
                    if (sourceProperty.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Scenario Source が設定されていません。",
                            MessageType.Warning);
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("+ Add Scenario"))
                {
                    scenariosProperty.InsertArrayElementAtIndex(
                        scenariosProperty.arraySize);

                    SerializedProperty newEntry =
                        scenariosProperty.GetArrayElementAtIndex(
                            scenariosProperty.arraySize - 1);

                    newEntry.FindPropertyRelative("Name").stringValue = "";
                    newEntry.FindPropertyRelative("Source").objectReferenceValue = null;
                }

                serializedRegistry.ApplyModifiedProperties();
            }
        };

        return provider;
    }

    [SettingsProvider]
    public static SettingsProvider CreateVariablesProvider()
    {
        var provider = new SettingsProvider(
            "Project/unisanchan/Variables",
            SettingsScope.Project)
        {
            label = "Variables",

            guiHandler = _ =>
            {
                VariableRegistry registry =
                    AssetDatabase.LoadAssetAtPath<VariableRegistry>(
                        VariableRegistryPath);

                if (registry == null)
                {
                    EditorGUILayout.HelpBox(
                        "VariableRegistry.asset が見つかりません。",
                        MessageType.Error);

                    return;
                }

                if (serializedVariableRegistry == null ||
                    serializedVariableRegistry.targetObject != registry)
                {
                    serializedVariableRegistry =
                        new SerializedObject(registry);

                    variableSettingsProperty =
                        serializedVariableRegistry.FindProperty("settings");
                    
                    variablesProperty =
                        variableSettingsProperty.FindPropertyRelative("Variables");
                }

                serializedVariableRegistry.Update();

                EditorGUILayout.LabelField(
                    $"Variables Count: {variablesProperty.arraySize}");

                Rect headerRect = EditorGUILayout.GetControlRect();

                float typeWidth = 100f;
                float buttonWidth = 24f;
                float spacing = 4f;

                float flexibleWidth =
                    headerRect.width - typeWidth - buttonWidth - spacing * 3f;

                float variableIdWidth = flexibleWidth * 0.5f;
                float initialValueWidth = flexibleWidth - variableIdWidth;

                Rect variableIdHeaderRect = new Rect(
                    headerRect.x,
                    headerRect.y,
                    variableIdWidth,
                    headerRect.height);

                Rect typeHeaderRect = new Rect(
                    variableIdHeaderRect.xMax + spacing,
                    headerRect.y,
                    typeWidth,
                    headerRect.height);

                Rect initialValueHeaderRect = new Rect(
                    typeHeaderRect.xMax + spacing,
                    headerRect.y,
                    initialValueWidth,
                    headerRect.height);

                EditorGUI.LabelField(
                    variableIdHeaderRect,
                    "VariableID");

                EditorGUI.LabelField(
                    typeHeaderRect,
                    "Type");

                EditorGUI.LabelField(
                    initialValueHeaderRect,
                    "InitialValue");

                for (int i = 0; i < variablesProperty.arraySize; i++)
                {
                    SerializedProperty variable =
                        variablesProperty.GetArrayElementAtIndex(i);

                    SerializedProperty variableIdProperty =
                        variable.FindPropertyRelative("VariableID");

                    SerializedProperty typeProperty =
                        variable.FindPropertyRelative("Type");

                    SerializedProperty initialValueProperty =
                        variable.FindPropertyRelative("InitialValue");

                    Rect rowRect = EditorGUILayout.GetControlRect();

                    float rowFlexibleWidth =
                        rowRect.width - typeWidth - buttonWidth - spacing * 3f;

                    float rowVariableIdWidth = rowFlexibleWidth * 0.5f;
                    float rowInitialValueWidth =
                        rowFlexibleWidth - rowVariableIdWidth;

                    Rect variableIdRect = new Rect(
                        rowRect.x,
                        rowRect.y,
                        rowVariableIdWidth,
                        rowRect.height);

                    Rect typeRect = new Rect(
                        variableIdRect.xMax + spacing,
                        rowRect.y,
                        typeWidth,
                        rowRect.height);

                    Rect initialValueRect = new Rect(
                        typeRect.xMax + spacing,
                        rowRect.y,
                        rowInitialValueWidth,
                        rowRect.height);

                    Rect deleteButtonRect = new Rect(
                        initialValueRect.xMax + spacing,
                        rowRect.y,
                        buttonWidth,
                        rowRect.height);

                    EditorGUI.PropertyField(
                        variableIdRect,
                        variableIdProperty,
                        GUIContent.none);

                    int previousTypeIndex = typeProperty.enumValueIndex;

                    EditorGUI.PropertyField(
                        typeRect,
                        typeProperty,
                        GUIContent.none);

                    if (typeProperty.enumValueIndex != previousTypeIndex)
                    {
                        initialValueProperty.stringValue = "";
                    }

                    VariableType currentType =
                        (VariableType)typeProperty.enumValueIndex;

                    if (currentType == VariableType.Bool)
                    {
                        string[] boolOptions =
                        {
                            "未設定",
                            "false",
                            "true"
                        };

                        int selectedIndex = initialValueProperty.stringValue switch
                        {
                            "false" => 1,
                            "true" => 2,
                            _ => 0
                        };

                        int newSelectedIndex =
                            EditorGUI.Popup(
                                initialValueRect,
                                selectedIndex,
                                boolOptions);

                        initialValueProperty.stringValue =
                            newSelectedIndex switch
                            {
                                1 => "false",
                                2 => "true",
                                _ => ""
                            };
                    }
                    else if (currentType == VariableType.Int)
                    {
                        string currentValue =
                            initialValueProperty.stringValue;

                        if (string.IsNullOrEmpty(currentValue))
                        {
                            string input =
                                EditorGUI.TextField(
                                    initialValueRect,
                                    "");

                            if (int.TryParse(input, out int parsedValue))
                            {
                                initialValueProperty.stringValue =
                                    parsedValue.ToString();
                            }
                        }
                        else
                        {
                            int.TryParse(
                                currentValue,
                                out int intValue);

                            int newValue =
                                EditorGUI.IntField(
                                    initialValueRect,
                                    intValue);

                            initialValueProperty.stringValue =
                                newValue.ToString();
                        }
                    }
                    else if (currentType == VariableType.Float)
                    {
                        string currentValue =
                            initialValueProperty.stringValue;

                        if (string.IsNullOrEmpty(currentValue))
                        {
                            string input =
                                EditorGUI.TextField(
                                    initialValueRect,
                                    "");

                            if (float.TryParse(input, out float parsedValue))
                            {
                                initialValueProperty.stringValue =
                                    parsedValue.ToString();
                            }
                        }
                        else
                        {
                            float.TryParse(
                                currentValue,
                                out float floatValue);

                            float newValue =
                                EditorGUI.FloatField(
                                    initialValueRect,
                                    floatValue);

                            initialValueProperty.stringValue =
                                newValue.ToString();
                        }
                    }
                    else if (currentType == VariableType.String)
                    {
                        string newValue =
                            EditorGUI.TextField(
                                initialValueRect,
                                initialValueProperty.stringValue);

                        initialValueProperty.stringValue =
                            newValue;
                    }

                    if (GUI.Button(deleteButtonRect, "-"))
                    {
                        variablesProperty.DeleteArrayElementAtIndex(i);

                        serializedVariableRegistry.ApplyModifiedProperties();

                        break;
                    }

                    string currentVariableId =
                        variableIdProperty.stringValue?.Trim() ?? "";
                    
                    if (string.IsNullOrEmpty(currentVariableId))
                    {
                        EditorGUILayout.HelpBox(
                            "VariableIDを入力してください。",
                            MessageType.Warning);
                    }

                    if (!string.IsNullOrEmpty(currentVariableId))
                    {
                        bool isDuplicate = false;

                        for (int j = 0; j < variablesProperty.arraySize; j++)
                        {
                            if (j == i)
                                continue;

                            SerializedProperty otherVariable =
                                variablesProperty.GetArrayElementAtIndex(j);

                            SerializedProperty otherVariableIdProperty =
                                otherVariable.FindPropertyRelative("VariableID");

                            string otherVariableId =
                                otherVariableIdProperty.stringValue?.Trim() ?? "";

                            if (currentVariableId == otherVariableId)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (isDuplicate)
                        {
                            EditorGUILayout.HelpBox(
                                $"VariableID「{currentVariableId}」が重複しています。",
                                MessageType.Error);
                        }
                    }
                }

                serializedVariableRegistry.ApplyModifiedProperties();
                
                if (GUILayout.Button("+ Add Variable"))
                {
                    int newIndex = variablesProperty.arraySize;

                    variablesProperty.InsertArrayElementAtIndex(newIndex);

                    SerializedProperty newVariable =
                        variablesProperty.GetArrayElementAtIndex(newIndex);

                    newVariable.FindPropertyRelative("VariableID").stringValue = "";

                    newVariable.FindPropertyRelative("Type").enumValueIndex =
                        (int)VariableType.Bool;

                    newVariable.FindPropertyRelative("InitialValue").stringValue =
                        "";

                    newVariable.FindPropertyRelative("Save").enumValueIndex =
                        (int)VariableSaveType.Game;

                    serializedVariableRegistry.ApplyModifiedProperties();
                }
            }
        };

        return provider;
    }
}