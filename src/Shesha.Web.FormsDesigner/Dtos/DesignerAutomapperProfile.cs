using Shesha.AutoMapper;
using Shesha.Web.FormsDesigner.Domain;

namespace Shesha.Web.FormsDesigner.Dtos
{
    /// <summary>
    /// Form Designer Automapper profile
    /// </summary>
    public class DesignerAutomapperProfile: ShaProfile
    {
        public DesignerAutomapperProfile()
        {
            CreateMap<Form, FormDto>()
                .ForMember(e => e.Markup, m => m.MapFrom(e => e.Settings));

            CreateMap<FormUpdateMarkupInput, Form>()
                .ForMember(e => e.Settings, m => m.MapFrom(e => e.Markup));

            CreateMap<ConfigurableComponent, ConfigurableComponentDto>();

            CreateMap<ConfigurableComponentDto, ConfigurableComponent>();
            CreateMap<ConfigurableComponentUpdateSettingsInput, ConfigurableComponent>();
        }
    }
}
