using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.Employee")]
    public class Employee : Person
    {
        public Employee()
        {
            SecurityClearance = RefListSecurityClassification.Public;
            HasNoComputer = false;
            SignatureType = RefListSignatureType.None;
        }

        [DisplayFormat(DataFormatString = "Yes|No")]
        [Display(Name = "Details have been validated")]
        public virtual bool DetailsValidated { get; set; }

        [StringLength(20)]
        [Display(Name = "Employee No")]
        public virtual string EmployeeNo { get; set; }

        /// <summary>
        /// Security clearance
        /// </summary>
        [Display(Name = "Security Clearance")]
        public virtual RefListSecurityClassification SecurityClearance { get; set; }

        /// <summary>
        /// Security clearance end date (after this date it's reset to Public)
        /// </summary>
        [Display(Name = "Security Clearance valid till")]
        public virtual DateTime? SecurityClearanceEndDate { get; set; }

        /// <summary>
        /// Actual security clearance
        /// </summary>
        [NotMapped]
        public virtual RefListSecurityClassification ActualSecurityClearance =>
            SecurityClearanceEndDate != null && SecurityClearanceEndDate.Value.Date > DateTime.Now.Date
                ? SecurityClearance
                : RefListSecurityClassification.Public;

        [Display(Name = "Office Location")]
        [AllowInherited]
        public virtual Location OfficeLocation { get; set; }

        [StringLength(20)]
        [Display(Name = "Office Room No")]
        public virtual string OfficeRoomNo { get; set; }

        [NotMapped]
        public virtual string SurnameAndInitials => LastName + (string.IsNullOrEmpty(Initials) ? (string.IsNullOrEmpty(FirstName) ? "" : string.Format(" {0}", FirstName[0])) : Initials);

        [NotMapped]
        public virtual string InitialAndSurname => (string.IsNullOrEmpty(Initials) ? (string.IsNullOrEmpty(FirstName) || FirstName.Length == 0 ? "" : string.Format("{0} ", FirstName[0])) : Initials) + LastName;

        /// <summary>
        /// If true, indicates that this employee has no computer and doesn't work with the system directly. His account is used by another users to act on behalf of he
        /// </summary>
        [Display(Name = "Has no computer")]
        public virtual bool HasNoComputer { get; set; }

        [Display(Name = "Shift Worker")]
        public virtual bool IsShiftWorker { get; set; }

        public virtual Address PostalAddress { get; set; }
        public virtual Address ResidentialAddress { get; set; }

        public virtual RefListSignatureType SignatureType { get; set; }

        [Display(Name = "Salary Level")]
        public virtual int? SalaryLevel { get; set; }

        [Display(Name = "Notch")]
        public virtual int? Notch { get; set; }

        [Display(Name = "Occupational Classification")]
        public virtual string OccupationalClassification { get; set; }

        [Display(Name = "Component")]
        public virtual string Component { get; set; }
    }
}
