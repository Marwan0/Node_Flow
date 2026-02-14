using System;

namespace NodeSystem
{
    /// <summary>
    /// Attribute to mark string fields as variable selectors in the node editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GraphVariableAttribute : Attribute
    {
        /// <summary>
        /// If true, allows creating new variables from this field.
        /// If false, only allows selecting existing variables.
        /// </summary>
        public bool AllowCreation { get; private set; }

        public GraphVariableAttribute(bool create = false)
        {
            AllowCreation = create;
        }
    }
}
