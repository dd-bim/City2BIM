**Stand (26.04.2019):**

----Geometrie----

- Gebäudegeometrie-Übertragung nach Revit (namespace-unabhängig) zu 80 - 90 Prozent

----Semantik----

- Auslesen der Semantik pro Gebäude aus CityGML funktioniert
- Erstellen der Parameter für Revit anhand von CityGML-Attributen implementiert
- Füllen der Attributwerte funktioniert
- verschiedene Parametertypen anhand Vorgaben aus CityGML implementiert (wenn möglich) --> Text, Number, Integer, Measure 
- Gruppierung der Parameter erfolgt nach CityGML-Modulen (bldg, core, gen, xal) 

----Georef----

- simple Georef funktioniert (wenn PBP bei 0/0/0, wenn Import in m, und Porjekt in m; einfache Feet-Meter Umrechnung) 


**Nächste Schritte:**

----Georef----
- Projekteinheit aus Revit auslesen.
- Umrechnung der Koordinaten zu Projekteinheit (Achtung: auch bei m zu m muss feet-Fehler beachtet werden).
- Interne Umrechnung bezogen auf Projektbasispunkt implementieren.
- Ungleiche CRS abfangen (Workaround überlegen, GUI?).

----Geometrie---- (später)
- Erhöhung der Prozentzahl transformierter Gebäude.
- Heilen bei fehlenden Flächen.