using FluentValidation;
using System.ComponentModel.DataAnnotations;
using static stpp.Data.Entities.Country;

namespace stpp.Data.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }

        public string EntityType { get; set; }
        public int EntityId { get; set; }

        [Required]
        public required string UserId { get; set; }

        public record CommentDto(int Id, string Content, string EntityType, int EntityId, string UserId);
        public record CreateCommentDto(string Content, string EntityType, int EntityId); // Representing the associated country as a DTO);

        public record UpdateCommentDto(string Content);
        public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
        {
            public CreateCommentDtoValidator()
            {
                RuleFor(dto => dto.Content).NotEmpty().NotNull().Length(1, 350);
            }
        }

        public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
        {
            public UpdateCommentDtoValidator()
            {
                RuleFor(dto => dto.Content).NotEmpty().NotNull().Length(1, 350);
            }
        }
    }
}
