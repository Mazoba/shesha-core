using System;
using Abp.Application.Services;
using Shesha.Notes.Dto;
using Shesha.Roles.Dto;

namespace Shesha.Notes
{
    public interface INoteAppService : IAsyncCrudAppService<NoteDto, Guid, PagedRoleResultRequestDto, CreateNoteDto, UpdateNoteDto>
    {
    }
}
