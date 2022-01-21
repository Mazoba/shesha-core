using Abp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Context of the dynamic DTO building process, is used by the <see cref="IDynamicDtoTypeBuilder"/>
    /// </summary>
    public class DynamicDtoTypeBuildingContext : IHasNamePrefixStack
    {
        /// <summary>
        /// Model type. Typically it's a type of entity
        /// </summary>
        public Type ModelType { get; set; }

        /// <summary>
        /// Property filter. Return true if the field should be included into the result type
        /// </summary>
        public Func<string, bool> PropertyFilter { get; set; }

        /*
        /// <summary>
        /// Fired when new property is created
        /// </summary>
        public event EventHandler OnPropertyCreated;

        /// <summary>
        /// Fired when new class is created
        /// </summary>
        public event EventHandler OnClassCreated;

        /// <summary>
        /// Fire the <see cref="OnClassCreated"/> event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="e"></param>
        public void FireClassCreated(Type type, EventArgs e) 
        {
            OnClassCreated?.Invoke(type, e);
        }

        /// <summary>
        /// Fire the <see cref="OnPropertyCreated"/> event
        /// </summary>
        /// <param name="type"></param>
        /// <param name="e"></param>
        public void FirePropertyCreated(Type type, EventArgs e)
        {
            OnClassCreated?.Invoke(type, e);
        }
        */
        public Dictionary<string, Type> Classes = new Dictionary<string, Type>();
        public void ClassCreated(Type @class) 
        {
            Classes.Add(CurrentPrefix, @class);
        }

        #region IHasNamePrefixStack implementation

        private Stack<string> _namePrefixStack = new Stack<string>();

        public IDisposable OpenNamePrefix(string prefix)
        {
            _namePrefixStack.Push(prefix);

            return new DisposeAction(() => CloseNamePrefix(prefix));
        }

        private void CloseNamePrefix(string prefix)
        {
            var closedPrefix = _namePrefixStack.Pop();
            if (prefix != closedPrefix)
                throw new Exception($"Name prefix closed in a wrong order. Expected prefix value: '{prefix}', actual: '{closedPrefix}'. Make sure that you use dispose results of the {nameof(OpenNamePrefix)} method");
        }
        public string CurrentPrefix {
            get {
                var value = _namePrefixStack.Any()
                    ? _namePrefixStack.Aggregate((next, current) => current + "." + next)
                    : string.Empty;
                return value;
            }
        }

        #endregion
    }
}

/// <param name="baseType">DTO type</param>
/// <param name="propertyFilter">Property filter. Return true if the field should be included into the result type</param>