using System.ComponentModel.DataAnnotations;

namespace UI.Models.ViewModels
{
    public enum GenreTypes
    {
        [Display(Name = "Бойовик")]
        Action,

        [Display(Name = "Пригоди")]
        Adventure,

        [Display(Name = "Фентезі")]
        Fantasy,

        [Display(Name = "Жахи")]
        Horror,

        [Display(Name = "Детектив")]
        Mystery,

        [Display(Name = "Романтика")]
        Romance,

        [Display(Name = "Наукова фантастика")]
        ScienceFiction,

        [Display(Name = "Трилер")]
        Thriller,

        [Display(Name = "Вестерн")]
        Western,

        [Display(Name = "Історична література")]
        HistoricalFiction,

        [Display(Name = "Документальна")]
        NonFiction
    }
    public enum BookTypes
    {
        [Display(Name = "Паперова")]
        Paper,

        [Display(Name = "Аудіо")]
        Audio,

        [Display(Name = "Електронна")]
        Ebook
    }
}

