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

### Site Subregion
- Performance sehr schlecht, wenn mehr als ca. 20 Subregions pro TopographySurface definiert werden!
```C#
	SiteSubRegion.Create()
```
- Noch mehr TopographySurfaces für Flurstücke anlegen und diese in 10er- 20er Pakete erstellen?
  - Nachteil: Noch viel mehr "unnötiges" Gelände in Revit-Dokument
- Anderes Revit-Bauteil verwenden? 

### Materialien
- Für Alkis und XPlanung nach Umstrukturierung keine Materialdefinition mehr

### Surveyors Plan to Revit
?


## IFC-Export

### Umgebungsmodelle Master
- Revit Projektdateien neu aufsetzen? Für verschiedene Rvt Versionen (2019, 2020, 2021)? --> dauert lange
- Export durchführen. Fehler dokumentieren


## Dokumentation
- Berarbeitung nötig :(


## Abstandsflchen
- GeoOffice?
- Testdatensatz anfertigen
- Implementierung Vorschlag Zehrfeld?


## DataCat
?
