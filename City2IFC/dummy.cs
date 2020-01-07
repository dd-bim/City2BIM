namespace City2IFC
{
    /// <summary>
    /// Just for information regarding things to do
    /// </summary>
    class dummy
    {
        // TODO City2IFC implementation

        /*/HINT City2IFC basic information
        Implementation of a stand-alone tool (Revit independent) for converting CityGML (LOD1 / LOD2) to IFC
        Preparations made:
        Separation of import logic and Revit write logic (see independent projects: City2BIM, City2IFC, City2RVT)
        Import logic: see and USE City2BIM logic for import nearly the same way as City2RVT does
        Use each City2BIM.CityGml_Bldg with its geometry and semantic as data for IFC export
        /*/

        /*/HINT City2IFC library hints:        
        use XBIM-library for writing IFC
        look at previous projects which used XBim (IfcHouse, GeoRefChecker, ... does)
        IfcHouse fron Enrico: https://kis5.geoinformation.htw-dresden.de/gitlab/romanschek/ifchouse
        /*/

        /*/HINT City2IFC documentation hints: 
        always use the IFC documentation to know how the entity is structured which you want to use
        IFC doc: https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/
        At the index site you can quickly search for the entity you need
        At each site of the entity I recommend to look at the "Attribute inheritance" (must be unfold) to see ALL attributes of the entity
        Also use the described concepts in the docu (chapter 4) to understand how IFC works and to know what you have to implement for fulfilling standard (or rather MVD´s)
        /*/

        /*/HINT City2IFC geometry transfer (solids)
        Keep in mind: there are two possible ways to transfer geometry (at least in the Revit plugin)
        Decide which way should be transfered (or maybe both ways)
        For solid geometry I recommend the following IFC entities:
        For each solid (which represents the geometry of an GmlBuilding or GmlBuildingPart) create an IFCBUILDINGELEMENTPROXY element
        Attributes:

        1 GlobalId: assign via XBim

        2 OwnerHistory: assign information you want to transfer via XBim (e.g. see: 
        https://kis5.geoinformation.htw-dresden.de/gitlab/goerne/ifcgeoref/blob/master/IfcGeoRefChecker/IfcGeoRefChecker_v3/IfcGeoRefChecker_v3/IO/IfcWriter.cs

        3 Name: assign something specific (maybe BuildingId or BuildingPartId from CitGml)

        4 Decription: assign something specific or leave it empty

        5 ObjectType: dependent of attribute 9: I recommend to use some unambiguous label like "CityGML-Building(part)"

        6 ObjectPlacement: VERY IMPORTANT concept, for this see IfcHouse and inform yourself about coordinate systems in IFC, relative transformations
        look at other IFC-files, look into IfcHouse, talk to Enrico, look into LoGeoRef concept
        simple solution would be: ask for project base point in tool gui and use this coordinate for acquiring global georef regarding LoGeoRef concept,
        for each Proxy element calculate the difference to the project base point and store them into LocalPlacement of Proxy

        7 Representation: also VERY IMPORTANT, decide whcih geometry type you want to implement, I recommend BRep because the coordinates of the planes of the solid
        are stored in the CityGml_Bldg.Solid object, ATTENTION: if you use the relative transformations (you should do that for good BIM, see attribute 6) 
        the coordinates must be relative to the specified local placement, talk to Enrico if you have problems to understand

        8 Tag: assign something specific or leave it empty

        9 PredefinedType: you have to use the enum specified in the IFC docu, I recommend "USERDEFINED", in this case don't forget to set attribute 5  

        /*/

        /*/HINT City2IFC geometry transfer (surfaces)
        For surface transfer an idea would be to transfer the surfaces to its representing IFC types, 
        that would be IfcRoof, IfcWall, IfcSlab, (perhaps IfcVirtualElement), they than could be aggregated into IfcBuilding,
        this approach could be more difficult if you want to fulfill each implementation rule, there is reasearch necessary regarding concepts which
        have to implmented, e.g. a wall must have a relation to a material, also the relative transformations could get more complicated
        However you can also add semantic to them (also the semantic for surfaces, e.g. "Dachneigung") via PropertySets (see next paragraph)

        /*/

        /*/HINT City2IFC semantic transfer
        Use the possiblities of IFC to store user-defined semantic attributes: using PropertySets: IFCPROPERTY in IFCPROPERTYSET via IFCRELDEFINESBYPROPERTIES
        Bind the properties for each Building object to the IfcBuildingElementProxy element via mentioned relation
        Decide for a name for the property set 
        There is a lot of literature for acquirimng properties to objects (no big deal)
        Examples are also at IfcGeoRefChecker (for the map conversion attributes for Ifc2x3 data) and also in the web, 
        e.g.: https://docs.xbim.net/examples/basic-model-operations.html  see under Create
         
        /*/




    }
}
