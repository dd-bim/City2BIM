using System;
using System.Collections.Generic;
using System.Text;

namespace BimGisCad.Representation.Geometry.Linear
{
    /// <summary>
    /// Schnittstelle für Solve
    /// </summary>
    public interface ILinear
    {
        /// <summary>
        /// Gleichung für Punktberechnung
        /// </summary>
        double[][] PointEqu { get; }
    }

    /// <summary>
    /// 2D
    /// </summary>
    public interface ILinear2 : ILinear
    {

    }

    /// <summary>
    /// 3D
    /// </summary>
    public interface ILinear3 : ILinear
    {

    }
}
