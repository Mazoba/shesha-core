﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Auditing;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Table("Core_Organisations")]
    [Discriminator]
    public class OrganisationBase : FullAuditedEntityWithExternalSync<Guid>, IMayHaveTenant
    {
        [EntityDisplayName]
        [StringLength(255, MinimumLength = 2)]
        [Required(AllowEmptyStrings = false)]
        public virtual string Name { get; set; }

        [StringLength(50)]
        public virtual string ShortAlias { get; set; }

        [StringLength(300), DataType(DataType.MultilineText)]
        public virtual string Description { get; set; }

        [StringLength(1000), DataType(DataType.MultilineText)]
        [Display(Name = "Address (free text)")]
        public virtual string FreeTextAddress { get; set; }

        [ReferenceList("Shesha.Core", "OrganisationUnitType")]
        public virtual int? OrganisationType { get; set; }
        public virtual int? TenantId { get; set; }

        [StringLength(30)]
        public virtual string CompanyRegistrationNo { get; set; }

        [StringLength(30)]
        public virtual string VatRegistrationNo { get; set; }

        [StringLength(200)]
        public virtual string ContactEmail { get; set; }
        [StringLength(50)]
        public virtual string ContactMobileNo { get; set; }
    }


    public class OrganisationBase<T, TAddress, TPerson> : OrganisationBase where T : OrganisationBase<T, TAddress, TPerson> where TPerson : Person
    {
        /// <summary>
        /// Parent organisation
        /// </summary>
        public virtual T Parent { get; set; }

        /// <summary>
        /// Primary Address
        /// </summary>
        [Audited]
        public virtual TAddress PrimaryAddress { get; set; }

        /// <summary>
        /// Primary contact
        /// </summary>
        [Audited]
        public virtual TPerson PrimaryContact { get; set; }

    }

    public class OrganisationBase<T> : OrganisationBase<T, Address, Person>
        where T : OrganisationBase<T>
    {
    }
}
