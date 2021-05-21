using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Domain._Services
{
    class OrganisationPostAppointmentService
    {
        /* NOTE: the code moved from the `OrganisationRoleAppointment` entity, to be implemented as part of the service/helper/extension
        [EntityDisplayName]
        public virtual string FullName => Employee != null ? Employee.GetDisplayName() + " - " + OrganisationRole.NameForAppointment(Employee) : null;

        public virtual string FullNameRoleFirst => Employee != null ? OrganisationRole.NameForAppointment(Employee) + ": " + Employee.GetDisplayName() : null;
        public virtual bool IsActive
        {
            get
            {
                return OrganisationRole != null && Employee != null &&
                    !OrganisationRole.InactiveFlag && !Employee.InactiveFlag &&
                    (!AppointmentStartDate.HasValue || AppointmentStartDate.Value <= SheshaTime.Now) &&
                    (!AppointmentEndDate.HasValue || AppointmentEndDate.Value > SheshaTime.Now);
            }
        }

        public virtual bool GetIsActive(DateTime date)
        {
            return OrganisationRole != null && Employee != null &&
                !OrganisationRole.InactiveFlag && !Employee.InactiveFlag &&
                (!InactivatedTimestamp.HasValue || InactivatedTimestamp.Value.Date >= date.Date) &&
                (!AppointmentStartDate.HasValue || AppointmentStartDate.Value.Date <= date.Date) &&
                (!AppointmentEndDate.HasValue || AppointmentEndDate.Value.Date > date.Date);
        }

        public virtual IList<OrganisationRoleAppointmentDelegation> ActiveDelegations()
        {
            return Delegation.Where(t => t.IsActive && (t.DelegatedToPerson.Id != this.Employee.Id)).ToList();
        }
         */
    }
}
