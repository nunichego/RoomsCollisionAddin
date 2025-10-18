// Backward compatibility - re-export all model classes under old namespace
// This allows existing code to continue using RoomsManagerAddin.Models namespace

namespace RoomsManagerAddin.Models
{
    // Filtering Models - Type aliases
    using ParameterDataType = Domain.Models.Filtering.ParameterDataType;
    using LogicalOperator = Domain.Models.Filtering.LogicalOperator;
    using FilterOperator = Domain.Models.Filtering.FilterOperator;
    using ParameterInfo = Domain.Models.Filtering.ParameterInfo;
    using IFilterItem = Domain.Models.Filtering.IFilterItem;
    using RoomFilterRule = Domain.Models.Filtering.RoomFilterRule;
    using FilterSet = Domain.Models.Filtering.FilterSet;
    using RoomFilterConfiguration = Domain.Models.Filtering.RoomFilterConfiguration;
    using CategoryInfo = Domain.Models.Filtering.CategoryInfo;

    // Analysis Models - Type aliases
    using RoomAnalysisResult = Domain.Models.Analysis.RoomAnalysisResult;
    using RoomPreviewResult = Domain.Models.Analysis.RoomPreviewResult;
    using CurveGroups = Domain.Models.Analysis.CurveGroups;
    using RoomCollisionResult = Domain.Models.Analysis.RoomCollisionResult;

    // Shared Models - Type aliases
    using RoomItem = Domain.Models.Shared.RoomItem;
    using WallItem = Domain.Models.Shared.WallItem;
    using FloorItem = Domain.Models.Shared.FloorItem;
    using ElementItem = Domain.Models.Shared.ElementItem;
    using InitialDataResult = Domain.Models.Shared.InitialDataResult;
    using ProgressInfo = Domain.Models.Shared.ProgressInfo;

    // Configuration Models - Type aliases
    using AppSettings = Domain.Models.Configuration.AppSettings;
}
