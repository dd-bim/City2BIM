#### Download for **Autodesk Revit 2019** under *releases* (see bar above)

Guide / Anleitung als HTML:
[**Link to City2RVT (Plugin for Revit) Guide as HTML**](Doc_City2RVT.html)


Guide / Anleitung als Markdown:
[**Link to City2RVT (Plugin for Revit) Guide as markdown**](Doc_City2RVT.md)


**If you find any bugs please let us know via mail or create an issue at github.**

**Sollten Sie Programmfehler entdecken, schreiben Sie uns bitte eine Mail oder erstellen Sie einen Issue hier auf github.**

----------------------


**Stand (07.01.2020):**

#### Umfang Repo:
- drei VisualStudio-Projekte:

1.) **City2BIM:**
 - Library, zum Einlesen von CityGML-Gebäuden, ALKIS-Flurstücken, ALKIS-Nutzungen, ALKIS-Gebäudeumringen, Raster-DGM
 - Nutzung von XDocument (XML.Linq)
 - Revit - unabhängig
 - XBim (IFC) - unabhängig
 - Abhängigkeiten: Serilog

2.) **City2RVT:**
 - Revit-Plugin für Revit 2019
 - PlugIn zur Übergabe einer Georeferenzierung sowie zur Darstellung von CityGml-Gebäuden, ALKIS-Themen, Raster-DGM
 - Addin-Datei beachten (dient dau Revit von PlugIn in Kenntnis zu setzen)
 - Abhängigkeiten: RevitAPI (proprietär), RevitAPIUI (proprietär), GeographicLib, City2BIM

3.) **City2IFC:**
 - derzeit nur Dummy
 - Aussicht: stand-alone WPF tool zur Konvertierung von CityGml-Gebäuden nach IFC
 - Abhängigkeiten (voraussichtlich): XBim, GeographicLib, City2BIM

---------------------------

### City2BIM

#### Scope

- liest CityGml-Daten über XDocument
- speichert Gebäude und Gebäudeteile in LOD1 und LOD2 intern ab
- interne Objekte enthalten Geometrie und Semantik
- keine Texturen, keine Vegetation, kein Gelände

#### Einleselogik

- auf lokal vorliegender Datei
- bei Stadtmodellen zusätzlich: Abfrage des WFS-Servers von VirtualCitySystems wird bereit gestellt


#### Interne Speicherung

##### Stadtmodelle

- je CityGml-Buildung wird intern ein CityGml_Bldg-Objekt angelegt
- wenn vorhanden werden BuildingParts gespeichert
- enthält jeweils eometrie der Begrenzungsflächen als Liste von CityGml_Surfaces
- optional wird über Ebenenschnitte ein geschlossener Volumenkörper (Solid) berechnet (Punktkoordinaten werden hierfür neu berechnet)
- Semantik und Flächenart wird aus CityGml übernommen und als Key-Value im Objekt gespeichert
- Mapping von Codierungen nach Adv zu lesbaren Eigenschaften wird bereitgestellt

##### Flurstücksdaten

- je ALKIS-Objekt wird ein AX_Objekt angelegt
- enthält Liste der begrenzenden Segmente
- unterscheidet zwischen ALKIS-Themen: Flurstück, Tatsächliche Nutzung, Gebäude
- enthält Semantik für Flurstücke sowie (wenn vorhanden) Daten zum Eigentümer

------------------------------------

### City2RVT

#### Scope

- Gebäude und Gebäudeteile in LOD1 und LOD2 (aus City2BIM) werden nach Revit übertragen
- mit Geometrie und Semantik
- keine Texturen, keine Vegetation, kein Gelände

#### GUI

##### Georeferencing

- UI zur Übergabe einer Georeferenzierung
- Adressübergabe: TO DO, Recherche nach bestem Speicherort in Revit API notwendig
- LatLon für SiteLocation
- Projektion für Projektbasispunkt bzw. Revit-Projektparameter
- Berechnung von LatLon zu UTM-System und vice versa
- Berechnung eines Projektmaßstabes im UTM-System sowie abhängig von der Projekthöhe (wird später bei Import von Geodaten berücksichtigt)
- Berechnung von Grid North zu True North und vice versa

#####  City2BIM

###### Settings

- Server oder File Import
- Server URL
- Umfang der abzufragenden Daten (Umkreis um Koordinate)
- Möglichkeit zur Speicherung der Server-Antwort
- lokale File Location
- Koordinatenreihenfolge
- Codelistübersetzung: ja/nein; Adv oder Sig3D

###### Import als Solids

- Erfolgsquote Solids > 90 %
- Übertragung erfolgt als DirectShape-Element
- Geometrieerzeugung als TesselatedShapeBuilder-Objekt
- Fallback 1 (LOD2 nicht möglich):
-- LOD1-Erzeugung aus Grundfläche und max. Höhe
- Fallback 2 (LOD1 aus Grundfläche nicht möglich): 
-- aus Grundflächenpunkten wird konvexe Hülle als Umring verwendet und als LOD1 extrudiert
- jedes Gebäude bzw. jeder Gebäudeteil wird eigenes Revit-Objekt
- gebäude(teil)spezifische Semantik wird in Form von Parametren (Shared Parameters) übertragen
- Flächensemantik (falls vorhanden) geht verloren
- Kategorisierung der Objekte innerhalb "Umgebung"

###### Import als Surfaces

- Erfolgsquote > 98 %
- Übertragung erfolgt als DirectShape-Element
- Geometrieerzeugung als Extrusion (pseudo-Volumenkörper mit Extrusionshöhe von 1 cm)
- jede Fläche wird eigenes eigenes Revit-Objekt
- flächenspezifische Semantik (inklusive übergeordneter Gebäude-Semantik) wird in Form von Parametren (Shared Parameters) übertragen
- Kategorisierung der Objekte abhängig von Flächenart in Dach, Fundament, Wand, Allgemeines Modell

##### ALKIS2BIM

###### Settings

- lokale File Location
- Koordinatenreihenfolge
- Drapieren eines oder mehrerer ALKIS-Themen auf Gelände (falls vorhanden und per PlugIn vorher importiert)

###### Import

- Berücksichtigung der ALKIS-Themen: Flurstücke, Tatsächliche Nutzungen, Gebäude
- Anlegen von Topographie-Objekten als Referenzflächen je ALKIS-Thema
-- außer Haken für Drapieren wurde gesetzt, dann ist Referenz das TIN
-- 2D-Flächen unterhalb der Projekthöhe
- Import der einzelnen Flächen
-- geometrisch als Liste von CurveLoops mit Außenring und (wenn vorhanden) Innenringen
-- semantisch als Unterregion innerhalb der Referenz-Topographie
- Einfärbung der Flurstücke (grau), Gebäudeumringe (rot), Nutzungen (verschieden je nach Unterthema)

##### DTM2BIM

###### Import

- über FileDialog als .txt- oder .csv-Datei

##### IFC Export

- legt lokal (AppData/Lokal/City2BIM) eine ParameterSet-Datei an, mit welcher gesteuert werden kann, dass das Property Set der Georef-Attribute den standardisierten Namen "ePset_MapConversion" bzw "ePset_ProjectedCRS" bekommt
- der Nutzer bekommt einen Hinweis dahingehend angezeigt
- Info: beim IFC-Export mit dem Revit IFC-Exporter muss diese Text-Datei als Template für ein benutzerefiniertes Property Set importiert werden

- Ausblick:
-- schöner wäre es hier einen eigenen IFC-Export zu implementieren (IFC Exporter ist Open Source und könnte somit für eigene Zwecke angepasst werden, Einarbeitung ist allerdings aufwendig)

### Logging

- via Serilog
- derzeit nur für CityGml-Daten
- gespeichert im lokalen Profil: %LOCALAPPDATA%/City2BIM
- enthält Schrankenwerte
- enthält Statistik (Erfolgsrate)
- enthält Protokoll zu überführten Gebäuden (bei Solids mit Statistik zu Geometrieoperationen)
