using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Filtering
{
    /// <summary>
    /// Information about a Revit category for selection and filtering
    /// </summary>
    public class CategoryInfo
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public CategoryType CategoryType { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
