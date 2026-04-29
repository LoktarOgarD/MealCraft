using MealCraft.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MealCraft.Services
{
    /// <summary>
    /// Speichert und lädt Rezepte als JSON-Datei.
    /// Diese Klasse enthält keine UI-Logik und kann später leicht erweitert werden.
    /// </summary>
    public static class RecipeStorageService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Speichert alle Rezepte in einer JSON-Datei.
        /// </summary>
        public static void Save(string filePath, IEnumerable<Recipe> recipes)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Der Dateipfad darf nicht leer sein.", nameof(filePath));

            string json = JsonSerializer.Serialize(recipes, JsonOptions);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Lädt Rezepte aus einer JSON-Datei.
        /// Falls die Datei leer ist, wird eine leere Liste zurückgegeben.
        /// </summary>
        public static List<Recipe> Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Der Dateipfad darf nicht leer sein.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Die JSON-Datei wurde nicht gefunden.", filePath);

            string json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return new List<Recipe>();

            return JsonSerializer.Deserialize<List<Recipe>>(json, JsonOptions)
                   ?? new List<Recipe>();
        }
    }
}