using MovieApi.Application.DTOs.Auth;

namespace MovieApi.Application.Interfaces;

public interface ICurrentUserService
{
    CurrentUserInfo GetCurrentUser();
}