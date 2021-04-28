#### Download for **Autodesk Revit 2019** under *releases* (see bar above)

Guide:
[**Link to City2RVT (Plugin for Revit) Guide as HTML**](Doc_City2RVT.html)


Guide:
[**Link to City2RVT (Plugin for Revit) Guide as markdown**](Doc_City2RVT.md)


**If you find any bugs please let us know via mail or create an issue at github.**

----------------------


**Status (07th January 2020):**

#### scope repo:
- Three VisualStudio projects:

1.) **City2BIM:**
 - Library, to read in CityGML buildings, ALKIS parcels, ALKIS land uses, ALKIS building surrounds, raster DTM.
 - Use of XDocument (XML.Linq)
 - Revit - independent
 - XBim (IFC) - independent
 - Dependencies: Serilog

2.) **City2RVT:**
 - Revit plugin for Revit 2019
 - PlugIn for the transfer of a georeferencing as well as for the representation of CityGml buildings, ALKIS themes, raster-DTM.
 - Note addin file (used to notify Revit of PlugIn).
 - Dependencies: RevitAPI (proprietary), RevitAPIUI (proprietary), GeographicLib, City2BIM.

3.) **City2IFC:**
 - currently only dummy
 - prospect: stand-alone WPF tool to convert CityGml buildings to IFC
 - dependencies (expected): XBim, GeographicLib, City2BIM

---------------------------

### City2BIM


#### Scope

- Reads CityGml data via XDocument
- stores buildings and building parts in LOD1 and LOD2 internally
- internal objects contain geometry and semantics
- no textures, no vegetation, no terrain

#### reading logic

- on locally available file
- for city models additionally: query of the WFS server of VirtualCitySystems is provided


#### Internal storage

##### City models

- for each CityGml-building a CityGml_Bldg-object is created internally
- if available BuildingParts are stored
- contains geometry of bounding surfaces as a list of CityGml_Surfaces
- optionally a closed solid is calculated by plane intersections (point coordinates are recalculated for this)
- semantics and surface type are taken from CityGml and stored as key-value in the object
- Mapping of encodings according to Adv to readable properties is provided

##### Parcel data

- one AX_Object is created for each ALKIS object
- contains list of bounding segments
- distinguishes between ALKIS topics: parcel, actual use, building
- contains semantics for parcels and (if available) owner data

------------------------------------


### City2RVT

#### Scope

- Buildings and building parts in LOD1 and LOD2 (from City2BIM) are transferred to Revit
- with geometry and semantics
- no textures, no vegetation, no terrain

#### GUI

##### Georeferencing

- UI for the transfer of georeferencing
- address transfer: TO DO, research for best save location in Revit API necessary
- LatLon for SiteLocation
- Projection for project base point or Revit project parameter
- Calculation from LatLon to UTM system and vice versa
- Calculation of a project scale in the UTM system as well as depending on the project height (will be considered later when importing geodata)
- Calculation of Grid North to True North and vice versa

##### City2BIM

###### Settings

- Server or File Import
- Server URL
- Scope of data to be queried (radius around coordinate)
- Possibility to save the server response
- local file location
- coordinate order
- codelist translation: yes/no; Adv or Sig3D

###### Import as solids

- success rate solids > 90%
- Transfer as DirectShape-element
- Geometry generation as TesselatedShapeBuilder object
- Fallback 1 (LOD2 not possible):
-- LOD1 generation from base area and max. height
- Fallback 2 (LOD1 from base area not possible): 
-- from base area points convex hull is used as perimeter and extruded as LOD1
- each building or part of a building becomes an own Revit object
- building (part) specific semantics is transferred in form of parameters (Shared Parameters)
- area semantics (if available) are lost
- Categorization of objects within "environment

###### Import as Surfaces

- Success rate > 98%
- Transfer as DirectShape element
- Geometry generation as extrusion (pseudo solid with extrusion height of 1 cm)
- each surface becomes its own Revit object
- surface-specific semantics (including higher-level building semantics) is transferred in the form of parameters (shared parameters)
- Categorization of objects depending on surface type in roof, foundation, wall, general model

##### ALKIS2BIM

###### Settings

- local file location
- Coordinate sequence
- Drape one or more ALKIS themes on terrain (if available and imported by PlugIn before)

###### Import

- Consideration of ALKIS topics: Parcels, Actual Land Uses, Buildings
- Creation of topography objects as reference areas per ALKIS topic
-- unless check mark for Drape was set, then reference is the TIN
-- 2D surfaces below the project height
- Import of the individual surfaces
-- geometrically as list of CurveLoops with outer ring and (if available) inner rings
-- semantically as subregion within the reference topography
- Coloring of parcels (gray), building perimeters (red), land uses (different depending on subtopic).

##### DTM2BIM

###### Import

- via FileDialog as .txt or .csv file

##### IFC Export

- creates a ParameterSet file locally (AppData/Local/City2BIM), which can be used to control that the property set of the Georef attributes gets the standardized name "ePset_MapConversion" or "ePset_ProjectedCRS".
- the user gets a hint to this effect
- Info: when exporting IFC with the Revit IFC-Exporter, this text file must be imported as a template for a user-defined property set.

- Outlook:
-- it would be nicer to implement an own IFC export here (IFC Exporter is open source and could therefore be adapted for own purposes, however, familiarization is time-consuming)

### Logging

- via Serilog
- currently only for CityGml data
- stored in local profile: AppData/Local/City2BIM
- contains barrier values
- contains statistics (success rate)
- contains log of transferred buildings (for solids with statistics on geometry operations)
