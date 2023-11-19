using FluentValidation;
using System.ComponentModel.DataAnnotations;
using stpp.Auth.Model;
using static stpp.Data.Entities.Country;

namespace stpp.Data.Entities
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public required Country Country { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User { get; set; }

        public record CityDto(int Id, string Name, string Description, CountryDto Country);
        public record CreateCityDto(string Name, string Description, int CountryId); // Representing the associated country as a DTO);

        public record UpdateCityDto(string Description);
        public class CreateCityDtoValidator : AbstractValidator<CreateCityDto>
        {
            public CreateCityDtoValidator()
            {
                RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(2, 60);
                RuleFor(dto => dto.Description).NotEmpty().Length(10, 350);
            }
        }

        public class UpdateCityDtoValidator : AbstractValidator<UpdateCityDto>
        {
            public UpdateCityDtoValidator()
            {
                RuleFor(dto => dto.Description).NotEmpty().Length(10, 350);
            }
        }
    }
}
