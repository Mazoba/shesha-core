using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Domain._Services
{
    class EmployeeService
    {
        /*
        /// <summary>
        /// Active appointments list (permanent first)
        /// </summary>
        public virtual IList<OrganisationRoleAppointment> ActiveAppointments
        {
            get
            {
                return
                    Appointments.Where(ap => ap.IsActive && !ap.InactiveFlag)
                        .OrderBy(ap => ap.OrganisationRole.OrganisationRoleLevel == null ? int.MaxValue : (ap.OrganisationRole.OrganisationRoleLevel.RankLevel ?? int.MaxValue))
                        .ThenBy(a => a.AppointmentStatus == RefListAppointmentStatus.Permanent
                            ? 2
                            : a.AppointmentStatus == RefListAppointmentStatus.Contract
                                ? 3
                                : 1)
                        // Then sort by start date
                        .ThenBy(a => a.AppointmentStartDate.HasValue ? a.AppointmentStartDate.Value : DateTime.MaxValue)
                        // Ensure that appointments are not mixed from call to call
                        .ThenBy(a => a.Id)
                        .ToList();
            }
        }

         public virtual OrganisationRole PermanentPost()
        {
            return ActiveAppointments.FirstOrDefault(a => a.AppointmentStatus == RefListAppointmentStatus.Permanent).NullSafe().OrganisationRole;
        }

        public virtual OrganisationRole ActingPost()
        {
            return ActiveAppointments.FirstOrDefault(a => a.AppointmentStatus == RefListAppointmentStatus.Acting).NullSafe().OrganisationRole;
        }

         /// <summary>
        /// Active role (earliest appointment, permanent first)
        /// </summary>
        [Display(Name = "Primary post")]
        public virtual OrganisationRole ActiveRole => (ActiveAppointment ?? new OrganisationRoleAppointment()).OrganisationRole;

        /// <summary>
        /// Active role for the given date
        /// </summary>
        public virtual OrganisationRole GetActiveRole(DateTime date)
        {
            return (GetActiveAppointment(date) ?? new OrganisationRoleAppointment()).OrganisationRole;
        }

        /// <summary>
        /// Name with permanent role (if any)
        /// </summary>
        public virtual string FullNameWithActiveRole
        {
            get
            {
                var role = ActiveRole;
                // Person's display name
                return (string.IsNullOrEmpty(FirstName) ? "" : (FirstName + " ")) +
                       LastName + (role == null ? "" : (" - " + role.NameForAppointment(this)));
            }
        }

        /// <summary>
        /// Name with specified role
        /// </summary>
        public virtual string FullNameWithRole(OrganisationRole role)
        {
            if (role == null)
                return FullName;
            var appointment = Appointments.Where(a => a.OrganisationRole == role).FirstOrDefault();
            return appointment == null
                ? FullName
                : (FullName + " - " + role.NameForAppointment(this));
        }

        /// <summary>
        /// Active appointment (earliest, permanent first)
        /// </summary>
        public virtual OrganisationRoleAppointment ActiveAppointment
        {
            get
            {
                return
                    Appointments.Where(ap => ap.IsActive)
                        .OrderBy(ap => ap.OrganisationRole?.OrganisationRoleLevel?.RankLevel ?? int.MaxValue)
                        .ThenBy(a => a.AppointmentStatus == RefListAppointmentStatus.Permanent
                            ? 2
                            : a.AppointmentStatus == RefListAppointmentStatus.Contract
                                ? 3
                                : 1)
                        // Then sort by start date
                        .ThenBy(a => a.AppointmentStartDate ?? DateTime.MaxValue)
                        // Ensure that appointments are not mixed from call to call
                        .ThenBy(a => a.Id)
                        .FirstOrDefault();
            }
        }

        /// <summary>
        /// Default appointment of the user (not a highest rank appointment). Is used for authorization.
        /// </summary>
        public virtual OrganisationRoleAppointment DefaultAppointment
        {
            get
            {
                return Appointments.Where(ap => ap.IsActive)
                    // First show permanent appointments, then contract, and finally acting
                    .OrderBy(a => a.AppointmentStatus == RefListAppointmentStatus.Permanent
                        ? 1
                        : a.AppointmentStatus == RefListAppointmentStatus.Contract
                            ? 2
                            : 3)
                    // Then sort by start date
                    .ThenBy(a => a.AppointmentStartDate.HasValue ? a.AppointmentStartDate.Value : DateTime.MaxValue)
                    // Ensure that appointments are not mixed from call to call
                    .ThenBy(a => a.Id)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Active appointment (earliest, permanent first) for the given date
        /// </summary>
        public virtual OrganisationRoleAppointment GetActiveAppointment(DateTime date)
        {
            return
                Appointments.Where(ap => ap.GetIsActive(date))
                    // First show permanent appointments, then contract, and finally acting
                    .OrderBy(ap => ap.OrganisationRole.OrganisationRoleLevel == null ? int.MaxValue : (ap.OrganisationRole.OrganisationRoleLevel.RankLevel ?? int.MaxValue))
                    .ThenBy(a => a.AppointmentStatus == RefListAppointmentStatus.Permanent
                        ? 2
                        : a.AppointmentStatus == RefListAppointmentStatus.Contract
                            ? 3
                            : 1)
                    // Then sort by start date
                    .ThenBy(a => a.AppointmentStartDate.HasValue ? a.AppointmentStartDate.Value : DateTime.MaxValue)
                    // Ensure that appointments are not mixed from call to call
                    .ThenBy(a => a.Id)
                    .FirstOrDefault();
        }

           */
    }
}
