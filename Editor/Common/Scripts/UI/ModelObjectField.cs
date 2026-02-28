using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PonyuDev.SherpaOnnx.Editor.Common.UI
{
    /// <summary>
    /// ObjectField-like control for model file paths.
    /// Displays the file as a Unity asset, validates that the selected file
    /// belongs to the model directory, and opens a file dialog scoped to that directory.
    /// </summary>
    internal sealed class ModelObjectField : VisualElement
    {
        private readonly ObjectField _field;
        private readonly string _modelDir;
        private readonly string _label;
        private readonly string _extension;
        private readonly string _keyword;
        private readonly bool _isFolder;
        private readonly bool _isRequired;
        private Action<string> _onChanged;

        private const string RequiredEmptyClass = "model-field--required-empty";
        private const string RequiredEmptyTooltip = "This field is required";

        internal ModelObjectField(string label, string value, string modelDir,
            string extension = "", string keyword = "",
            bool isFolder = false, bool isRequired = false)
        {
            _label = label;
            _modelDir = modelDir.Replace('\\', '/');
            _extension = extension ?? "";
            _keyword = keyword ?? "";
            _isFolder = isFolder;
            _isRequired = isRequired;

            _field = new ObjectField(label);
            _field.objectType = typeof(Object);
            _field.allowSceneObjects = false;

            if (!string.IsNullOrEmpty(value))
            {
                string assetPath = CombinePath(_modelDir, value);
                _field.value = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            }

            _field.RegisterValueChangedCallback(HandleValueChanged);
            Add(_field);

            InterceptPickerButton();
            UpdateRequiredState(value);
        }

        internal void RegisterFileChangedCallback(Action<string> callback)
        {
            _onChanged = callback;
        }

        // ── Picker interception ──

        private void InterceptPickerButton()
        {
            _field.RegisterCallback<MouseDownEvent>(HandleMouseDown, TrickleDown.TrickleDown);
        }

        private void HandleMouseDown(MouseDownEvent evt)
        {
            var target = evt.target as VisualElement;
            if (target == null) return;

            bool isPickerButton = false;
            var current = target;
            while (current != null && current != _field)
            {
                if (current.ClassListContains("unity-object-field__selector"))
                {
                    isPickerButton = true;
                    break;
                }
                current = current.parent;
            }

            if (!isPickerButton) return;

            evt.StopImmediatePropagation();
            evt.PreventDefault();

            string fullDir = Path.GetFullPath(_modelDir);
            string path = _isFolder
                ? EditorUtility.OpenFolderPanel(_label, fullDir, "")
                : EditorUtility.OpenFilePanel(_label, fullDir, _extension);
            if (string.IsNullOrEmpty(path)) return;

            string assetPath = AbsoluteToAssetPath(path);
            if (assetPath == null) return;

            string relativeName = ExtractRelativeName(assetPath);
            if (relativeName == null || !IsValidEntry(relativeName)) return;

            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj == null) return;

            _field.SetValueWithoutNotify(obj);
            _onChanged?.Invoke(relativeName);
            UpdateRequiredState(relativeName);
        }

        // ── ObjectField change handler ──

        private void HandleValueChanged(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null)
            {
                _onChanged?.Invoke("");
                UpdateRequiredState("");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(evt.newValue);
            string relativeName = ExtractRelativeName(assetPath);

            if (relativeName == null || !IsValidEntry(relativeName))
            {
                _field.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            _onChanged?.Invoke(relativeName);
            UpdateRequiredState(relativeName);
        }

        // ── Helpers ──

        private void UpdateRequiredState(string value)
        {
            if (!_isRequired) return;

            if (string.IsNullOrEmpty(value))
            {
                AddToClassList(RequiredEmptyClass);
                tooltip = RequiredEmptyTooltip;
            }
            else
            {
                RemoveFromClassList(RequiredEmptyClass);
                tooltip = "";
            }
        }

        private static string AbsoluteToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return null;
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        private string ExtractRelativeName(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            string normalized = assetPath.Replace('\\', '/');
            string prefix = _modelDir + "/";

            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            return normalized.Substring(prefix.Length);
        }

        private bool IsValidEntry(string name)
        {
            if (!_isFolder && !string.IsNullOrEmpty(_extension)
                && !name.EndsWith("." + _extension, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(_keyword)
                && name.IndexOf(_keyword, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return true;
        }

        private static string CombinePath(string dir, string file)
        {
            return (dir + "/" + file).Replace('\\', '/');
        }
    }
}
