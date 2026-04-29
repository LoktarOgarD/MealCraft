using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MealCraft.Models;
using MealCraft.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace MealCraft.ViewModels
{
    /// <summary>
    /// Haupt-ViewModel der Anwendung.
    /// Enthält die Rezeptliste, Suche, Filter, Statistik und einfache CRUD-Funktionen.
    /// Ziel: eine stabile, verständliche Arbeitsversion für Day 3 und Day 4.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        // Konstanten für die Filter-ComboBoxen.
        public const string AllCategories = "Alle Kategorien";
        public const string AllDifficulties = "Alle Schwierigkeiten";

        // Interne Hauptliste. Diese Liste enthält wirklich alle Rezepte.
        private readonly ObservableCollection<Recipe> _allRecipes = new();

        // Gefilterte Sicht auf die Hauptliste. Die ListBox bindet an diese View.
        private readonly ICollectionView _view;

        /// <summary>
        /// Kategorien für den Kategorie-Filter links.
        /// </summary>
        public string[] CategoryFilters { get; }

        /// <summary>
        /// Schwierigkeitsgrade für den Schwierigkeits-Filter links.
        /// </summary>
        public string[] DifficultyFilters { get; }

        /// <summary>
        /// Kategorien für das Bearbeitungsfeld rechts.
        /// </summary>
        public RecipeCategory[] Categories { get; } = Enum.GetValues<RecipeCategory>();

        /// <summary>
        /// Schwierigkeitsgrade für das Bearbeitungsfeld rechts.
        /// </summary>
        public Difficulty[] Difficulties { get; } = Enum.GetValues<Difficulty>();

        /// <summary>
        /// Gefilterte Rezeptliste für die Anzeige in der ListBox.
        /// </summary>
        public ICollectionView FilteredRecipes => _view;

        private string _searchText = string.Empty;

        /// Suchtext für die Titelsuche.
        /// Jede Änderung aktualisiert sofort die Liste.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshView();
                }
            }
        }

        private string _selectedCategoryFilter = AllCategories;

        /// <summary>
        /// Aktiver Kategorie-Filter.
        /// </summary>
        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (SetProperty(ref _selectedCategoryFilter, value))
                {
                    RefreshView();
                }
            }
        }

        private string _selectedDifficultyFilter = AllDifficulties;

        /// <summary>
        /// Aktiver Schwierigkeits-Filter.
        /// </summary>
        public string SelectedDifficultyFilter
        {
            get => _selectedDifficultyFilter;
            set
            {
                if (SetProperty(ref _selectedDifficultyFilter, value))
                {
                    RefreshView();
                }
            }
        }

        /// <summary>
        /// Aktuell ausgewähltes Rezept.
        /// Die Detailfelder rechts binden an dieses Objekt.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteRecipeCommand))]
        [NotifyPropertyChangedFor(nameof(HasSelectedRecipe))]
        private Recipe? selectedRecipe;

        /// <summary>
        /// True, wenn ein Rezept ausgewählt ist.
        /// Kann später für Sichtbarkeit oder Aktivierung verwendet werden.
        /// </summary>
        public bool HasSelectedRecipe => SelectedRecipe is not null;

        /// <summary>
        /// Statistik für die kleine Übersicht im UI.
        /// </summary>
        public RecipeStats Stats
        {
            get
            {
                var visibleRecipes = _view.Cast<Recipe>().ToList();

                return new RecipeStats(
                    TotalVisible: visibleRecipes.Count,
                    TotalAll: _allRecipes.Count,
                    ActiveCategory: SelectedCategoryFilter == AllCategories ? "Alle" : SelectedCategoryFilter,
                    AvgPrepTime: visibleRecipes.Count > 0
                        ? (int)Math.Round(visibleRecipes.Average(recipe => recipe.PreparationTimeMinutes))
                        : 0
                );
            }
        }

        public MainViewModel()
        {
            // Filterlisten vorbereiten.
            CategoryFilters = new[] { AllCategories }
                .Concat(Enum.GetValues<RecipeCategory>().Select(category => category.ToString()))
                .ToArray();

            DifficultyFilters = new[] { AllDifficulties }
                .Concat(Enum.GetValues<Difficulty>().Select(difficulty => difficulty.ToString()))
                .ToArray();

            // CollectionView einrichten.
            _view = CollectionViewSource.GetDefaultView(_allRecipes);
            _view.Filter = ApplyFilter;
            _view.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Stats));

            LoadSampleRecipes();
        }

        /// <summary>
        /// Prüft, ob ein Rezept bei den aktuellen Filtern sichtbar sein soll.
        /// </summary>
        private bool ApplyFilter(object item)
        {
            if (item is not Recipe recipe)
                return false;

            bool matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                || recipe.Title.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);

            bool matchesCategory = SelectedCategoryFilter == AllCategories
                || recipe.Category.ToString() == SelectedCategoryFilter;

            bool matchesDifficulty = SelectedDifficultyFilter == AllDifficulties
                || recipe.Difficulty.ToString() == SelectedDifficultyFilter;

            return matchesSearch && matchesCategory && matchesDifficulty;
        }

        /// <summary>
        /// Fügt ein neues Rezept hinzu und wählt es direkt aus.
        /// Danach werden die Filter zurückgesetzt, damit das neue Rezept sichtbar bleibt.
        /// </summary>
        [RelayCommand]
        private void AddRecipe()
        {
            try
            {
                var recipe = new Recipe
                {
                    Title = "Neues Rezept",
                    Category = RecipeCategory.Miscellaneous,
                    Difficulty = Difficulty.Easy,
                    PreparationTimeMinutes = 30,
                    Ingredients = "Zutat 1",
                    Steps = "Schritt 1"
                };

                _allRecipes.Add(recipe);
                ClearFilters();
                SelectedRecipe = recipe;
                RefreshView();
            }
            catch (Exception ex)
            {
                ShowError("Das Rezept konnte nicht hinzugefügt werden.", ex);
            }
        }

        /// <summary>
        /// Löscht das ausgewählte Rezept nach einer Sicherheitsabfrage.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteRecipe))]
        private void DeleteRecipe()
        {
            try
            {
                if (SelectedRecipe is null)
                    return;

                var result = MessageBox.Show(
                    $"Möchten Sie \"{SelectedRecipe.Title}\" wirklich löschen?",
                    "Rezept löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                _allRecipes.Remove(SelectedRecipe);

                RefreshView();
                SelectedRecipe = _view.Cast<Recipe>().FirstOrDefault();
            }
            catch (Exception ex)
            {
                ShowError("Das Rezept konnte nicht gelöscht werden.", ex);
            }
        }

        /// <summary>
        /// Rezept kann nur gelöscht werden, wenn wirklich ein Rezept ausgewählt ist.
        /// </summary>
        private bool CanDeleteRecipe() => SelectedRecipe is not null;

        /// <summary>
        /// Setzt Suchtext und Filter zurück.
        /// </summary>
        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategoryFilter = AllCategories;
            SelectedDifficultyFilter = AllDifficulties;
        }

        /// <summary>
        /// Prüft die Eingaben des ausgewählten Rezepts.
        /// Fehler werden als MessageBox angezeigt, damit es für das Schulprojekt einfach bleibt.
        /// </summary>
        [RelayCommand]
        private void ValidateSelectedRecipe()
        {
            try
            {
                if (SelectedRecipe is null)
                {
                    MessageBox.Show(
                        "Bitte wählen Sie zuerst ein Rezept aus.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var errors = RecipeValidationService.Validate(SelectedRecipe);

                if (errors.Length == 0)
                {
                    MessageBox.Show(
                        "Das Rezept ist gültig.",
                        "Validierung",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                MessageBox.Show(
                    string.Join(Environment.NewLine, errors),
                    "Validierungsfehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                ShowError("Die Validierung konnte nicht durchgeführt werden.", ex);
            }
        }

        /// <summary>
        /// Lädt Beispielrezepte für den ersten Start.
        /// Dadurch ist die Oberfläche sofort testbar.
        /// </summary>
        private void LoadSampleRecipes()
        {
            _allRecipes.Add(new Recipe
            {
                Title = "Spaghetti Carbonara",
                Category = RecipeCategory.Dinner,
                Difficulty = Difficulty.Medium,
                PreparationTimeMinutes = 25,
                Ingredients = "Spaghetti\nEier\nPecorino\nPancetta\nPfeffer",
                Steps = "Pasta kochen\nPancetta anbraten\nEier mit Käse mischen\nAlles cremig vermengen"
            });

            _allRecipes.Add(new Recipe
            {
                Title = "Pfannkuchen",
                Category = RecipeCategory.Breakfast,
                Difficulty = Difficulty.Easy,
                PreparationTimeMinutes = 20,
                Ingredients = "Mehl\nEier\nMilch\nButter\nSalz",
                Steps = "Teig anrühren\nPfanne erhitzen\nPfannkuchen backen\nServieren"
            });

            _allRecipes.Add(new Recipe
            {
                Title = "Tomatensuppe",
                Category = RecipeCategory.Soup,
                Difficulty = Difficulty.Easy,
                PreparationTimeMinutes = 35,
                Ingredients = "Tomaten\nZwiebel\nKnoblauch\nGemüsebrühe\nBasilikum",
                Steps = "Zwiebel anbraten\nTomaten hinzufügen\nKöcheln lassen\nPürieren\nWürzen"
            });

            _allRecipes.Add(new Recipe
            {
                Title = "Schokoladenmousse",
                Category = RecipeCategory.Dessert,
                Difficulty = Difficulty.Hard,
                PreparationTimeMinutes = 45,
                Ingredients = "Zartbitterschokolade\nEier\nSahne\nZucker",
                Steps = "Schokolade schmelzen\nEigelb einrühren\nSahne steif schlagen\nAlles vorsichtig unterheben\nKaltstellen"
            });

            _allRecipes.Add(new Recipe
            {
                Title = "Caesar Salad",
                Category = RecipeCategory.Salad,
                Difficulty = Difficulty.Easy,
                PreparationTimeMinutes = 15,
                Ingredients = "Römersalat\nParmesan\nCroutons\nCaesar-Dressing",
                Steps = "Salat waschen\nDressing zugeben\nParmesan und Croutons hinzufügen"
            });

            RefreshView();
            SelectedRecipe = _allRecipes.FirstOrDefault();
        }

        /// <summary>
        /// Aktualisiert die gefilterte Liste und die Statistik.
        /// </summary>
        private void RefreshView()
        {
            _view.Refresh();
            OnPropertyChanged(nameof(Stats));
        }

        /// <summary>
        /// Zeigt technische Fehler verständlich an.
        /// </summary>
        private static void ShowError(string message, Exception ex)
        {
            MessageBox.Show(
                $"{message}\n\nTechnische Details:\n{ex.Message}",
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Kleines Datenobjekt für die Statistik im UI.
    /// </summary>
    public record RecipeStats(
        int TotalVisible,
        int TotalAll,
        string ActiveCategory,
        int AvgPrepTime);
}
