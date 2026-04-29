# MealCraft

MealCraft ist eine kleine WPF-Anwendung zur Verwaltung von Kochrezepten.
Das Projekt wurde für eine Projektwoche erstellt und nutzt C#, .NET 8, WPF und das MVVM-Muster.

## Aktueller Stand

### Day 1
- WPF-Projekt erstellt
- Grundstruktur erstellt
- Model `Recipe` erstellt
- Beispielrezepte hinzugefügt

### Day 2
- Rezeptliste anzeigen
- Rezept auswählen
- Rezeptdetails bearbeiten
- Neues Rezept hinzufügen
- Rezept löschen
- Einfache Validierung

### Day 3
- Suche nach Rezepttitel
- Filter nach Kategorie
- Filter nach Schwierigkeit
- Statistikbereich mit sichtbaren Rezepten, Gesamtanzahl und durchschnittlicher Zubereitungszeit
- UI verbessert

## Funktionen

- Rezepte anzeigen
- Rezepte hinzufügen
- Rezepte direkt bearbeiten
- Rezepte löschen
- Eingaben prüfen
- Suche und Filter verwenden
- Einfache Statistik anzeigen

## Projektstruktur

```text
MealCraft/
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── MainViewModel.cs
├── Recipe.cs
├── RecipeCategory.cs
├── RecipeValidationService.cs
└── MealCraft.csproj
```

## Technologie

- C#
- .NET 8
- WPF
- CommunityToolkit.Mvvm 8.4.2
- MVVM Pattern

## Nächster Schritt: Day 4

- JSON speichern/laden
- README finalisieren
- Kurzpräsentation vorbereiten
