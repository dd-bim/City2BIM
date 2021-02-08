# RoadMap CityBIM-Plugin

## OGR
- Teil von GDAL für Verarbeitung von Vektordaten
- Viele Driver für Lesen bestimmter Datenformate

### Installation
- Folgende NuGet-Pakete müssen eingebunden werden:
  - [GDAL](https://www.nuget.org/packages/GDAL/)
  - [GDAL Native](https://www.nuget.org/packages/GDAL.Native/)

### Startpunkte fr GDAL / OGR
- [Vector API Tutorial](https://gdal.org/tutorials/vector_api_tut.html)
  - Ist hauptschlich fr C / C++ und Python beschrieben. Aber die Grundkonzepte leicht auf C# übertragbar. Auch in [C++ API Dokumentation](https://gdal.org/doxygen/index.html) nachschauen
- [Geoprocessing with Python](https://learning.oreilly.com/library/view/geoprocessing-with-python/9781617292149/)
  - gut um Konzepte in OGR zu verstehen. Praktische Implementierung jedoch für Python
  - nur mit SLUB-Account

### Geometrie
- Noch nicht ganz klar, wie Bögengeometrien verarbeitet werden?
- Methoden sowohl für Ausgabe der Geometrie als lineare Approximation als auch mit Bögen verfügbar (https://gdal.org/api/ogrgeometry_cpp.html)
- Multipolygone können gelesen werden (ALKIS)

## Geodaten

### ALKIS
- Einlesen mit [OGR NAS-Driver](https://gdal.org/drivers/vector/nas.html#vector-nas)
- Geometrie und Sachdaten können gelesen werden --> kompletter Re-write ?

### INSPIRE Parcel-Data
- Einelesen mit [OGR GMLAS-Driver](https://gdal.org/drivers/vector/gmlas.html#vector-gmlas)?
- Vorteil: Flurstcksdaten fr ganz Europa?

### X-Planung
- Einelesen mit [OGR GMLAS-Driver](https://gdal.org/drivers/vector/gmlas.html#vector-gmlas)?
- Geometrie und Sachdaten knnen gelesen werden --> kompletter Re-write ?
- Welche Objekte sind wichtig? 


## Revit

### Aufbau Solution und Projektdateien

- Solution (GeospatialEngineeringBIM)
  - BIMGISInteropLibs (alle mit Serilog)
    - Georef (Bibliothek für LoGeoRef, CRS-Berechnungen)
    - Terrain (Bibliothek für DGM-Import, Modellbildung)
    - CityGML (Bibliothek zum Lesen und Heilen von CityGML)
    - Alkis
    - XPlanung
    - SurveyorsPlan (Biliothek zum CAD Import für topographische Objekte)
      - DXF (Punkte,Linien,Flächen,Layer)
      - Anbinden von Objektbibliotheken (?)
    - datacat
  - City2Rvt (Revit-Plugin mit Revit API) 
    - Georef 
    - Terrain
    - CityGML
    - ALKIS
    - XPlanung
    - datacat
    - SurveyorsPlan
    - Options (Manage Properties / Hide Surfaces / log )    
  - City2IFC (IFC Konverter, stand alone)
  - Georefchecker (exe)
  - IfcTerrain (exe mit und ihne GUI)
  - SurveyorsPlanToIfc
  - UnitTests

### Versionsmanagement, Bauen und Testen
- Studierende und Mitarbeiter arbeiten im eigenen Branch und senden regelmäßig Pull Requests
- Es gibt einen Testdatensatz (Ingoldstadt, DD-BIM Ordner auf kfs1) zum Test der City2BIM Plugins Releaseversionen in allen Revit Versionen (2019-2021)
- Kurze Testanleitung schreiben (Georef->DGM->,CityGML,ALKIS, IFC-Export)


### Site Subregion
- Performance sehr schlecht, wenn mehr als ca. 20 Subregions pro TopographySurface definiert werden!
```C#
	SiteSubRegion.Create()
```
- Noch mehr TopographySurfaces für Flurstücke anlegen und diese in 10er- 20er Pakete erstellen?
  - Nachteil: Noch viel mehr "unnötiges" Gelände in Revit-Dokument
- Anderes Revit-Bauteil verwenden?
- Work-Around: ES gibt einen räumlichen Filter / Buffer um den Projektbasispunkt ca. 100 m (default) 
  - ALKIS +/-100 m
  - XPlanung  +/- 250 m 
  
### Materialien
- Für Alkis und XPlanung nach Umstrukturierung keine Materialdefinition mehr
- Siehe Felix ProgressPatch / XXX.cs

### Surveyors Plan to Revit
- (MA Marcus Schröder)

## IFC-Export

### Umgebungsmodelle Master
- Revit Projektdateien neu aufsetzen? Für verschiedene Rvt Versionen (2019, 2020, 2021)? --> dauert lange
- Export durchführen. Fehler dokumentieren
- Tim an Marcus

## Dokumentation und Testung

- Berarbeitung nötig :(in Wiki)
    - Kurzanleitung zum Bedienen
    - Installation
    - Ausführliche Beschreibung (Parameter,...)
    - Entwicklung (Wiki?)
- Marcus startet mit Tests


## Abstandsflchen
- GeoOffice?
- Testdatensatz anfertigen
- Implementierung Vorschlag Zehrfeld?


## DataCat
?
