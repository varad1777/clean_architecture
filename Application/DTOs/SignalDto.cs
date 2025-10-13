using System.ComponentModel.DataAnnotations;

namespace MyApp.Application.DTO
{
    public class SignalDto
    {

        [Required(ErrorMessage = "Signal Name is Required")]
        [Length(2, 15, ErrorMessage = "Signal Name must be between {1} and {2} character.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Signal Description is Required")]
        [Length(2, 100, ErrorMessage = " Signal Description must be between {1} and {2} character.")]
        public string Description { get; set; }

    }
}