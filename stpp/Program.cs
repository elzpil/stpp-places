using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;
using stpp.Data;
using stpp.Data.Entities;
using System.Diagnostics.Metrics;
using static stpp.Data.Entities.City;
using static stpp.Data.Entities.Country;
using static stpp.Data.Entities.Place;
using Microsoft.AspNetCore.Identity;
using stpp.Auth.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using stpp.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        builder => builder.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .WithHeaders("content-type")
                          .WithExposedHeaders("content-type"));
});

builder.Services.AddDbContext<ForumDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient<JwtTokenService>();
builder.Services.AddScoped<AuthDbSeeder>();

builder.Services.AddIdentity<ForumRestUser, IdentityRole>()
    .AddEntityFrameworkStores<ForumDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{ 
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]));
});
builder.Services.AddAuthorization();





var app = builder.Build();
app.UseCors("AllowLocalhost3000");

#region Endpoints
//country,city,place
var countriesGroup = app.MapGroup("/api").WithValidationFilter();


countriesGroup.MapGet("countries", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    return (await dbContext.Countries.ToListAsync(cancellationToken)).Select( o => new CountryDto(o.Id,o.Name, o.Description));
});

countriesGroup.MapGet("countries/{countryId}", async (int countryId, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    return Results.Ok(new CountryDto(country.Id, country.Name, country.Description));
});

countriesGroup.MapPost("countries", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async ([Validate] CreateCountryDto createCountryDto, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var country = new Country()
    {
        Name = createCountryDto.Name,
        Description = createCountryDto.Description,
        UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
    };
    dbContext.Countries.Add(country);
    await dbContext.SaveChangesAsync();
    return Results.Created("api/countries/{country.Id}", new CountryDto(country.Id, country.Name, country.Description));
});

countriesGroup.MapPut("countries/{countryId}", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async (int countryId, [Validate] UpdateCountryDto dto, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    if(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != country.UserId && !httpContext.User.IsInRole(ForumRoles.Admin))
    {
        return Results.Forbid();
    }

    country.Description = dto.Description;
    dbContext.Update(country);
    await dbContext.SaveChangesAsync();
    return Results.Ok(new CountryDto(country.Id, country.Name, country.Description));
});


countriesGroup.MapDelete("countries/{countryId}", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async (int countryId, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    if (!httpContext.User.IsInRole(ForumRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != country.UserId)
    {
        return Results.Forbid();
    }

    dbContext.Remove(country); 
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});


var citiesGroup = app.MapGroup("/api/countries/{countryId}").WithValidationFilter();

citiesGroup.MapGet("cities", async (int countryId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var cities = await dbContext.Cities
        .Include(c => c.Country)
        .Where(c => c.Country.Id == countryId)  // Filter cities by countryId
        .ToListAsync(cancellationToken);

    var cityDtos = cities.Select(city =>
    {
        var countryDto = city.Country != null
            ? new CountryDto(city.Country.Id, city.Country.Name, city.Country.Description)
            : null; // Handle the case where city.Country is null

        return new CityDto(city.Id, city.Name, city.Description, countryDto);
    }).ToList();

    return Results.Ok(cityDtos);
});



citiesGroup.MapGet("cities/{cityId}", async (int countryId, int cityId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();

    return Results.Ok(new CityDto(city.Id, city.Name, city.Description, new CountryDto(city.Country.Id, city.Country.Name, city.Country.Description)));
});



citiesGroup.MapPost("cities", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async (int countryId, [Validate] CreateCityDto createCityDto, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var existingCountry = await dbContext.Countries.FindAsync(countryId);

    if (existingCountry == null)
    {
        return Results.NotFound();
    }

    var city = new City
    {
        Name = createCityDto.Name,
        Description = createCityDto.Description,
        Country = existingCountry,
        UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
    };

    dbContext.Cities.Add(city);
    await dbContext.SaveChangesAsync();

    return Results.Created($"api/countries/{existingCountry.Id}/cities/{city.Id}", new CityDto(city.Id, city.Name, city.Description, new CountryDto(existingCountry.Id, existingCountry.Name, existingCountry.Description)));
});

citiesGroup.MapPut("cities/{cityId}", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async (int countryId, int cityId, [Validate] UpdateCityDto dto, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();

    if (!httpContext.User.IsInRole(ForumRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != city.UserId)
    {
        return Results.Forbid();
    }

    city.Description = dto.Description;
    dbContext.Update(city);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new CityDto(city.Id, city.Name, city.Description, new CountryDto(country.Id, country.Name, country.Description)));


});

citiesGroup.MapDelete("cities/{cityId}", [Authorize(Roles = ForumRoles.ForumUser + "," + ForumRoles.Admin)] async (int countryId, int cityId, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();

    if (!httpContext.User.IsInRole(ForumRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != city.UserId)
    {
        return Results.Forbid();
    }

    dbContext.Remove(city);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});



var placesGroup = app.MapGroup("/api/countries/{countryId}/cities/{cityId}").WithValidationFilter();

placesGroup.MapGet("places", async (int countryId, int cityId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var places = await dbContext.Places
        .Include(p => p.City)
            .ThenInclude(c => c.Country)
        .Where(p => p.City.Country.Id == countryId && p.City.Id == cityId)  // Filter places by countryId and cityId
        .ToListAsync(cancellationToken);

    var placeDtos = places.Select(place =>
    {
        CityDto cityDto = null;
        if (place.City != null)
        {
            var countryDto = place.City.Country != null
                ? new CountryDto(place.City.Country.Id, place.City.Country.Name, place.City.Country.Description)
                : null;

            cityDto = new CityDto(place.City.Id, place.City.Name, place.City.Description, countryDto);
        }

        return new PlaceDto(place.Id, place.Name, place.Description, cityDto);
    }).ToList();

    return Results.Ok(placeDtos);
});



placesGroup.MapGet("places/{placeId}", async (int countryId, int cityId, int placeId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();

    var place = await dbContext.Places.FirstOrDefaultAsync(p => p.Id == placeId && p.City.Id == cityId);
    if (place == null)
        return Results.NotFound();

    var countrydto = new CountryDto(city.Country.Id, city.Country.Name, city.Country.Description);
    var citydto = new CityDto(city.Id, city.Name, city.Description, countrydto);

    return Results.Ok(new PlaceDto(place.Id, place.Name, place.Description, citydto));
});

placesGroup.MapPost("places", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async (int cityId, int countryId, [Validate] CreatePlaceDto createPlaceDto, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var existingCountry = await dbContext.Countries.FindAsync(countryId);
    if (existingCountry == null)
    {
        return Results.NotFound();
    }
    var existingCity = await dbContext.Cities.FindAsync(cityId);
    if (existingCity == null)
    {
        return Results.NotFound();
    }

    var place = new Place
    {
        Name = createPlaceDto.Name,
        Description = createPlaceDto.Description,
        City = existingCity,
        UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
    };

    dbContext.Places.Add(place);
    await dbContext.SaveChangesAsync();

    // Create DTOs for response
    var cityDto = new CityDto(existingCity.Id, existingCity.Name, existingCity.Description,
        new CountryDto(existingCity.Country.Id, existingCity.Country.Name, existingCity.Country.Description));

    return Results.Created($"api/countries/{existingCity.Country.Id}/cities/{existingCity.Id}/places/{place.Id}",
        new PlaceDto(place.Id, place.Name, place.Description, cityDto));

});

placesGroup.MapPut("places/{placeId}", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async (int countryId, int cityId, int placeId, [Validate] UpdatePlaceDto dto, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();

    var place = await dbContext.Places.FirstOrDefaultAsync(c => c.Id == placeId && c.City.Id == cityId);
    if (place == null)
        return Results.NotFound();

    if (!httpContext.User.IsInRole(ForumRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != place.UserId)
    {
        return Results.Forbid();
    }

    place.Description = dto.Description;
    dbContext.Update(place);
    await dbContext.SaveChangesAsync();
    CityDto citydto = new CityDto(city.Id, city.Name, city.Description, new CountryDto(country.Id, country.Name, country.Description));
    return Results.Ok(new PlaceDto(place.Id, place.Name, place.Description, citydto));
});

placesGroup.MapDelete("places/{placeId}", [Authorize(Roles = ForumRoles.Admin + "," + ForumRoles.ForumUser)] async (int countryId, int cityId, int placeId, HttpContext httpContext, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();

    var place = await dbContext.Places.FirstOrDefaultAsync(p => p.Id == placeId && p.City.Id == cityId);
    if (place == null)
        return Results.NotFound();

    if (!httpContext.User.IsInRole(ForumRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != place.UserId)
    {
        return Results.Forbid();
    }


    dbContext.Remove(place);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});
#endregion


app.AddAuthApi();
app.UseAuthentication();
app.UseAuthorization();

using var scope = app.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<ForumDbContext>();
dbContext.Database.Migrate();
var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
await dbSeeder.SeedAsync();

app.Run();


