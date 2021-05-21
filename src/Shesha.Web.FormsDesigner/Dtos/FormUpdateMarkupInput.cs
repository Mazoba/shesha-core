using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Shesha.Web.FormsDesigner.Domain;

namespace Shesha.Web.FormsDesigner.Dtos
{
    /// <summary>
    /// Form DTO
    /// </summary>
    [AutoMap(typeof(Form))]
    public class FormUpdateMarkupInput : EntityDto<Guid>
    {
        /// <summary>
        /// Form markup (components) in JSON format
        /// </summary>
        public string Markup { get; set; }
    }
}
