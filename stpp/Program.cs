using FluentValidation;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;
using stpp.Data;
using stpp.Data.Entities;
using System.Diagnostics.Metrics;
using static stpp.Data.Entities.City;
using static stpp.Data.Entities.Country;
using static stpp.Data.Entities.Place;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ForumDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
var app = builder.Build();

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

countriesGroup.MapPost("countries", async ([Validate] CreateCountryDto createCountryDto,  ForumDbContext dbContext) =>
{
    var country = new Country()
    {
        Name = createCountryDto.Name,
        Description = createCountryDto.Description
    };
    dbContext.Countries.Add(country);
    await dbContext.SaveChangesAsync();
    return Results.Created("api/countries/{country.Id}", new CountryDto(country.Id, country.Name, country.Description));
});

countriesGroup.MapPut("countries/{countryId}", async (int countryId, [Validate]UpdateCountryDto dto, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();
    country.Description = dto.Description;
    dbContext.Update(country);
    await dbContext.SaveChangesAsync();
    return Results.Ok(new CountryDto(country.Id, country.Name, country.Description));


});

countriesGroup.MapDelete("countries/{countryId}", async (int countryId, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();
    dbContext.Remove(country); 
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});


var citiesGroup = app.MapGroup("/api/countries/{countryId}").WithValidationFilter();


citiesGroup.MapGet("cities", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var cities = await dbContext.Cities.Include(c => c.Country).ToListAsync(cancellationToken);
    var cityDtos = cities.Select(city =>
    {
        var countryDto = city.Country != null
            ? new CountryDto(city.Country.Id, city.Country.Name, city.Country.Description)
            : null; // Handle the case where city.Country is null

        return new CityDto(city.Id, city.Name, city.Description, countryDto);
    }).ToList();

    return Results.Ok(cityDtos);
});


citiesGroup.MapGet("cities/{cityId}", async (int countryId, int cityId,ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();

    return Results.Ok(new CityDto(city.Id, city.Name, city.Description, new CountryDto(city.Country.Id, city.Country.Name, city.Country.Description)));
});



citiesGroup.MapPost("cities", async (int countryId, [Validate] CreateCityDto createCityDto, ForumDbContext dbContext) =>
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
        Country = existingCountry 
    };

    dbContext.Cities.Add(city);
    await dbContext.SaveChangesAsync();

    return Results.Created($"api/countries/{existingCountry.Id}/cities/{city.Id}", new CityDto(city.Id, city.Name, city.Description, new CountryDto(existingCountry.Id, existingCountry.Name, existingCountry.Description)));
});

citiesGroup.MapPut("cities/{cityId}", async (int countryId, int cityId, [Validate] UpdateCityDto dto, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();
    city.Description = dto.Description;
    dbContext.Update(city);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new CityDto(city.Id, city.Name, city.Description, new CountryDto(country.Id, country.Name, country.Description)));


});

citiesGroup.MapDelete("cities/{cityId}", async (int countryId, int cityId, ForumDbContext dbContext) =>
{
    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == countryId);
    if (country == null)
        return Results.NotFound();

    var city = await dbContext.Cities.FirstOrDefaultAsync(c => c.Id == cityId && c.Country.Id == countryId);
    if (city == null)
        return Results.NotFound();
    dbContext.Remove(city);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});




var placesGroup = app.MapGroup("/api/countries/{countryId}/cities/{cityId}").WithValidationFilter();


placesGroup.MapGet("places", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var places = await dbContext.Places
        .Include(p => p.City)
            .ThenInclude(c => c.Country)
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

placesGroup.MapPost("places", async (int cityId, int countryId, [Validate] CreatePlaceDto createPlaceDto, ForumDbContext dbContext) =>
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
        City = existingCity
    };

    dbContext.Places.Add(place);
    await dbContext.SaveChangesAsync();

    // Create DTOs for response
    var cityDto = new CityDto(existingCity.Id, existingCity.Name, existingCity.Description,
        new CountryDto(existingCity.Country.Id, existingCity.Country.Name, existingCity.Country.Description));

    return Results.Created($"api/countries/{existingCity.Country.Id}/cities/{existingCity.Id}/places/{place.Id}",
        new PlaceDto(place.Id, place.Name, place.Description, cityDto));

});

placesGroup.MapPut("places/{placeId}", async (int countryId, int cityId, int placeId, [Validate] UpdatePlaceDto dto, ForumDbContext dbContext) =>
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
    place.Description = dto.Description;
    dbContext.Update(place);
    await dbContext.SaveChangesAsync();
    CityDto citydto = new CityDto(city.Id, city.Name, city.Description, new CountryDto(country.Id, country.Name, country.Description));
    return Results.Ok(new PlaceDto(place.Id, place.Name, place.Description, citydto));
});

placesGroup.MapDelete("places/{placeId}", async (int countryId, int cityId, int placeId, ForumDbContext dbContext) =>
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

    dbContext.Remove(place);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});


app.Run();


