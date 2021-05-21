using System;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;

namespace Shesha.Notes.Dto
{
    public class NoteProfile : ShaProfile
    {
        public NoteProfile()
        {
            CreateMap<CreateNoteDto, Note>();

            CreateMap<Note, NoteDto>()
                .ForMember(u => u.Author, options => options.MapFrom(e => e.Author != null ? new EntityWithDisplayNameDto<Guid> { Id = e.Author.Id, DisplayText = e.Author.FullName } : null));
            CreateMap<NoteDto, Note>();

            CreateMap<UpdateNoteDto, Note>();
        }
    }
}
