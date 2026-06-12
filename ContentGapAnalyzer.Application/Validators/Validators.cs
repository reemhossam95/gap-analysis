using ContentGapAnalyzer.Application.Commands;
using ContentGapAnalyzer.Application.Queries;
using FluentValidation;

namespace ContentGapAnalyzer.Application.Validators;

public class FetchTrendingVideosCommandValidator : AbstractValidator<FetchTrendingVideosCommand>
{
    public FetchTrendingVideosCommandValidator()
    {
        RuleFor(x => x.Region)
            .NotEmpty().WithMessage("Region is required.")
            .MaximumLength(10).WithMessage("Region must not exceed 10 characters.");

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 50).WithMessage("MaxResults must be between 1 and 50.");

        RuleFor(x => x.CategoryId)
            .MaximumLength(10).WithMessage("CategoryId must not exceed 10 characters.")
            .When(x => !string.IsNullOrEmpty(x.CategoryId));

        RuleFor(x => x.Keywords)
            .MaximumLength(200).WithMessage("Keywords must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Keywords));
    }
}

public class AnalyzeGapCommandValidator : AbstractValidator<AnalyzeGapCommand>
{
    public AnalyzeGapCommandValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.")
            .MaximumLength(32).WithMessage("VideoId must not exceed 32 characters.")
            .Matches(@"^[a-zA-Z0-9_\-]+$").WithMessage("VideoId contains invalid characters.");
    }
}

public class SaveAnalysisSessionCommandValidator : AbstractValidator<SaveAnalysisSessionCommand>
{
    public SaveAnalysisSessionCommandValidator()
    {
        RuleFor(x => x.GapReportId)
            .GreaterThan(0).WithMessage("GapReportId must be a positive integer.");

        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.")
            .MaximumLength(32).WithMessage("VideoId must not exceed 32 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.ContextJson)
            .MaximumLength(50000).WithMessage("ContextJson payload is too large.")
            .When(x => !string.IsNullOrEmpty(x.ContextJson));
    }
}

public class GetAnalysisHistoryQueryValidator : AbstractValidator<GetAnalysisHistoryQuery>
{
    public GetAnalysisHistoryQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}

public class GetGapReportByVideoIdQueryValidator : AbstractValidator<GetGapReportByVideoIdQuery>
{
    public GetGapReportByVideoIdQueryValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.")
            .MaximumLength(32).WithMessage("VideoId must not exceed 32 characters.");
    }
}
