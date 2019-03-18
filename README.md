Stand:

- Gebäudegeometrie-Übertragung nach Revit (namespace-unabhängig) zu 80 - 90 Prozent
- Parameter-Übertragung nach Revit möglich (leere Shared Parameter Datei muss vorhanden sein)
- simple Georef funktioniert (wenn PBP bei 0/0/0, wenn Import in m, und Porjekt in m; einfache Feet-Meter Umrechnung) 


Nächste Schritte:

----Semantik----
- Auslesen der Semantik pro Gebäude aus CityGML.
- Erstellen der Parameter für Revit anhand von CityGML-Attributen. (für jeden File neu --> dynamisch)
- Füllen der Attributwerte (wenn vorhanden)
- ggf. sinnvolle Gruppierung implementieren (def: City Model data)
- ggf. verschiedene BuiltIN-Parameter Gruppen nutzen (def: Data)

----Georef----
- Projekteinheit aus Revit auslesen.
- Umrechnung der Koordinaten zu Projekteinheit (Achtung: auch bei m zu m muss feet-Fehler beachtet werden).
- Interne Umrechnung bezogen auf Projektbasispunkt implementieren.
- Ungleiche CRS abfangen (Workaround überlegen, GUI?).

----Geometrie---- (später)
- Erhöhung der Prozentzahl transformierter Gebäude.
- Heilen bei fehlenden Flächen.