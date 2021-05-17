using System;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Collections;
using BimGisCad.Representation;

namespace BimGisCad
{
    /// <summary>
    /// Mögliche Projekttypen
    /// </summary>
    public enum ProjectType
    {
        /// <summary>
        /// BIM Projekt (z.B. IFC basiert)
        /// </summary>
        BIM,
        /// <summary>
        /// GIS Projekt (z.B. GML basiert)
        /// </summary>
        GIS,
        /// <summary>
        /// CAD Projekt (z.B. DXF basiert)
        /// </summary>
        CAD
    }

    /// <summary>
    /// Definiert ein Projekt (in der Regel eine Datei)
    /// </summary>
    public class Project : Root
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        /// <param name="type"></param>
        /// <param name="uri"></param>
        /// <param name="objects"></param>
        /// <param name="relations"></param>
        /// <param name="representationContexts"></param>
        public Project(string id, string name, UniqueCollection<Attribute> attributes, 
            ProjectType type, Uri uri, 
            UniqueCollection<Object> objects = null, 
            UniqueCollection<RelationShip> relations = null, 
            HashSet<Context> representationContexts = null)
            : base(id, name, attributes)
        {
            this.Type = type;
            this.Uri = uri;
            this.Objects = objects ?? UniqueCollection<Object>.Create();
            this.Relations = relations ?? UniqueCollection<RelationShip>.Create();
            this.RepresentationContexts = representationContexts ?? new HashSet<Context>();
        }

        /// <summary>
        /// Projekttyp
        /// </summary>
        public ProjectType Type { get; }

        /// <summary>
        /// Datei oder URL Adresse der Originaldatei
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Zugehörige Objekte (Modell)
        /// </summary>
        public UniqueCollection<Object> Objects { get; }

        /// <summary>
        /// Zugehörige Beziehungen
        /// </summary>
        public UniqueCollection<RelationShip> Relations { get; }

        /// <summary>
        /// Alle Darstellungen
        /// </summary>
        public HashSet<Context> RepresentationContexts { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="attributes"></param>
        /// <param name="type"></param>
        /// <param name="uri"></param>
        /// <param name="objects"></param>
        /// <param name="relations"></param>
        /// <param name="representationContexts"></param>
        /// <returns></returns>
        public static Project Create(string id, string name, UniqueCollection<Attribute> attributes,
            ProjectType type, Uri uri, UniqueCollection<Object> objects,
            UniqueCollection<RelationShip> relations, HashSet<Context> representationContexts) => new Project(id, name, attributes, type, uri, objects, relations, representationContexts);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Project Create(string name, ProjectType type, Uri uri) => new Project(null, name, null, type, uri, null, null, null);
    }
}
