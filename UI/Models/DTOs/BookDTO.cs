using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UI.Models.DTOs
{
    public class BookDTO
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        [Required]
        [Display(Name = "Назва")]
        public string Title { get; set; }

        [JsonProperty("author")]
        [Required]
        [Display(Name = "Автор")]
        public string Author { get; set; }

        [JsonProperty("description")]
        [Display(Name = "Опис")]
        public string Description { get; set; }

        [JsonProperty("genre")]
        public int GenreId { get; set; }

        [JsonProperty("type")]
        public int TypeId { get; set; }

        [JsonProperty("isAvailable")]
        [Display(Name = "Доступна")]
        public bool IsAvailable { get; set; }

        [JsonProperty("year")]
        [Display(Name = "Рік видання")]
        public DateTime Year { get; set; }

        public string OrderId { get; set; }

        // Властивості для UI (не серіалізуються в JSON)
        [JsonIgnore]
        [Required]
        [Display(Name = "Жанр")]
        public string Genre
        {
            get => ConvertIntToGenre(GenreId);
            set => GenreId = ConvertGenreToInt(value);
        }

        [JsonIgnore]
        [Required]
        [Display(Name = "Тип")]
        public string Type
        {
            get => ConvertIntToBookType(TypeId);
            set => TypeId = ConvertBookTypeToInt(value);
        }

        // Додаткове поле для роботи з роком як числом
        [JsonIgnore]
        [NotMapped]
        [Display(Name = "Рік видання")]
        [Range(1000, 2030, ErrorMessage = "Рік повинен бути між 1000 та 2030")]
        public int YearNumber
        {
            get => Year.Year;
            set => Year = new DateTime(value, 1, 1);
        }

        // Властивості для відображення
        [JsonIgnore]
        public string GenreDisplayName => ConvertIntToGenre(GenreId);

        [JsonIgnore]
        public string TypeDisplayName => ConvertIntToBookType(TypeId);

        [JsonIgnore]
        public string AvailabilityDisplayName => IsAvailable ? "Доступна" : "Недоступна";

        // Методи конвертації
        private string ConvertIntToGenre(int genreId)
        {
            return genreId switch
            {
                1 => "Fiction",
                2 => "Science",
                3 => "History",
                4 => "Biography",
                5 => "Fantasy",
                6 => "Mystery",
                7 => "Romance",
                8 => "Thriller",
                _ => ""
            };
        }

        private string ConvertIntToBookType(int typeId)
        {
            return typeId switch
            {
                0 => "Physical",
                1 => "Digital",
                2 => "Audio",
                _ => ""
            };
        }

        private int ConvertGenreToInt(string genre)
        {
            return genre switch
            {
                "Fiction" => 1,
                "Science" => 2,
                "History" => 3,
                "Biography" => 4,
                "Fantasy" => 5,
                "Mystery" => 6,
                "Romance" => 7,
                "Thriller" => 8,
                _ => 0
            };
        }

        private int ConvertBookTypeToInt(string type)
        {
            return type switch
            {
                "Physical" => 0,
                "Digital" => 1,
                "Audio" => 2,
                _ => 0
            };
        }

        // Методи для відображення українською
        public string GetGenreDisplayNameUkrainian()
        {
            return GenreId switch
            {
                1 => "Художня література",
                2 => "Наукова",
                3 => "Історична",
                4 => "Біографія",
                5 => "Фентезі",
                6 => "Детектив",
                7 => "Романтика",
                8 => "Трилер",
                _ => "Невідомо"
            };
        }

        public string GetTypeDisplayNameUkrainian()
        {
            return TypeId switch
            {
                0 => "Фізична",
                1 => "Цифрова",
                2 => "Аудіо",
                _ => "Невідомо"
            };
        }
    }

    // Кастомний валідатор для року
    public class YearValidationAttribute : ValidationAttribute
    {
        private readonly int _minYear;
        private readonly int _maxYear;

        public YearValidationAttribute(int minYear, int maxYear)
        {
            _minYear = minYear;
            _maxYear = maxYear;
        }

        public override bool IsValid(object value)
        {
            if (value is DateTime dateTime)
            {
                int year = dateTime.Year;
                return year >= _minYear && year <= _maxYear;
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Рік повинен бути від {_minYear} до {_maxYear}";
        }
    }
}