namespace Shesha.Web.FormsDesigner.Dtos
{
    public class FormDtoOld
    {
        /// <summary>
        /// Identifier of the form. Note: file path may be used as an identifier
        /// </summary>
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ModelType { get; set; }
    }
}
