using FluentValidation;
using System.ComponentModel.DataAnnotations;
using stpp.Auth.Model;
using static stpp.Data.Entities.City;
using static stpp.Data.Entities.Country;

namespace stpp.Data.Entities
{
    public class Place
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; }
        public required City City { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User { get; set; }

        public record PlaceDto(int Id, string Name, string Description, CityDto City);
        public record CreatePlaceDto(string Name, string Description, int CityId); // Representing the associated city as a DTO);

        public record UpdatePlaceDto(string Description);
        public class CreatePlaceDtoValidator : AbstractValidator<CreatePlaceDto>
        {
            public CreatePlaceDtoValidator()
            {
                RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(2, 60);
                RuleFor(dto => dto.Description).NotEmpty().Length(10, 350);
            }
        }

        public class UpdatePlaceDtoValidator : AbstractValidator<UpdatePlaceDto>
        {
            public UpdatePlaceDtoValidator()
            {
                RuleFor(dto => dto.Description).NotEmpty().Length(10, 350);
            }
        }

    }
}
