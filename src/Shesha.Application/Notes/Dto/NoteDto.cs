﻿using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Shesha.AutoMapper.Dto;

namespace Shesha.Notes.Dto
{
    public class NoteDto: EntityDto<Guid>
    {
        /// <summary>
        /// Id of the owner entity
        /// </summary>
        [Required]
        public string OwnerId { get; set; }

        /// <summary>
        /// Type short alias of the owner entity
        /// </summary>
        [Required]
        public string OwnerType { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// Category of the note. Is used to split notes into groups
        /// </summary>
        public int? Category { get; set; }

        /// <summary>
        /// Note importance (priority)
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// Id of the parent note
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        [Required]
        public string NoteText { get; set; }

        public EntityWithDisplayNameDto<Guid> Author { get; set; }
    }
}
