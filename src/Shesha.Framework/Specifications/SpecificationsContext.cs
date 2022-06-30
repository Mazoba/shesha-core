using Abp.Extensions;
using System;

namespace Shesha.Specifications
{
    /// <summary>
    /// Specifications context
    /// </summary>
    public class SpecificationsContext : ISpecificationsContext
    {
        /// inheritedDoc
        public Type SpecificationsType { get; private set; }

        /// inheritedDoc
        public string Id { get; set; }

        /// inheritedDoc
        public Type EntityType { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="specificationsType">Specification type</param>
        /// <param name="entityType">Entity type</param>
        public SpecificationsContext(Type specificationsType, Type entityType)
        {
            Id = Guid.NewGuid().ToString();
            SpecificationsType = specificationsType;
            EntityType = entityType;
        }

        /// inheritedDoc
        public event EventHandler Disposed;

        /// <summary>
        /// Called to trigger <see cref="Disposed"/> event.
        /// </summary>
        protected virtual void OnDisposed()
        {
            Disposed.InvokeSafely(this);
        }

        /// <summary>
        /// Gets a value indicates that this unit of work is disposed or not.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// inheritedDoc
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            OnDisposed();
        }
    }
}
