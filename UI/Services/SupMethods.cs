namespace UI.Services
{
    public class SupMethods
    {

        public static string ConvertIntToGenre(int genreId)
        {
            return genreId switch
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

        public static string ConvertIntToBookType(int typeId)
        {
            return typeId switch
            {
                0 => "Фізична",
                1 => "Цифрова",
                2 => "Аудіо",
                _ => "Невідомо"
            };
        }

        public static int ConvertGenreToNumber(string genre)
        {
            return genre switch
            {
                "Fiction" => 0,
                "Science" => 1,
                "History" => 2,
                "Biography" => 3,
                "Fantasy" => 4,
                "Mystery" => 5,
                "Romance" => 6,
                "Thriller" => 7,
                "Drama" => 8,
                _ => 0 // За замовчуванням Fiction
            };
        }

        public static int ConvertTypeToNumber(string type)
        {
            return type switch
            {
                "Physical" => 0,
                "Digital" => 1,
                "Audio" => 2,
                _ => 0 // За замовчуванням Physical
            };
        }

        public static int ConvertGenreToInt(string genre)
        {
            var result = genre switch
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

            return result;
        }

        public static int ConvertBookTypeToInt(string type)
        {
            var result = type switch
            {
                "Physical" => 0,
                "Digital" => 1,
                "Audio" => 2,
                _ => 0
            };

            return result;
        }

        public static int ConvertGenreToInt(object genre)
        {
            if (genre is int intGenre) return intGenre;
            if (genre is string strGenre && int.TryParse(strGenre, out int parsedGenre)) return parsedGenre;
            if (genre is Enum enumGenre) return Convert.ToInt32(enumGenre);
            return 0; // default value
        }

        public static int ConvertTypeToInt(object type)
        {
            if (type is int intType) return intType;
            if (type is string strType && int.TryParse(strType, out int parsedType)) return parsedType;
            if (type is Enum enumType) return Convert.ToInt32(enumType);
            return 0; // default value
        }
    }
}
