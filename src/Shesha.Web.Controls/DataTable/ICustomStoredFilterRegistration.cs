using System;
using System.Collections.Generic;
using Abp.Domain.Entities;
using Shesha.Domain;
using Shesha.Web.DataTable;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// This class is for adding custom filter callback or custom visibility to an existing stored filter. The filter have to be registered with the same ID in a DB migration
    /// </summary>
    public interface ICustomStoredFilterRegistration
    {
        /// <summary>
        /// Filter ID. Must be unique and must match filter registration ID. The filter still can be hidden from the list by admin or users
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Custom filtering definition (for cases when HQL doesn't work). Must return true or false for the entity on input.
        /// </summary>
        public Func<bool, TRowClass, Microsoft.AspNetCore.Mvc.ControllerContext> CustomRowFilterFunc<TRowClass>() where TRowClass : class;

        /// <summary>
        /// When provided, the filter is only visible for a user if this method returns true
        /// </summary>
        public Func<bool, Microsoft.AspNetCore.Mvc.ControllerContext> CustomVisibilityFunc { get; }
    }
}
