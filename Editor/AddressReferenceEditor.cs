using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SAGE.AddressablesUI.Editor
{
    [Serializable]
    public sealed class AddressReferenceConfig
    {
        public string key;
        public string[] labels = Array.Empty<string>();
        public bool isAddressable = true;
        public UnityEngine.Object asset;
        public AssetReference reference;
    }

    [CreateAssetMenu(menuName = "SAGE/Addressables UI/Address Reference", fileName = "AddressReference")]
    public sealed class AddressReferenceEditor : ScriptableObject
    {
        public string group = "BaseScreen";
        public string className = "BaseScreenAddress";
        public string getNameMethod = "GetName";
        public List<AddressReferenceConfig> addresses = new List<AddressReferenceConfig>();

        /// <summary>Scans main assets; appends by default and skips existing keys/GUIDs.</summary>
        public int InsertFromFolder(string folder, string filter = "t:Prefab", string keyPrefix = "",
            bool replace = false)
        {
            var assets = LoadAssetsAtPath<UnityEngine.Object>(folder, filter);
            if (addresses == null || replace) addresses = new List<AddressReferenceConfig>();
            var count = 0;
            foreach (var item in assets)
            {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
                var key = keyPrefix + item.name;
                if (addresses.Exists(x => x != null && (x.key == key ||
                    (x.reference != null && x.reference.AssetGUID == guid)))) continue;
                addresses.Add(new AddressReferenceConfig
                {
                    key = key, reference = new AssetReference(guid),
                    labels = new[] { group }, isAddressable = true
                });
                count++;
            }
            EditorUtility.SetDirty(this);
            return count;
        }

        public static List<T> LoadAssetsAtPath<T>(string folder, string filter = null) where T : UnityEngine.Object
        {
            folder = folder?.Replace('\\', '/').TrimEnd('/');
            if (folder == null || !folder.StartsWith("Assets/", StringComparison.Ordinal) ||
                !AssetDatabase.IsValidFolder(folder))
                throw new ArgumentException("Choose an existing folder below Assets/.", nameof(folder));
            var guids = AssetDatabase.FindAssets(filter ?? ("t:" + typeof(T).Name), new[] { folder });
            Array.Sort(guids, StringComparer.Ordinal);
            var result = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) result.Add(asset);
            }
            return result;
        }
    }
}
