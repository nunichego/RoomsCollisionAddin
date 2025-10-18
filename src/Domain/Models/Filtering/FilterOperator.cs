namespace RoomsManagerAddin.Domain.Models.Filtering
{
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        BeginsWith,
        EndsWith,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        HasValue,
        HasNoValue
    }
}
