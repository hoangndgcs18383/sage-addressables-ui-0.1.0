using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace SAGE.AddressablesUI.Editor
{
    /// <summary>Editor main-thread APIs. Call SaveAssets after a batch of RegisterAsset calls.</summary>
    public static class AddressablesEditorApi
    {
        public static AddressableAssetSettings GetOrCreateSettings() =>
            AddressableAssetSettingsDefaultObject.GetSettings(true);

        public static AddressableAssetGroup GetOrCreateGroup(string name, AddressableAssetSettings settings = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Group name is required.");
            settings = settings != null ? settings : GetOrCreateSettings();
            var group = settings.FindGroup(name);
            if (group == null)
                group = settings.CreateGroup(name, false, false, true, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            if (group.ReadOnly) throw new InvalidOperationException("Group is read-only: " + name);
            if (!group.HasSchema<BundledAssetGroupSchema>()) group.AddSchema<BundledAssetGroupSchema>();
            if (!group.HasSchema<ContentUpdateGroupSchema>()) group.AddSchema<ContentUpdateGroupSchema>();
            return group;
        }

        public static AddressableAssetEntry RegisterAsset(UnityEngine.Object asset, string groupName,
            string address, IEnumerable<string> labels = null, bool replaceLabels = false,
            AddressableAssetSettings settings = null)
        {
            return RegisterGuid(GetGuid(asset), groupName, address, labels, replaceLabels, settings);
        }

        public static AddressableAssetEntry RegisterGuid(string guid, string groupName, string address,
            IEnumerable<string> labels = null, bool replaceLabels = false, AddressableAssetSettings settings = null)
        {
            ValidateGuid(guid);
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required.");
            // Materialize and validate before mutating the settings.
            var labelList = new List<string>(labels ?? Array.Empty<string>());
            foreach (var label in labelList)
                if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Labels cannot be empty.");
            settings = settings != null ? settings : GetOrCreateSettings();
            var existing = settings.FindAssetEntry(guid);
            if (existing != null && existing.parentGroup.ReadOnly)
                throw new InvalidOperationException("Asset belongs to a read-only group: " + guid);
            var group = GetOrCreateGroup(groupName, settings);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            if (replaceLabels)
                foreach (var label in new List<string>(entry.labels)) entry.SetLabel(label, false);
            foreach (var label in labelList) entry.SetLabel(label, true, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true, true);
            EditorUtility.SetDirty(group);
            return entry;
        }

        internal static string GetGuid(UnityEngine.Object asset)
        {
            if (asset == null) throw new ArgumentException("Asset is required.");
            if (AssetDatabase.IsSubAsset(asset)) throw new ArgumentException("Use a main asset, not a sub-asset.");
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            ValidateGuid(guid);
            return guid;
        }

        internal static string ValidateGuid(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid ?? "");
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || AssetDatabase.IsValidFolder(path) ||
                path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new ArgumentException("Addressable must be an existing main asset under Assets, outside Resources: " + guid);
            return path;
        }

        internal static string ResourcesPath(UnityEngine.Object asset)
        {
            if (asset == null || AssetDatabase.IsSubAsset(asset)) throw new ArgumentException("Resources main asset is required.");
            var path = AssetDatabase.GetAssetPath(asset).Replace('\\', '/');
            const string marker = "/Resources/";
            var index = path.LastIndexOf(marker, StringComparison.Ordinal);
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || index < 0 || AssetDatabase.IsValidFolder(path))
                throw new ArgumentException("Resources asset must already be inside an Assets/**/Resources/ folder.");
            var relative = path.Substring(index + marker.Length);
            return relative.Substring(0, relative.Length - Path.GetExtension(relative).Length);
        }
    }
}
