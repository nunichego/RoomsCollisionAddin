using Autodesk.Revit.DB;

namespace RoomsManagerAddin.Domain.Models.Filtering
{
    public interface IFilterItem
    {
        bool Evaluate(Element element);
        string GetDescription();
    }
}
