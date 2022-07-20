﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Auditing;
using Abp.Localization;
using Abp.Timing;
using JetBrains.Annotations;
using Shesha.Authorization.Users;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;
using Shesha.EntityHistory;
using Shesha.Extensions;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.Person")]
    [Table("Core_Persons") /* pluralize() returns wrong version */]
    [Discriminator]
    [DisplayManyToManyAuditTrail(typeof(ShaRoleAppointedPerson), "Role", DisplayName = "Role Appointment")]
    public class Person : FullPowerEntity
    {
        [Display(Name = "Organisation")]
        public virtual Organisation PrimaryOrganisation { get; set; }

        [StoredFile(IsVersionControlled = true)]
        public virtual StoredFile Photo { get; set; }

        [StringLength(13)]
        [Display(Name = "Identity Number")]
        public virtual string IdentityNumber { get; set; }

        public virtual RefListPersonTitle? Title { get; set; }

        [StringLength(50)]
        [Display(Name = "First Name")]
        [Audited]
        public virtual string FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last Name")]
        [Audited]
        public virtual string LastName { get; set; }

        /// <summary>
        /// Initials override. If empty, the first letter of FirstName is taken.
        /// </summary>
        [StringLength(10), Display(Name = "Initials")]
        public virtual string Initials { get; set; }

        /// <summary>
        /// Custom short name (overrides calculated short name)
        /// </summary>
        [StringLength(60)]
        [Display(Name = "Custom Short Name")]
        public virtual string CustomShortName { get; set; }

        [StringLength(20)]
        public virtual string HomeNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Mobile Number")]
        [Audited]
        public virtual string MobileNumber1 { get; set; }

        [StringLength(20)]
        [Display(Name = "Alternate Mobile Number")]
        public virtual string MobileNumber2 { get; set; }

        [StringLength(100), EmailAddress]
        [Display(Name = "Email Address")]
        [Audited]
        public virtual string EmailAddress1 { get; set; }

        [StringLength(100), EmailAddress]
        [Display(Name = "Alternative Email Address")]
        public virtual string EmailAddress2 { get; set; }

        [Past]
        [Audited]
        [DisableDateTimeNormalization]
        [DataType(DataType.Date)]
        public virtual DateTime? DateOfBirth { get; set; }

        [Audited]
        public virtual RefListGender? Gender { get; set; }

        [ReferenceList("Shesha.Core", "PreferredContactMethod")]
        public virtual int? PreferredContactMethod { get; set; }

        /// <summary>
        /// Calcuated property in the following format: FirstName + ' ' + LastName
        /// </summary>
        [EntityDisplayName]
        [ReadonlyProperty]
        public virtual string FullName { get; protected set; }

        [Display(Name = "Type of account")]
        public virtual RefListTypeOfAccount? TypeOfAccount { get; set; }

        [Display(Name = "Email Address Confirmed")]
        public virtual bool EmailAddressConfirmed { get; set; }

        [Display(Name = "Mobile Number Confirmed")]
        public virtual bool MobileNumberConfirmed { get; set; }

        /// <summary>
        /// User record, may be null for non registered users
        /// </summary>
        [CanBeNull]
        public virtual User User { get; set; }

        public override string ToString()
        {
            return FullName;
        }

        public virtual bool IsMobileVerified { get; set; }

        [ManyToMany("Core_Persons_Languages", "LanguageId", "PersonId")]
        public virtual IList<ApplicationLanguage> PreferredLanguages { get; set; } = new List<ApplicationLanguage>();
    }
}
