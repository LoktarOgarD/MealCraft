using CommunityToolkit.Mvvm.ComponentModel;

namespace MealCraft.Models
{
    /// <summary>
    /// Repräsentiert ein einzelnes Kochrezept.
    /// Diese Klasse enthält nur Daten und keine UI-Logik.
    /// </summary>
    public partial class Recipe : ObservableObject
    {
        /// <summary>
        /// Titel des Rezepts, z.B. "Spaghetti Carbonara".
        /// </summary>
        [ObservableProperty]
        private string title = string.Empty;

        /// <summary>
        /// Kategorie des Rezepts, z.B. Frühstück, Suppe oder Dessert.
        /// </summary>
        [ObservableProperty]
        private RecipeCategory category = RecipeCategory.Miscellaneous;

        /// <summary>
        /// Schwierigkeitsgrad des Rezepts.
        /// </summary>
        [ObservableProperty]
        private Difficulty difficulty = Difficulty.Medium;

        /// <summary>
        /// Zubereitungszeit in Minuten.
        /// </summary>
        [ObservableProperty]
        private int preparationTimeMinutes = 30;

        /// <summary>
        /// Zutaten als einfacher Text.
        /// Jede Zutat kann in eine neue Zeile geschrieben werden.
        /// </summary>
        [ObservableProperty]
        private string ingredients = string.Empty;

        /// <summary>
        /// Zubereitungsschritte als einfacher Text.
        /// Jeder Schritt kann in eine neue Zeile geschrieben werden.
        /// </summary>
        [ObservableProperty]
        private string steps = string.Empty;
    }
}