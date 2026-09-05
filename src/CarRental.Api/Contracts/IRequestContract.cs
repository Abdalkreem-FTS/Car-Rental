namespace CarRental.Api.Contracts;

public interface IRequestContract<out TDto>
{
    TDto ToDto();
}
