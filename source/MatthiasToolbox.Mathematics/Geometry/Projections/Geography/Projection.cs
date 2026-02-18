using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Datastructures.Geometry;

namespace MatthiasToolbox.Mathematics.Geometry.Projections.Geography
{
    public abstract class Projection
    {
        #region cvar

        // minimum latitude of the bounds of this projection
        protected double minLatitude = -Math.PI / 2;

        // minimum longitude of the bounds of this projection. This is relative to the projection centre.
        protected double minLongitude = -Math.PI;

        // maximum latitude of the bounds of this projection
        protected double maxLatitude = Math.PI / 2;

        // maximum longitude of the bounds of this projection. This is relative to the projection centre.
        protected double maxLongitude = Math.PI;

        // latitude of the centre of projection
        protected double projectionLatitude = 0.0;

        // longitude of the centre of projection
        protected double projectionLongitude = 0.0;

        // standard parallel 1 (for projections which use it)
        protected double projectionLatitude1 = 0.0;

        // standard parallel 2 (for projections which use it)
        protected double projectionLatitude2 = 0.0;

        // projection scale factor
        protected double scaleFactor = 1.0;

        // false easting of this projection
        protected double falseEasting = 0;

        // false northing of this projection
        protected double falseNorthing = 0;

        // latitude of true scale. Only used by specific projections.
        protected double trueScaleLatitude = 0.0;

        // equator radius
        protected double a = 0;

        // eccentricity
        protected double e = 0;

        // eccentricity squared
        protected double es = 0;

        // 1-(eccentricity squared)
        protected double one_es = 0;

        // 1/(1-(eccentricity squared))
        protected double rone_es = 0;

        // ellipsoid used by this projection
        protected Ellipsoid ellipsoid;

        // true if this projection is using a sphere (es == 0)
        protected bool spherical;

        // true if this projection is geocentric
        protected bool geocentric;

        // name of this projection
        protected String name;

        // conversion factor from metres to whatever units the projection uses.
        protected double fromMetres = 1;

        // total scale factor = Earth radius * units
        private double totalScale = 1;

        // falseEasting, adjusted to the appropriate units using fromMetres
        private double totalFalseEasting = 0;

        // falseNorthing, adjusted to the appropriate units using fromMetres
        private double totalFalseNorthing = 0;

        // more constants
        protected const double EPS10 = 1e-10;

        #endregion
        #region prop

        public Ellipsoid Ellipsoid
        {
            get { return this.ellipsoid; }
            set
            {
                this.ellipsoid = value;
                this.a = ellipsoid.EquatorRadius;
                this.e = ellipsoid.Eccentricity;
                this.es = ellipsoid.EccentricitySquared;
            }
        }

        //   /**
        //* Returns true if this projection is conformal
        //*/
        //   public boolean isConformal()
        //   {
        //       return false;
        //   }

        //   /**
        //    * Returns true if this projection is equal area
        //    */
        //   public boolean isEqualArea()
        //   {
        //       return false;
        //   }

        //   /**
        //    * Returns true if this projection has an inverse
        //    */
        //   public boolean hasInverse()
        //   {
        //       return false;
        //   }

        //   /**
        //    * Returns true if lat/long lines form a rectangular grid for this projection
        //    */
        //   public boolean isRectilinear()
        //   {
        //       return false;
        //   }

        //   /**
        //    * Returns true if latitude lines are parallel for this projection
        //    */
        //   public boolean parallelsAreParallel()
        //   {
        //       return isRectilinear();
        //   }

        //   /**
        //    * Set the name of this projection.
        //    */
        //   public void setName(String name)
        //   {
        //       this.name = name;
        //   }

        //   public String getName()
        //   {
        //       if (name != null)
        //           return name;
        //       return toString();
        //   }

        //   public String toString()
        //   {
        //       return "None";
        //   }

        //   /**
        //    * Set the minimum latitude. This is only used for Shape clipping and doesn't affect projection.
        //    */
        //   public void setMinLatitude(double minLatitude)
        //   {
        //       this.minLatitude = minLatitude;
        //   }

        //   public double getMinLatitude()
        //   {
        //       return minLatitude;
        //   }

        //   /**
        //    * Set the maximum latitude. This is only used for Shape clipping and doesn't affect projection.
        //    */
        //   public void setMaxLatitude(double maxLatitude)
        //   {
        //       this.maxLatitude = maxLatitude;
        //   }

        //   public double getMaxLatitude()
        //   {
        //       return maxLatitude;
        //   }

        //   public double getMaxLatitudeDegrees()
        //   {
        //       return maxLatitude * RTD;
        //   }

        //   public double getMinLatitudeDegrees()
        //   {
        //       return minLatitude * RTD;
        //   }

        //   public void setMinLongitude(double minLongitude)
        //   {
        //       this.minLongitude = minLongitude;
        //   }

        //   public double getMinLongitude()
        //   {
        //       return minLongitude;
        //   }

        //   public void setMinLongitudeDegrees(double minLongitude)
        //   {
        //       this.minLongitude = DTR * minLongitude;
        //   }

        //   public double getMinLongitudeDegrees()
        //   {
        //       return minLongitude * RTD;
        //   }

        //   public void setMaxLongitude(double maxLongitude)
        //   {
        //       this.maxLongitude = maxLongitude;
        //   }

        //   public double getMaxLongitude()
        //   {
        //       return maxLongitude;
        //   }

        //   public void setMaxLongitudeDegrees(double maxLongitude)
        //   {
        //       this.maxLongitude = DTR * maxLongitude;
        //   }

        //   public double getMaxLongitudeDegrees()
        //   {
        //       return maxLongitude * RTD;
        //   }

        //   /**
        //    * Set the projection latitude in radians.
        //    */
        //   public void setProjectionLatitude(double projectionLatitude)
        //   {
        //       this.projectionLatitude = projectionLatitude;
        //   }

        //   public double getProjectionLatitude()
        //   {
        //       return projectionLatitude;
        //   }

        //   /**
        //    * Set the projection latitude in degrees.
        //    */
        //   public void setProjectionLatitudeDegrees(double projectionLatitude)
        //   {
        //       this.projectionLatitude = DTR * projectionLatitude;
        //   }

        //   public double getProjectionLatitudeDegrees()
        //   {
        //       return projectionLatitude * RTD;
        //   }

        //   /**
        //    * Set the projection longitude in radians.
        //    */
        //   public void setProjectionLongitude(double projectionLongitude)
        //   {
        //       this.projectionLongitude = normalizeLongitudeRadians(projectionLongitude);
        //   }

        //   public double getProjectionLongitude()
        //   {
        //       return projectionLongitude;
        //   }

        //   /**
        //    * Set the projection longitude in degrees.
        //    */
        //   public void setProjectionLongitudeDegrees(double projectionLongitude)
        //   {
        //       this.projectionLongitude = DTR * projectionLongitude;
        //   }

        //   public double getProjectionLongitudeDegrees()
        //   {
        //       return projectionLongitude * RTD;
        //   }

        //   /**
        //    * Set the latitude of true scale in radians. This is only used by certain projections.
        //    */
        //   public void setTrueScaleLatitude(double trueScaleLatitude)
        //   {
        //       this.trueScaleLatitude = trueScaleLatitude;
        //   }

        //   public double getTrueScaleLatitude()
        //   {
        //       return trueScaleLatitude;
        //   }

        //   /**
        //    * Set the latitude of true scale in degrees. This is only used by certain projections.
        //    */
        //   public void setTrueScaleLatitudeDegrees(double trueScaleLatitude)
        //   {
        //       this.trueScaleLatitude = DTR * trueScaleLatitude;
        //   }

        //   public double getTrueScaleLatitudeDegrees()
        //   {
        //       return trueScaleLatitude * RTD;
        //   }

        //   /**
        //    * Set the projection latitude in radians.
        //    */
        //   public void setProjectionLatitude1(double projectionLatitude1)
        //   {
        //       this.projectionLatitude1 = projectionLatitude1;
        //   }

        //   public double getProjectionLatitude1()
        //   {
        //       return projectionLatitude1;
        //   }

        //   /**
        //    * Set the projection latitude in degrees.
        //    */
        //   public void setProjectionLatitude1Degrees(double projectionLatitude1)
        //   {
        //       this.projectionLatitude1 = DTR * projectionLatitude1;
        //   }

        //   public double getProjectionLatitude1Degrees()
        //   {
        //       return projectionLatitude1 * RTD;
        //   }

        //   /**
        //    * Set the projection latitude in radians.
        //    */
        //   public void setProjectionLatitude2(double projectionLatitude2)
        //   {
        //       this.projectionLatitude2 = projectionLatitude2;
        //   }

        //   public double getProjectionLatitude2()
        //   {
        //       return projectionLatitude2;
        //   }

        //   /**
        //    * Set the projection latitude in degrees.
        //    */
        //   public void setProjectionLatitude2Degrees(double projectionLatitude2)
        //   {
        //       this.projectionLatitude2 = DTR * projectionLatitude2;
        //   }

        //   public double getProjectionLatitude2Degrees()
        //   {
        //       return projectionLatitude2 * RTD;
        //   }

        //   /**
        //    * Set the false Northing in projected units.
        //    */
        //   public void setFalseNorthing(double falseNorthing)
        //   {
        //       this.falseNorthing = falseNorthing;
        //   }

        //   public double getFalseNorthing()
        //   {
        //       return falseNorthing;
        //   }

        //   /**
        //    * Set the false Easting in projected units.
        //    */
        //   public void setFalseEasting(double falseEasting)
        //   {
        //       this.falseEasting = falseEasting;
        //   }

        //   public double getFalseEasting()
        //   {
        //       return falseEasting;
        //   }

        //   /**
        //    * Set the projection scale factor. This is set to 1 by default.
        //    */
        //   public void setScaleFactor(double scaleFactor)
        //   {
        //       this.scaleFactor = scaleFactor;
        //   }

        //   public double getScaleFactor()
        //   {
        //       return scaleFactor;
        //   }

        //   public double getEquatorRadius()
        //   {
        //       return a;
        //   }

        //   /**
        //    * Set the conversion factor from metres to projected units. This is set to 1 by default.
        //    */
        //   public void setFromMetres(double fromMetres)
        //   {
        //       this.fromMetres = fromMetres;
        //   }

        //   public double getFromMetres()
        //   {
        //       return fromMetres;
        //   }

        #endregion
        #region ctor

        protected Projection()
        {
            this.Ellipsoid = Ellipsoid.SPHERE;
        }

        #endregion
        #region init

        /// <summary>
        /// initialize the projection. this should be called after 
        /// setting the parameters and before using the projection.
        /// </summary>
        public void Initialize()
        {
            spherical = e == 0d;
            one_es = 1d - es;
            rone_es = 1d / one_es;
            totalScale = a * fromMetres;
            totalFalseEasting = falseEasting * fromMetres;
            totalFalseNorthing = falseNorthing * fromMetres;
        }

        #endregion
        #region abst

        /// <summary>
        /// the method which actually does the projection
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public abstract Point Project(double x, double y);

        /// <summary>
        /// the method which actually does the inverse projection
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public abstract Point ProjectInverse(double x, double y);

        #endregion
        #region impl

        /// <summary>
        /// project a lat/long point (in degrees), producing a result in metres
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public Point Transform(Point src)
        {
            Point result = new Point();
            double x = src.X * MMath.DTR;
            if (projectionLongitude != 0) x = MMath.NormalizeLongitude(x - projectionLongitude);
            result = Project(x, src.Y * MMath.DTR);
            result.X = totalScale * result.X + totalFalseEasting;
            result.Y = totalScale * result.Y + totalFalseNorthing;
            return result;
        }

        /// <summary>
        /// project a latitude/longitude point, producing a result in metres
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public Point TransformRadians(Point src)
        {
            Point result = new Point();
            double x = src.X;
            if (projectionLongitude != 0) x = MMath.NormalizeLongitude(x - projectionLongitude);
            result = Project(x, src.Y);
            result.X = totalScale * result.X + totalFalseEasting;
            result.Y = totalScale * result.Y + totalFalseNorthing;
            return result;
        }

        /// <summary>
        /// inverse-project a point (in metres), producing a latitude/longitude result in degrees
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public Point TransformInverse(Point src)
        {
            Point result = new Point();
            double x = (src.X - totalFalseEasting) / totalScale;
            double y = (src.Y - totalFalseNorthing) / totalScale;
            result = ProjectInverse(x, y);
            if (result.X < -Math.PI) result.X = -Math.PI;
            else if (result.X > Math.PI) result.X = Math.PI;
            if (projectionLongitude != 0) result.X = MMath.NormalizeLongitude(result.X + projectionLongitude);
            result.X *= MMath.RTD;
            result.Y *= MMath.RTD;
            return result;
        }

        /// <summary>
        /// inverse-project a point (in metres), producing a latitude/longitude result in radians
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public Point TransformInverseRadians(Point src)
        {
            Point result = new Point();
            double x = (src.X - totalFalseEasting) / totalScale;
            double y = (src.Y - totalFalseNorthing) / totalScale;
            result = ProjectInverse(x, y);
            if (result.X < -Math.PI) result.X = -Math.PI;
            else if (result.X > Math.PI) result.X = Math.PI;
            if (projectionLongitude != 0) result.X = MMath.NormalizeLongitude(result.X + projectionLongitude);
            return result;
        }

        // further conversions
        //    
        //    /**
        //     * The method which actually does the inverse projection. This should be overridden for all projections.
        //     */
        //    public Point projectInverse(double x, double y, Point dst)
        //    {
        //        dst.x = x;
        //        dst.y = y;
        //        return dst;
        //    }

        //    /**
        //     * Inverse-project a number of points (in metres), producing a lat/long result in degrees
        //     */
        //    public void inverseTransform(double[] srcPoints, int srcOffset, double[] dstPoints, int dstOffset, int numPoints) {
        //    Point in = new Point();
        //    Point out = new Point();
        //    for (int i = 0; i < numPoints; i++) {
        //        in.x = srcPoints[srcOffset++];
        //        in.y = srcPoints[srcOffset++];
        //        inverseTransform(in, out);
        //        dstPoints[dstOffset++] = out.x;
        //        dstPoints[dstOffset++] = out.y;
        //    }
        //}

        //    /**
        //     * Inverse-project a number of points (in metres), producing a lat/long result in radians
        //     */
        //    public void inverseTransformRadians(double[] srcPoints, int srcOffset, double[] dstPoints, int dstOffset, int numPoints) {
        //    Point in = new Point();
        //    Point out = new Point();
        //    for (int i = 0; i < numPoints; i++) {
        //        in.x = srcPoints[srcOffset++];
        //        in.y = srcPoints[srcOffset++];
        //        inverseTransformRadians(in, out);
        //        dstPoints[dstOffset++] = out.x;
        //        dstPoints[dstOffset++] = out.y;
        //    }
        //}

        //    /**
        //     * Project a number of lat/long points (in degrees), producing a result in metres
        //     */
        //    public void transform(double[] srcPoints, int srcOffset, double[] dstPoints, int dstOffset, int numPoints) {
        //    Point in = new Point();
        //    Point out = new Point();
        //    for (int i = 0; i < numPoints; i++) {
        //        in.x = srcPoints[srcOffset++];
        //        in.y = srcPoints[srcOffset++];
        //        transform(in, out);
        //        dstPoints[dstOffset++] = out.x;
        //        dstPoints[dstOffset++] = out.y;
        //    }
        //}

        //    /**
        //     * Project a number of lat/long points (in radians), producing a result in metres
        //     */
        //    public void transformRadians(double[] srcPoints, int srcOffset, double[] dstPoints, int dstOffset, int numPoints) {
        //    Point in = new Point();
        //    Point out = new Point();
        //    for (int i = 0; i < numPoints; i++) {
        //        in.x = srcPoints[srcOffset++];
        //        in.y = srcPoints[srcOffset++];
        //        transform(in, out);
        //        dstPoints[dstOffset++] = out.x;
        //        dstPoints[dstOffset++] = out.y;
        //    }
        //}

        //    /**
        //     * Finds the smallest lat/long rectangle wholly inside the given view rectangle.
        //     * This is only a rough estimate.
        //     */
        //    public Rectangle2D inverseTransform(Rectangle2D r) {
        //    Point in = new Point();
        //    Point out = new Point();
        //    Rectangle2D bounds = null;
        //    if (isRectilinear()) {
        //        for (int ix = 0; ix < 2; ix++) {
        //            double x = r.getX()+r.getWidth()*ix;
        //            for (int iy = 0; iy < 2; iy++) {
        //                double y = r.getY()+r.getHeight()*iy;
        //                in.x = x;
        //                in.y = y;
        //                inverseTransform(in, out);
        //                if (ix == 0 && iy == 0)
        //                    bounds = new Rectangle2D.Double(out.x, out.y, 0, 0);
        //                else
        //                    bounds.add(out.x, out.y);
        //            }
        //        }
        //    } else {
        //        for (int ix = 0; ix < 7; ix++) {
        //            double x = r.getX()+r.getWidth()*ix/6;
        //            for (int iy = 0; iy < 7; iy++) {
        //                double y = r.getY()+r.getHeight()*iy/6;
        //                in.x = x;
        //                in.y = y;
        //                inverseTransform(in, out);
        //                if (ix == 0 && iy == 0)
        //                    bounds = new Rectangle2D.Double(out.x, out.y, 0, 0);
        //                else
        //                    bounds.add(out.x, out.y);
        //            }
        //        }
        //    }
        //    return bounds;
        //}

        //    /**
        //     * Transform a bounding box. This is only a rough estimate.
        //     */
        //    public Rectangle2D transform(Rectangle2D r) {
        //    Point in = new Point();
        //    Point out = new Point();
        //    Rectangle2D bounds = null;
        //    if ( isRectilinear() ) {
        //        for (int ix = 0; ix < 2; ix++) {
        //            double x = r.getX()+r.getWidth()*ix;
        //            for (int iy = 0; iy < 2; iy++) {
        //                double y = r.getY()+r.getHeight()*iy;
        //                in.x = x;
        //                in.y = y;
        //                transform(in, out);
        //                if (ix == 0 && iy == 0)
        //                    bounds = new Rectangle2D.Double(out.x, out.y, 0, 0);
        //                else
        //                    bounds.add(out.x, out.y);
        //            }
        //        }
        //    } else {
        //        for (int ix = 0; ix < 7; ix++) {
        //            double x = r.getX()+r.getWidth()*ix/6;
        //            for (int iy = 0; iy < 7; iy++) {
        //                double y = r.getY()+r.getHeight()*iy/6;
        //                in.x = x;
        //                in.y = y;
        //                transform(in, out);
        //                if (ix == 0 && iy == 0)
        //                    bounds = new Rectangle2D.Double(out.x, out.y, 0, 0);
        //                else
        //                    bounds.add(out.x, out.y);
        //            }
        //        }
        //    }
        //    return bounds;
        //}

        #endregion
        #region util

        /// <summary>
        /// returns true if the given latitude/longitude point is visible in this projection
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsInside(double x, double y)
        {
            x = NormalizeLongitude((float)(x * MMath.DTR - projectionLongitude));
            return minLongitude <= x && x <= maxLongitude && minLatitude <= y && y <= maxLatitude;
        }

        public static float NormalizeLongitude(float angle)
        {
            if (Double.IsInfinity(angle) || Double.IsNaN(angle)) throw new NotFiniteNumberException("Infinite longitude");
            while (angle > 180) angle -= 360;
            while (angle < -180) angle += 360;
            return angle;
        }

        //   public static double normalizeLongitudeRadians(double angle)
        //   {
        //       if (Double.isInfinite(angle) || Double.isNaN(angle))
        //           throw new IllegalArgumentException("Infinite longitude");
        //       while (angle > Math.PI)
        //           angle -= MapMath.TWOPI;
        //       while (angle < -Math.PI)
        //           angle += MapMath.TWOPI;
        //       return angle;
        //   }

        #endregion
    }
}
