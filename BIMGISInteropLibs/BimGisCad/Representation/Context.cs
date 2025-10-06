using System;
using System.Collections.Generic;
using System.Text;
using BimGisCad.Representation.Geometry;

namespace BimGisCad.Representation
{
    /// <summary>
    /// Übergeordneter Rahmen 
    /// </summary>
    public class Context
    {
        private double precision;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="placement"></param>
        /// <param name="precision"></param>
        public Context(Axis2Placement3D placement, double precision = 1.0e-6)
        {
             this.Placement = placement;
            this.Precision = precision;
            this.PrecisionSquared = precision * precision;
        }


        /// <summary>
        /// Übergeordnetes "Vermessungs"-Koordinatensystem
        /// </summary>
        public Axis2Placement3D Placement { get; set; }

        /// <summary>
        /// minimaler Streckenunterschied 
        /// </summary>
        public double Precision
        {
            get
            {
                return this.precision;
            }
            set
            {
                this.PrecisionSquared = value * value;
                this.precision = value;
            }
        }

        /// <summary>
        /// minimaler Streckenunterschied 
        /// </summary>
        public double PrecisionSquared { get; private set; }
    }
}
