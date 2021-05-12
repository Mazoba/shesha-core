using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Domain._Services
{
    class OrganisationPostService
    {
        /* NOTE: the code moved from the `OrganisationRole` entity, to be implemented as part of the service/helper/extension
        /// <summary>
        /// Returns the persons currently appointed to the role (older appointments first)
        /// </summary>
        public virtual IList<OrganisationRoleAppointment> ActiveAppointments
        {
            get
            {
                return Appointments.Where(app => app.IsActive)
                    // First show permanent appointments, then contract, and finally acting
                    .OrderByDescending(a => a.AppointmentStatus)
                    // Then sort by start date
                    .ThenBy(a => a.AppointmentStartDate.HasValue ? a.AppointmentStartDate : DateTime.MaxValue)
                    // Ensure that appointments are not mixed from call to call
                    .ThenBy(a => a.Id)
                    .ToList();
            }
        }

        public virtual IList<OrganisationRoleAppointment> GetActiveAppointments(DateTime date)
        {
            return Appointments.Where(app => app.GetIsActive(date))
                // First show permanent appointments, then contract, and finally acting
                .OrderByDescending(a => a.AppointmentStatus)
                // Then sort by start date
                .ThenBy(a => a.AppointmentStartDate.HasValue ? a.AppointmentStartDate : DateTime.MaxValue)
                // Ensure that appointments are not mixed from call to call
                .ThenBy(a => a.Id)
                .ToList();
        }

        /// <summary>
        /// Active appointment for today
        /// </summary>
        public virtual Employee ActiveAppointment
        {
            get
            {
                return GetActiveAppointment(DateTime.Now);
            }
        }

        /// <summary>
        /// Active appointed person for the given date
        /// </summary>
        public virtual Employee GetActiveAppointment(DateTime date)
        {
            var appointment = GetActiveAppointments(date).FirstOrDefault();
            return (appointment ?? new OrganisationRoleAppointment()).Employee;
        }

        /// <summary>
        /// Name of the post for the given appointed person
        /// </summary>
        public virtual string NameForAppointment(Person person)
        {
            return person is Employee employee
                ? NameForAppointment(employee, DateTime.Now)
                : null;
        }

        /// <summary>
        /// Name of the role for the specified appointed person and date
        /// </summary>
        public virtual string NameForAppointment(Employee person, DateTime date)
        {
            var appointmentService = DependencyResolver.Current.GetService<IRoleAppointmentService>();
            return appointmentService.GetAppointmentName(person, person, this, PersonNameFormat.None, PostNameFormat.Full, date);
        }

        /// <summary>
        /// Full name with all supervisors
        /// </summary>
        public virtual string FullName
        {
            get
            {
                return this.GetFullChain(r => r.SupervisorRole).ToList().Select(r => r.NullSafe().Name.Trim()).Delimited("\\");
            }
        }

        public override ICollection<ValidationResult> ValidationResults()
        {
            var results = base.ValidationResults();

            if (this.SupervisorRole != null && this.HasLoops(r => r.SupervisorRole))
            {
                results.Add(new ValidationResult(string.Format("The role '{0}' can't be used as supervisor for the role '{1}' because it causes circular references.", this.SupervisorRole.Name, this.Name), new List<string>() { "Supervisor Role" }));
            }

            return results;
        }

        /// <summary>
        /// Name with permanent role (if any)
        /// </summary>
        public virtual string FullNameWithActiveAppointment
        {
            get
            {
                var appointment = ActiveAppointment;
                if (appointment == null)
                    return Name;
                return NameForAppointment(appointment) + ": " + appointment.FullName;
            }
        }


         */
    }
}
