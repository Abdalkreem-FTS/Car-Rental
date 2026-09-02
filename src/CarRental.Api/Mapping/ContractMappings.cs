using CarRental.Api.Contracts.Auth;
using CarRental.Api.Contracts.Cars;
using CarRental.Api.Contracts.Profile;
using CarRental.Api.Contracts.Reservations;
using CarRental.Application.Dtos.Auth;
using CarRental.Application.Dtos.Cars;
using CarRental.Application.Dtos.Profile;
using CarRental.Application.Dtos.Reservations;
using Riok.Mapperly.Abstractions;

namespace CarRental.Api.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ContractMappings
{
    public static partial RegisterDto Map(RegisterRequest request);

    public static partial LoginDto Map(LoginRequest request);

    public static partial RefreshTokenDto Map(RefreshTokenRequest request);

    public static partial ConfirmEmailDto Map(ConfirmEmailRequest request);

    public static partial ResendConfirmationDto Map(ResendConfirmationRequest request);

    public static partial ForgotPasswordDto Map(ForgotPasswordRequest request);

    public static partial ResetPasswordDto Map(ResetPasswordRequest request);

    public static partial CarSearchDto Map(CarSearchRequest request);

    public static partial CarQueryDto Map(CarQueryRequest request);

    public static partial CreateCarDto Map(CreateCarRequest request);

    public static partial UpdateCarDto Map(UpdateCarRequest request);

    public static partial RetireCarDto Map(RetireCarRequest request);

    public static partial UpdateProfileDto Map(UpdateProfileRequest request);

    public static partial ChangePasswordDto Map(ChangePasswordRequest request);

    public static partial CreateReservationDto Map(CreateReservationRequest request);

    public static partial UpdateReservationDto Map(UpdateReservationRequest request);

    public static partial ReservationQueryDto Map(ReservationQuery query);

    public static partial AuthResponse ToResponse(this AuthDto dto);

    public static partial UserResponse ToResponse(this UserDto dto);

    public static partial CarResponse ToResponse(this CarDto dto);

    public static partial RetireCarResponse ToResponse(this RetireCarResultDto dto);

    public static partial ProfileResponse ToResponse(this ProfileDto dto);

    public static partial ReservationResponse ToResponse(this ReservationDto dto);
}
