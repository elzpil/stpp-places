using FluentValidation;
using stpp.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace stpp.Data.Entities
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; }    
        public string Description { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ForumRestUser User { get; set; }

    
    public record CountryDto(int Id, string Name, string Description);
    public record CreateCountryDto(string Name, string Description);

    public record UpdateCountryDto(string Description);
    public class CreateCoutryDtoValidator : AbstractValidator<CreateCountryDto>
    {
        public CreateCoutryDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(2, 60);
            RuleFor(dto => dto.Description).NotEmpty().Length(10, 350);
        }
    }

    public class UpdateCoutryDtoValidator : AbstractValidator<UpdateCountryDto>
    {
        public UpdateCoutryDtoValidator()
        {
            RuleFor(dto => dto.Description).NotEmpty().Length(10, 350);
        }
    }
    }

}
