using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SAGE.AddressablesUI.Editor
{
    [CreateAssetMenu(menuName = "SAGE/Addressables UI/Address Library", fileName = "AddressLibTool")]
    public sealed class AddressLibTool : ScriptableObject
    {
        public string generatePath = "Assets/Generated/Addresses";
        public string namespaceName = "Game.Addresses";
        public List<AddressReferenceEditor> screenReferences = new List<AddressReferenceEditor>();

        /// <summary>Validates every table and output before registering assets or generating scripts.</summary>
        public string[] Build() => Build(screenReferences, generatePath, namespaceName);

        /// <summary>Explicit discovery, useful for menu actions or CI. Does not run on asset import.</summary>
        public static string[] BuildAllReferences(string outputFolder, string namespaceName)
        {
            var guids = AssetDatabase.FindAssets("t:AddressReferenceEditor");
            Array.Sort(guids, StringComparer.Ordinal);
            var tables = new List<AddressReferenceEditor>();
            foreach (var guid in guids)
                tables.Add(AssetDatabase.LoadAssetAtPath<AddressReferenceEditor>(AssetDatabase.GUIDToAssetPath(guid)));
            return Build(tables, outputFolder, namespaceName);
        }

        public static string[] Build(IReadOnlyList<AddressReferenceEditor> tables, string outputFolder,
            string namespaceName, AddressableAssetSettings settings = null)
        {
            if (tables == null || tables.Count == 0) throw new ArgumentException("Add at least one reference table.");
            var folder = ValidateOutputFolder(outputFolder);
            var plans = new List<Plan>();
            var classes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var groups = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var table in tables)
            {
                if (table == null || string.IsNullOrWhiteSpace(table.group) || table.addresses == null)
                    throw new ArgumentException("Each reference table needs a group and address list.");
                AddressCodeGenerator.ValidateIdentifier(table.className);
                if (!classes.Add(table.className)) throw new ArgumentException("Duplicate class: " + table.className);
                var entries = new List<AddressCodeEntry>();
                foreach (var item in table.addresses)
                {
                    if (item == null) throw new ArgumentException("Reference table contains a null row.");
                    string address;
                    if (item.isAddressable)
                    {
                        address = item.reference != null && !string.IsNullOrWhiteSpace(item.reference.AssetGUID)
                            ? item.reference.AssetGUID : AddressablesEditorApi.GetGuid(item.asset);
                        AddressablesEditorApi.ValidateGuid(address);
                        if (groups.TryGetValue(address, out var existingGroup) && existingGroup != table.group)
                            throw new ArgumentException("Same asset assigned to different groups: " + item.key);
                        groups[address] = table.group;
                        foreach (var label in item.labels ?? Array.Empty<string>())
                            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Empty label: " + item.key);
                    }
                    else address = AddressablesEditorApi.ResourcesPath(item.asset);
                    entries.Add(new AddressCodeEntry { Key = item.key, Address = address, IsResource = !item.isAddressable });
                }
                var source = AddressCodeGenerator.GenerateSource(namespaceName, table.className, table.getNameMethod, entries);
                var file = Path.Combine(folder, table.className + ".cs");
                if (File.Exists(file) && !File.ReadAllText(file).StartsWith(AddressCodeGenerator.Header, StringComparison.Ordinal))
                    throw new InvalidOperationException("Output file is not owned by this generator: " + file);
                plans.Add(new Plan { Table = table, Entries = entries, Source = source, File = file });
            }

            // No mutations above this point. Register aliases by GUID, keeping the first key as canonical address.
            settings = settings != null ? settings : AddressablesEditorApi.GetOrCreateSettings();
            foreach (var plan in plans)
            {
                var group = settings.FindGroup(plan.Table.group);
                if (group != null && group.ReadOnly) throw new InvalidOperationException("Read-only group: " + group.Name);
                foreach (var item in plan.Entries)
                {
                    if (item.IsResource) continue;
                    var entry = settings.FindAssetEntry(item.Address);
                    if (entry != null && entry.parentGroup.ReadOnly)
                        throw new InvalidOperationException("Asset belongs to a read-only group: " + item.Key);
                }
            }
            var assigned = new HashSet<string>(StringComparer.Ordinal);
            var files = new List<string>();
            foreach (var plan in plans)
            {
                for (var i = 0; i < plan.Entries.Count; i++)
                {
                    var entry = plan.Entries[i];
                    if (entry.IsResource) continue;
                    var current = settings.FindAssetEntry(entry.Address);
                    var key = assigned.Add(entry.Address) ? entry.Key : current.address;
                    AddressablesEditorApi.RegisterGuid(entry.Address, plan.Table.group, key,
                        plan.Table.addresses[i].labels, false, settings);
                }
                Directory.CreateDirectory(folder);
                if (!File.Exists(plan.File) || File.ReadAllText(plan.File) != plan.Source)
                    File.WriteAllText(plan.File, plan.Source, new UTF8Encoding(false));
                files.Add(plan.File.Replace('\\', '/'));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return files.ToArray();
        }

        private sealed class Plan
        {
            internal AddressReferenceEditor Table;
            internal List<AddressCodeEntry> Entries;
            internal string Source;
            internal string File;
        }

        private static string ValidateOutputFolder(string folder)
        {
            folder = folder?.Replace('\\', '/').TrimEnd('/');
            if (folder == null || !folder.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Output folder must be below Assets/.");
            var project = Directory.GetParent(Application.dataPath).FullName;
            var full = Path.GetFullPath(Path.Combine(project, folder));
            var assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output folder cannot escape Assets/.");
            return full;
        }
    }
}
