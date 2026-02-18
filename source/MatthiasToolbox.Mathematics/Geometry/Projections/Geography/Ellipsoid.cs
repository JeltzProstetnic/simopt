using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Mathematics.Geometry.Projections.Geography
{
    /// <summary>
    /// some data taken from the public domain software PROJ (Gerald Evenden)
    /// Note that in the meantime the PROJ software has come under MIT licence.
    /// </summary>
    public class Ellipsoid
    {
        #region cvar

        private string name;
        
        private double poleRadius = 1.0;
        private double equatorRadius = 1.0;
        
        private double eccentricity = 1.0;
        private double eccentricitySquared = 1.0;

        #region stat

        public static Ellipsoid SPHERE = new Ellipsoid("Sphere", 6371008.7714, 6371008.7714);

        public static Ellipsoid AIRY30 = new Ellipsoid("Airy 1830", 6377563.396, 6356256.910);
        public static Ellipsoid AIRYMOD = new Ellipsoid("Modified Airy", 6377340.189, 6356034.446);
        public static Ellipsoid ANDRAE = new Ellipsoid("Andrae 1876 (Denmark, Iceland)", 6377104.43, 300);
        public static Ellipsoid ANSA69 = new Ellipsoid("Australian National & South American 1969", 6378160, 0, 298.25);
        public static Ellipsoid APL65 = new Ellipsoid("Applied Physics 1965", 6378137, 0, 298.25);
        public static Ellipsoid AUSTRALIAN = new Ellipsoid("Australian", 6378160, 6356774.7, 298.25);

        public static Ellipsoid BESSEL41 = new Ellipsoid("Bessel 1841", 6377397.155, 0, 299.1528128);
        public static Ellipsoid BESSNAM = new Ellipsoid("Bessel 1841 (Namibia)", 6377483.865, 0, 299.1528128);

        public static Ellipsoid CLARKE66 = new Ellipsoid("Clarke 1866", 6378206.4, 6356583.8);
        public static Ellipsoid CLARKE80 = new Ellipsoid("Clarke 1880 mod.", 6378249.145, 0, 293.4663);
        public static Ellipsoid CPM99 = new Ellipsoid("Comm. des Poids et Mesures 1799", 6375738.7, 0, 334.29);

        public static Ellipsoid DELMBR = new Ellipsoid("Delambre 1810 (Belgium)", 6376428, 0, 311.5);

        public static Ellipsoid ENGELIS85 = new Ellipsoid("Engelis 1985", 6378136.05, 0, 298.2566);
        public static Ellipsoid EVEREST30 = new Ellipsoid("Everest 1830", 6377276.345, 0, 300.8017);
        public static Ellipsoid EVEREST48 = new Ellipsoid("Everest 1948", 6377304.063, 0, 300.8017);
        public static Ellipsoid EVEREST56 = new Ellipsoid("Everest 1956", 6377301.243, 0, 300.8017);
        public static Ellipsoid EVEREST69 = new Ellipsoid("Everest 1969", 6377295.664, 0, 300.8017);
        public static Ellipsoid EVERESTSS = new Ellipsoid("Everest (Sabah & Sarawak)", 6377298.556, 0, 300.8017);

        public static Ellipsoid FISCHER60 = new Ellipsoid("Fischer (Mercury Datum) 1960", 6378166, 0, 298.3);
        public static Ellipsoid FISCHERMOD = new Ellipsoid("Modified Fischer 1960", 6378155, 0, 298.3);
        public static Ellipsoid FISCHER68 = new Ellipsoid("Fischer 1968", 6378150, 0, 298.3);

        public static Ellipsoid GRS67 = new Ellipsoid("GRS 1967 (IUGG 1967)", 6378160, 0, 298.2471674270);
        public static Ellipsoid GRS80 = new Ellipsoid("GRS 1980 (IUGG, 1980)", 6378137, 0, 298.257222101);

        public static Ellipsoid HELMERT06 = new Ellipsoid("Helmert 1906", 6378200, 0, 298.3);
        public static Ellipsoid HOUGH = new Ellipsoid("Hough", 6378270, 0, 297);

        public static Ellipsoid IAU76 = new Ellipsoid("IAU 1976", 6378140, 0, 298.257);
        public static Ellipsoid INTERNATIONAL09 = new Ellipsoid("International 1909 (Hayford)", 6378388, 0, 297);
        public static Ellipsoid INTERNATIONAL67 = new Ellipsoid("New International 1967", 6378157.5, 6356772.2);

        public static Ellipsoid KAULA61 = new Ellipsoid("Kaula 1961", 6378163, 0, 298.24);
        public static Ellipsoid KRASOVSKY42 = new Ellipsoid("Krassovsky 1942", 6378245, 298.3);

        public static Ellipsoid LERCH79 = new Ellipsoid("Lerch 1979", 6378139, 0, 298.257);

        public static Ellipsoid MAUPERTIUS38 = new Ellipsoid("Maupertius 1738", 6397300, 0, 191);
        public static Ellipsoid MERIT83 = new Ellipsoid("MERIT 1983", 6378137, 0, 298.257);

        public static Ellipsoid NAD27 = new Ellipsoid("NAD27: Clarke 1880 mod.", 6378249.145, 0, 293.4663);
        public static Ellipsoid NAD83 = new Ellipsoid("NAD83: GRS 1980 (IUGG, 1980)", 6378137, 0, 298.257222101);
        public static Ellipsoid NWL65 = new Ellipsoid("Naval Weapons Lab. 1965", 6378145, 298.25);

        public static Ellipsoid PLESSIS17 = new Ellipsoid("Plessis 1817 France", 6376523, 6355863);

        public static Ellipsoid SEASIA = new Ellipsoid("Southeast Asia", 6378155, 6356773.3205);
        public static Ellipsoid SGS85 = new Ellipsoid("Soviet Geodetic System 1985", 6378136, 0, 298.257);

        public static Ellipsoid WALBECK = new Ellipsoid("Walbeck", 6376896, 6355834.8467);
        public static Ellipsoid WGS60 = new Ellipsoid("WGS 1960", 6378165, 0, 298.3);
        public static Ellipsoid WGS66 = new Ellipsoid("WGS 1966", 6378145, 0, 298.25);
        public static Ellipsoid WGS72 = new Ellipsoid("WGS 1972", 6378135, 0, 298.26);
        public static Ellipsoid WGS84 = new Ellipsoid("WGS 1984", 6378137, 0, 298.257223563);

        #endregion

        #endregion
        #region prop

        public string Name { get { return name; } }
        
        public double PoleRadius { get { return poleRadius; } }
        public double EquatorRadius { get { return equatorRadius; } }
        
        public double Eccentricity { get { return eccentricity; } }
        public double EccentricitySquared { get { return eccentricitySquared; } }

        #endregion
        #region ctor

        public Ellipsoid() { }

        /// <summary>
        /// CAUTION: if a reciprocalFlattening != 0 is given, the poleRadius will be re-calculated
        /// </summary>
        /// <param name="name"></param>
        /// <param name="equatorRadius"></param>
        /// <param name="poleRadius"></param>
        /// <param name="reciprocalFlattening"></param>
        public Ellipsoid(string name, double equatorRadius = 6371008.7714, double poleRadius = 6371008.7714, double reciprocalFlattening = 0)
        {
            this.equatorRadius = equatorRadius;
            this.poleRadius = poleRadius;
            this.name = name;
            
            if (reciprocalFlattening != 0)
            {
                double flattening = 1.0 / reciprocalFlattening;
                double f = flattening;
                eccentricitySquared = 2 * f - f * f;
                poleRadius = equatorRadius * Math.Sqrt(1.0 - eccentricitySquared);
            }
            else
            {
                eccentricitySquared = 1.0 - (poleRadius * poleRadius) / (equatorRadius * equatorRadius);
            }

            eccentricity = Math.Sqrt(eccentricitySquared);
        }

        public Ellipsoid(String name, double equatorRadius, double eccentricitySquared)
        {
            this.name = name;
            this.equatorRadius = equatorRadius;

            this.eccentricitySquared = eccentricitySquared;
            poleRadius = equatorRadius * Math.Sqrt(1.0 - eccentricitySquared);
            eccentricity = Math.Sqrt(eccentricitySquared);
        }

        #endregion
    }
}
