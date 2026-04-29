using MealCraft.Models;
using System.Collections.Generic;
using System.Linq;

namespace MealCraft.Services
{
    /// <summary>
    /// Prüft, ob ein Rezept gültige Eingaben enthält.
    /// Die Klasse ist von der UI getrennt und kann später wiederverwendet werden.
    /// </summary>
    public static class RecipeValidationService
    {
        /// <summary>
        /// Validiert ein Rezept und gibt eine Liste mit Fehlermeldungen zurück.
        /// Eine leere Liste bedeutet: Das Rezept ist gültig.
        /// </summary>
        public static string[] Validate(Recipe recipe)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(recipe.Title))
                errors.Add("Der Titel darf nicht leer sein.");

            if (recipe.Title.Length > 100)
                errors.Add("Der Titel darf maximal 100 Zeichen lang sein.");

            if (recipe.PreparationTimeMinutes <= 0)
                errors.Add("Die Zubereitungszeit muss größer als 0 sein.");

            if (recipe.PreparationTimeMinutes > 1440)
                errors.Add("Die Zubereitungszeit darf nicht länger als 24 Stunden sein.");

            if (string.IsNullOrWhiteSpace(recipe.Ingredients))
                errors.Add("Bitte geben Sie mindestens eine Zutat ein.");

            if (string.IsNullOrWhiteSpace(recipe.Steps))
                errors.Add("Bitte geben Sie mindestens einen Zubereitungsschritt ein.");

            return errors.ToArray();
        }
    }
}