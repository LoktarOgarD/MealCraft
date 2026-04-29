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
    /// Enthält Rezeptliste, Suche, Filter, Statistik, CRUD und JSON-Speichern/Laden.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        public const string AllCategories = "Alle Kategorien";
        public const string AllDifficulties = "Alle Schwierigkeiten";

        private const string StorageFilePath = "recipes.json";

        private readonly ObservableCollection<Recipe> _allRecipes = new();
        private readonly ICollectionView _view;

        public string[] CategoryFilters { get; }
        public string[] DifficultyFilters { get; }

        public RecipeCategory[] Categories { get; } = Enum.GetValues<RecipeCategory>();
        public Difficulty[] Difficulties { get; } = Enum.GetValues<Difficulty>();

        public ICollectionView FilteredRecipes => _view;

        private string _searchText = string.Empty;

        /// <summary>
        /// Suchtext für die Rezeptsuche.
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
        /// Die Detailansicht bindet an dieses Objekt.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteRecipeCommand))]
        [NotifyPropertyChangedFor(nameof(HasSelectedRecipe))]
        private Recipe? selectedRecipe;

        public bool HasSelectedRecipe => SelectedRecipe is not null;

        /// <summary>
        /// Statistik für die Anzeige im UI.
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
            CategoryFilters = new[] { AllCategories }
                .Concat(Enum.GetValues<RecipeCategory>().Select(category => category.ToString()))
                .ToArray();

            DifficultyFilters = new[] { AllDifficulties }
                .Concat(Enum.GetValues<Difficulty>().Select(difficulty => difficulty.ToString()))
                .ToArray();

            _view = CollectionViewSource.GetDefaultView(_allRecipes);
            _view.Filter = ApplyFilter;

            _allRecipes.CollectionChanged += (_, e) =>
            {
                if (e.NewItems is not null)
                {
                    foreach (Recipe recipe in e.NewItems)
                    {
                        recipe.PropertyChanged += Recipe_PropertyChanged;
                    }
                }

                if (e.OldItems is not null)
                {
                    foreach (Recipe recipe in e.OldItems)
                    {
                        recipe.PropertyChanged -= Recipe_PropertyChanged;
                    }
                }

                RefreshView();
            };

            LoadSampleRecipes();
        }

        /// <summary>
        /// Reagiert auf Änderungen eines Rezepts.
        /// Dadurch werden Filter und Statistik sofort aktualisiert.
        /// </summary>
        private void Recipe_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RefreshView();
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
        /// Löscht das ausgewählte Rezept.
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

        private bool CanDeleteRecipe() => SelectedRecipe is not null;

        /// <summary>
        /// Setzt Suche und Filter zurück.
        /// </summary>
        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategoryFilter = AllCategories;
            SelectedDifficultyFilter = AllDifficulties;
        }

        /// <summary>
        /// Prüft das ausgewählte Rezept auf einfache Eingabefehler.
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
        /// Speichert alle Rezepte in einer JSON-Datei.
        /// </summary>
        [RelayCommand]
        private void SaveRecipes()
        {
            try
            {
                RecipeStorageService.Save(StorageFilePath, _allRecipes);

                MessageBox.Show(
                    "Rezepte wurden erfolgreich gespeichert.",
                    "Speichern",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("Die Rezepte konnten nicht gespeichert werden.", ex);
            }
        }

        /// <summary>
        /// Lädt Rezepte aus einer JSON-Datei.
        /// Die bestehende Liste wird ersetzt.
        /// </summary>
        [RelayCommand]
        private void LoadRecipes()
        {
            try
            {
                var loadedRecipes = RecipeStorageService.Load(StorageFilePath);

                _allRecipes.Clear();

                foreach (var recipe in loadedRecipes)
                {
                    _allRecipes.Add(recipe);
                }

                ClearFilters();
                RefreshView();
                SelectedRecipe = _view.Cast<Recipe>().FirstOrDefault();

                MessageBox.Show(
                    "Rezepte wurden erfolgreich geladen.",
                    "Laden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("Die Rezepte konnten nicht geladen werden.", ex);
            }
        }

        /// <summary>
        /// Lädt Beispielrezepte für den ersten Start.
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
        /// Aktualisiert gefilterte Liste und Statistik.
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
    /// Datenobjekt für die Statistik.
    /// </summary>
    public record RecipeStats(
        int TotalVisible,
        int TotalAll,
        string ActiveCategory,
        int AvgPrepTime);
}