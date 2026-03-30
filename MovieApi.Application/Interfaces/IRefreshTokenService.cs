namespace MovieApi.Application.Interfaces;

public interface IRefreshTokenService
{
    string GenerateToken();

    string HashToken(string token);
}