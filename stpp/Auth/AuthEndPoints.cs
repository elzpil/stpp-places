using Microsoft.AspNetCore.Identity;
using stpp.Auth.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace stpp.Auth
{
    public static class AuthEndPoints
    {
        public static void AddAuthApi(this WebApplication app)
        {
            //register
            app.MapPost("api/register", async (UserManager<ForumRestUser> userManager, RegisterUserDto registerUserDto) =>
            {
                // user exists
                var user = await userManager.FindByNameAsync(registerUserDto.Username);
                if (user != null)
                {
                    return Results.UnprocessableEntity("user name already taken");
                }
                var newUser = new ForumRestUser
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.Username
                };
                var createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);

                if (!createUserResult.Succeeded)
                {
                    var errorMessages = string.Join(", ", createUserResult.Errors.Select(error => error.Description));
                    return Results.UnprocessableEntity($"Failed to register: {errorMessages}");
                }

                await userManager.AddToRoleAsync(newUser, ForumRoles.ForumUser);

                return Results.Created("api/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
            });

            //login
            app.MapPost("api/login", async (UserManager<ForumRestUser> userManager,JwtTokenService jwtTokenService, LoginUserDto loginUserDto) =>
            {
                // user exists
                var user = await userManager.FindByNameAsync(loginUserDto.Username);
                if (user == null)
                {
                    return Results.UnprocessableEntity("username or password was incorrect");

                }
              
                var isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);
                if (!isPasswordValid)
                    return Results.UnprocessableEntity("username or password was incorrect");

                user.ForceRelogin = false;
                await userManager.UpdateAsync(user);

                var roles = await userManager.GetRolesAsync(user);
                //tokens
                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
            });

            //access token
            app.MapPost("api/accessToken", async (UserManager<ForumRestUser> userManager, JwtTokenService jwtTokenService, RefreshAccessTokenDto refreshAccessTokenDto) =>
            {
                if(!jwtTokenService.TryParseREfreshToken(refreshAccessTokenDto.RefreshToken, out var claims))
                {
                    return Results.UnprocessableEntity();
                }
                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var user = await userManager.FindByIdAsync(userId);
                if(user == null)
                {
                    return Results.UnprocessableEntity("Invalid token");
                }
                if(user.ForceRelogin)
                {
                    return Results.UnprocessableEntity();
                }

                var roles = await userManager.GetRolesAsync(user);
                //tokens
                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
            });
            // TO DO logout

        }
    }
    public record SuccessfulLoginDto (string AccessToken, string RefreshToken);
    public record UserDto(string UserId, string UserName, string Email);
    public record RegisterUserDto (string Username, string Email, string Password);
    public record LoginUserDto(string Username, string Password);
    public record RefreshAccessTokenDto(string RefreshToken);
}
