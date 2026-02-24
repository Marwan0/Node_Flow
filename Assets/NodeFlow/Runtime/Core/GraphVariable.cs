using System;
using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// Represents a variable stored in a NodeGraph
    /// </summary>
    [Serializable]
    public class GraphVariable
    {
        [SerializeField]
        private string _name = "";
        
        [SerializeField]
        private VariableType _type = VariableType.Bool;
        
        [SerializeField]
        private string _value = "";

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public VariableType Type
        {
            get => _type;
            set => _type = value;
        }

        public string Value
        {
            get => _value;
            set => _value = value;
        }

        // === Type-safe getters ===

        public bool GetBoolValue()
        {
            return _value?.ToLower() == "true" || _value == "1";
        }

        public int GetIntValue()
        {
            if (int.TryParse(_value, out int result))
                return result;
            return 0;
        }

        public float GetFloatValue()
        {
            if (float.TryParse(_value, out float result))
                return result;
            return 0f;
        }

        public string GetStringValue()
        {
            return _value ?? "";
        }

        // === Type-safe setters ===

        public void SetBoolValue(bool value)
        {
            _value = value ? "true" : "false";
        }

        public void SetIntValue(int value)
        {
            _value = value.ToString();
        }

        public void SetFloatValue(float value)
        {
            _value = value.ToString();
        }

        public void SetStringValue(string value)
        {
            _value = value ?? "";
        }

        // === Factory methods ===

        public static GraphVariable CreateBool(string name, bool defaultValue = false)
        {
            var variable = new GraphVariable
            {
                _name = name,
                _type = VariableType.Bool
            };
            variable.SetBoolValue(defaultValue);
            return variable;
        }

        public static GraphVariable CreateInt(string name, int defaultValue = 0)
        {
            var variable = new GraphVariable
            {
                _name = name,
                _type = VariableType.Int
            };
            variable.SetIntValue(defaultValue);
            return variable;
        }

        public static GraphVariable CreateFloat(string name, float defaultValue = 0f)
        {
            var variable = new GraphVariable
            {
                _name = name,
                _type = VariableType.Float
            };
            variable.SetFloatValue(defaultValue);
            return variable;
        }

        public static GraphVariable CreateString(string name, string defaultValue = "")
        {
            var variable = new GraphVariable
            {
                _name = name,
                _type = VariableType.String
            };
            variable.SetStringValue(defaultValue);
            return variable;
        }
    }

    /// <summary>
    /// Variable types (shared with SetVariableNode)
    /// </summary>
    public enum VariableType
    {
        Bool,
        Int,
        Float,
        String
    }
}

