using CarRental.Api.Contracts.Common;
using CarRental.Application.Dtos.Common;

namespace CarRental.Api.Mapping;

public static class PagedMapping
{
    public static PagedResponse<TResponse> ToResponse<TDto, TResponse>(
        this PagedResult<TDto> page,
        Func<TDto, TResponse> toResponse) =>
        new([.. page.Items.Select(toResponse)], page.Page, page.PageSize, page.TotalCount);
}
