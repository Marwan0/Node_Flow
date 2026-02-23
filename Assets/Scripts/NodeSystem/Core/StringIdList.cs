using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// A serializable string-ID selector that works like an enum.
    /// - You define IDs in a list (via inspector or code)
    /// - You pick one from a dropdown (like navigating enum values)
    /// 
    /// Usage in any node or MonoBehaviour:
    ///   [SerializeField] private StringIdSelector _mySelector = new StringIdSelector();
    /// 
    /// The custom PropertyDrawer draws:
    ///   [Foldout IDs list]  [▼ dropdown to pick one]
    /// </summary>
    [Serializable]
    public class StringIdSelector
    {
        [SerializeField]
        private List<string> _ids = new List<string>();

        [SerializeField]
        private string _selectedId = "";

        // ── Read API ──────────────────────────────────────────

        /// <summary>Currently selected ID</summary>
        public string SelectedId
        {
            get => _selectedId;
            set
            {
                if (_ids != null && _ids.Contains(value))
                    _selectedId = value;
                else if (string.IsNullOrEmpty(value))
                    _selectedId = "";
            }
        }

        /// <summary>All available IDs</summary>
        public IReadOnlyList<string> Ids => _ids ?? (_ids = new List<string>());

        /// <summary>Number of IDs in the list</summary>
        public int Count => _ids?.Count ?? 0;

        /// <summary>True if an ID is currently selected</summary>
        public bool HasSelection => !string.IsNullOrEmpty(_selectedId);

        /// <summary>True if the selected ID still exists in the list</summary>
        public bool IsSelectionValid()
        {
            if (string.IsNullOrEmpty(_selectedId) || _ids == null) return false;
            return _ids.Contains(_selectedId);
        }

        /// <summary>Index of the selected ID (-1 if none)</summary>
        public int GetSelectedIndex()
        {
            if (_ids == null || string.IsNullOrEmpty(_selectedId)) return -1;
            return _ids.IndexOf(_selectedId);
        }

        /// <summary>Check if the list contains an ID</summary>
        public bool Contains(string id)
        {
            if (string.IsNullOrEmpty(id) || _ids == null) return false;
            return _ids.Contains(id);
        }

        /// <summary>Get ID at index (empty string if out of range)</summary>
        public string GetAt(int index)
        {
            if (_ids == null || index < 0 || index >= _ids.Count) return string.Empty;
            return _ids[index];
        }

        /// <summary>Get index of an ID (-1 if not found)</summary>
        public int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id) || _ids == null) return -1;
            return _ids.IndexOf(id);
        }

        /// <summary>Get all IDs as a string array (handy for popups)</summary>
        public string[] GetIdsArray()
        {
            if (_ids == null || _ids.Count == 0) return Array.Empty<string>();
            return _ids.ToArray();
        }

        // ── Selection helpers ─────────────────────────────────

        /// <summary>Select by index</summary>
        public void SelectByIndex(int index)
        {
            if (_ids == null || index < 0 || index >= _ids.Count) return;
            _selectedId = _ids[index];
        }

        /// <summary>Select the first ID</summary>
        public void SelectFirst()
        {
            _selectedId = (_ids != null && _ids.Count > 0) ? _ids[0] : "";
        }

        /// <summary>Select a random ID</summary>
        public void SelectRandom()
        {
            if (_ids != null && _ids.Count > 0)
                _selectedId = _ids[UnityEngine.Random.Range(0, _ids.Count)];
        }

        // ── Mutation API ──────────────────────────────────────

        /// <summary>Add an ID (no duplicates). Auto-selects if nothing selected yet.</summary>
        public void AddId(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_ids == null) _ids = new List<string>();
            if (!_ids.Contains(id))
                _ids.Add(id);
            if (string.IsNullOrEmpty(_selectedId))
                _selectedId = id;
        }

        /// <summary>Remove an ID. Clears selection if it was the selected one.</summary>
        public void RemoveId(string id)
        {
            if (_ids == null) return;
            _ids.Remove(id);
            if (_selectedId == id)
                _selectedId = _ids.Count > 0 ? _ids[0] : "";
        }

        /// <summary>Clear all IDs and selection</summary>
        public void Clear()
        {
            _ids?.Clear();
            _selectedId = "";
        }

        /// <summary>Replace all IDs at once</summary>
        public void SetIds(IEnumerable<string> ids)
        {
            _ids = new List<string>();
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (!string.IsNullOrEmpty(id) && !_ids.Contains(id))
                        _ids.Add(id);
                }
            }
            // Re-validate selection
            if (!string.IsNullOrEmpty(_selectedId) && !_ids.Contains(_selectedId))
                _selectedId = _ids.Count > 0 ? _ids[0] : "";
        }

        // ── Factory ───────────────────────────────────────────

        /// <summary>Create a selector pre-filled with IDs</summary>
        public static StringIdSelector Create(params string[] ids)
        {
            var s = new StringIdSelector();
            if (ids != null && ids.Length > 0)
            {
                s.SetIds(ids);
                s._selectedId = ids[0];
            }
            return s;
        }
    }
}
