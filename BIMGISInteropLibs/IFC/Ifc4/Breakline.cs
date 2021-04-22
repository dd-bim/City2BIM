using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc;

using BimGisCad.Representation.Geometry.Elementary;

namespace BIMGISInteropLibs.IFC.Ifc4
{
    public class Breakline
    {
        /* TODO match into new structure
        public static IfcPolyline Create(IfcStore model, Vector3 origin, Dictionary<int, Line3> Breaklines)
        {
            double stX, stY, stZ;
            double npX, npY, npZ;
            double chX, chY, chZ;
            var bl = Breaklines.ElementAt(0);

            for (int i = 0; i <= Breaklines.Count; i++)
            {
                int index = 0;
                bool polyclosed = false;

                using (var txn = model.BeginTransaction("Create Polyline (Breakline)"))
                {
                    var polypl = model.Instances.New<IfcPolyline>(cp =>
                    {
                        var startPoint = model.Instances.New<IfcCartesianPoint>();
                        do
                        {
                            try
                            {
                                bl = Breaklines.ElementAt(i);
                            }
                            catch
                            {
                                polyclosed = true;
                            }
                            if (index > 0 && polyclosed == false)
                            {
                                var blb = Breaklines.ElementAt(i - 1); //vorherige Linie aufrufen

                                //Endpunkt der vorherigen Linie aufrufen
                                chX = blb.Value.Direction.X - origin.X;
                                chY = blb.Value.Direction.Y - origin.Y;
                                chZ = blb.Value.Direction.Z - origin.Z;
                                chX = Math.Round(chX, 3);
                                chY = Math.Round(chY, 3);
                                chZ = Math.Round(chZ, 3);

                                //Startpunkt der aktuellen Linie
                                npX = bl.Value.Position.X - origin.X;
                                npY = bl.Value.Position.Y - origin.Y;
                                npZ = bl.Value.Position.Z - origin.Z;
                                npX = Math.Round(npX, 3);
                                npY = Math.Round(npY, 3);
                                npZ = Math.Round(npZ, 3);

                                //Abgleich zwischen Endpunkt "Linie (n-1)" und Startpunkt "Linie n"
                                //somit gehören diese Linien zusammen
                                if (chX == npX && chY == npY && chZ == npZ)
                                {
                                    //Endpunkt der Linie n
                                    npX = bl.Value.Direction.X - origin.X;
                                    npY = bl.Value.Direction.Y - origin.Y;
                                    npZ = bl.Value.Direction.Z - origin.Z;
                                    npX = Math.Round(npX, 3);
                                    npY = Math.Round(npY, 3);
                                    npZ = Math.Round(npZ, 3);



                                    //Abgleich, ob Startpunkt des Polygons und aktueller Endpunkt identisch sind?
                                    if (npX == startPoint.X && npY == startPoint.Y && npZ == startPoint.Z)
                                    {
                                        //Startpunkt als Endpunkt setzten!
                                        cp.Points.Add(startPoint);

                                        //Polyline schließen --> Polygon ist somit geschlossen
                                        polyclosed = true;

                                        //Eigenschaft für IFC-Export???
                                    }
                                    else
                                    {
                                        var endPoint = model.Instances.New<IfcCartesianPoint>();
                                        endPoint.SetXYZ(npX, npY, npZ);
                                        cp.Points.Add(endPoint);
                                        polyclosed = false;
                                        i++; //weiterzählen, da Linie abgeschlossen ist
                                    }
                                }
                                else
                                {
                                    polyclosed = true; //Polyline schließen
                                    i--;//herunterzählen, da hier kein Punkt hinzugefügt werden konnte
                                }

                            }
                            else if (index == 0 && polyclosed == false) //erste Linie im Polygon
                            {
                                //Anfangspunkt (P1)
                                stX = bl.Value.Position.X - origin.X;
                                stY = bl.Value.Position.Y - origin.Y;
                                stZ = bl.Value.Position.Z - origin.Z;
                                stX = Math.Round(stX, 3);
                                stY = Math.Round(stY, 3);
                                stZ = Math.Round(stZ, 3);


                                startPoint.SetXYZ(stX, stY, stZ);
                                //polyline.Points.Add(startPoint);
                                cp.Points.Add(startPoint);

                                //nächster Punkt (P2)
                                npX = bl.Value.Direction.X - origin.X;
                                npY = bl.Value.Direction.Y - origin.Y;
                                npZ = bl.Value.Direction.Z - origin.Z;
                                npX = Math.Round(npX, 3);
                                npY = Math.Round(npY, 3);
                                npZ = Math.Round(npZ, 3);

                                var nextPoint = model.Instances.New<IfcCartesianPoint>();
                                //Punkt P2 setzen
                                nextPoint.SetXYZ(npX, npY, npZ);

                                //Punkt der aktuellen Polyline übergeben
                                cp.Points.Add(nextPoint);

                                //Status auf nicht geschlossen, da eventuell ein weiterer Punkt hinzugefügt werden kann
                                polyclosed = false;
                                i++;
                            }
                            index++;

                        } while (polyclosed == false);
                    });

                    txn.Commit();
                    return polypl;
                }
            }
        }*/
    }
}
