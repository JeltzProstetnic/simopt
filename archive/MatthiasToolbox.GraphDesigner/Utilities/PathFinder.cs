using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MatthiasToolbox.GraphDesigner.Controls;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.Logging;
using MatthiasToolbox.GraphDesigner.Enumerations;
using System.Windows.Controls;

namespace MatthiasToolbox.GraphDesigner.Utilities
{
    /// <summary>
    ///  Helper class to provide an orthogonal routing algorithm - connection path
    /// </summary>
    internal class PathFinder
    {
        #region cvar

        /// <summary>
        /// Gap at the end and the beginning of a line
        /// </summary>
        private const double MARGIN_PATH = 15;

        /// <summary>
        /// Margin to the connection line.
        /// </summary>
        private const int MARGIN = 0;

        private const double LENGTH_REFLEXIVE = 25;

        /// <summary>
        /// Border of the rectangles.
        /// </summary>
        private const double BORDER = 15;

        private const int ARROW_SIZE = 15;
        private const double ARROW_ANGLE = 0.3;

        #endregion
        #region routing
        #region orthogonal routing

        private static PathFigureCollection GetOrthogonalConnectionLine(ConnectionInfo source, ConnectionInfo sink, bool showLastLine)
        {
            List<Point> linePoints = new List<Point>();

            Rect rectSource = GetRectWithMargin(source, MARGIN);
            Rect rectSink = GetRectWithMargin(sink, MARGIN);

            //Point startPoint = GetOffsetPoint(source, rectSource); //dwi: 21.07.2011 creates a margin at the endpoint
            //Point endPoint = GetOffsetPoint(sink, rectSink);
            Point startPoint = source.Position;
            Point endPoint = sink.Position;


            linePoints.Add(startPoint);
            Point currentPoint = startPoint;

            if (!rectSink.Contains(currentPoint) && !rectSource.Contains(endPoint))
            {
                while (true)
                {
                    #region source node

                    if (IsPointVisible(currentPoint, endPoint, new Rect[] { rectSource, rectSink }))
                    {
                        linePoints.Add(endPoint);
                        currentPoint = endPoint;
                        break;
                    }

                    Point neighbour = GetNearestVisibleNeighborSink(currentPoint, endPoint, sink, rectSource, rectSink);
                    if (!double.IsNaN(neighbour.X))
                    {
                        linePoints.Add(neighbour);
                        linePoints.Add(endPoint);
                        currentPoint = endPoint;
                        break;
                    }

                    if (currentPoint == startPoint)
                    {
                        bool flag;
                        Point n = GetNearestNeighborSource(source, endPoint, rectSource, rectSink, out flag);
                        linePoints.Add(n);
                        currentPoint = n;

                        if (!IsRectVisible(currentPoint, rectSink, new Rect[] { rectSource }))
                        {
                            Point n1, n2;
                            GetOppositeCorners(source.Orientation, rectSource, out n1, out n2);
                            if (flag)
                            {
                                linePoints.Add(n1);
                                currentPoint = n1;
                            }
                            else
                            {
                                linePoints.Add(n2);
                                currentPoint = n2;
                            }
                            if (!IsRectVisible(currentPoint, rectSink, new Rect[] { rectSource }))
                            {
                                if (flag)
                                {
                                    linePoints.Add(n2);
                                    currentPoint = n2;
                                }
                                else
                                {
                                    linePoints.Add(n1);
                                    currentPoint = n1;
                                }
                            }
                        }
                    }
                    #endregion

                    #region sink node

                    else // from here on we jump to the sink node
                    {
                        Point n1, n2; // neighbour corner
                        Point s1, s2; // opposite corner
                        GetNeighborCorners(sink.Orientation, rectSink, out s1, out s2);
                        GetOppositeCorners(sink.Orientation, rectSink, out n1, out n2);

                        bool n1Visible = IsPointVisible(currentPoint, n1, new Rect[] { rectSource, rectSink });
                        bool n2Visible = IsPointVisible(currentPoint, n2, new Rect[] { rectSource, rectSink });

                        if (n1Visible && n2Visible)
                        {
                            if (rectSource.Contains(n1))
                            {
                                linePoints.Add(n2);
                                if (rectSource.Contains(s2))
                                {
                                    linePoints.Add(n1);
                                    linePoints.Add(s1);
                                }
                                else
                                    linePoints.Add(s2);

                                linePoints.Add(endPoint);
                                currentPoint = endPoint;
                                break;
                            }

                            if (rectSource.Contains(n2))
                            {
                                linePoints.Add(n1);
                                if (rectSource.Contains(s1))
                                {
                                    linePoints.Add(n2);
                                    linePoints.Add(s2);
                                }
                                else
                                    linePoints.Add(s1);

                                linePoints.Add(endPoint);
                                currentPoint = endPoint;
                                break;
                            }

                            if ((Distance(n1, endPoint) <= Distance(n2, endPoint)))
                            {
                                linePoints.Add(n1);
                                if (rectSource.Contains(s1))
                                {
                                    linePoints.Add(n2);
                                    linePoints.Add(s2);
                                }
                                else
                                    linePoints.Add(s1);
                                linePoints.Add(endPoint);
                                currentPoint = endPoint;
                                break;
                            }
                            else
                            {
                                linePoints.Add(n2);
                                if (rectSource.Contains(s2))
                                {
                                    linePoints.Add(n1);
                                    linePoints.Add(s1);
                                }
                                else
                                    linePoints.Add(s2);
                                linePoints.Add(endPoint);
                                currentPoint = endPoint;
                                break;
                            }
                        }
                        else if (n1Visible)
                        {
                            linePoints.Add(n1);
                            if (rectSource.Contains(s1))
                            {
                                linePoints.Add(n2);
                                linePoints.Add(s2);
                            }
                            else
                                linePoints.Add(s1);
                            linePoints.Add(endPoint);
                            currentPoint = endPoint;
                            break;
                        }
                        else
                        {
                            linePoints.Add(n2);
                            if (rectSource.Contains(s2))
                            {
                                linePoints.Add(n1);
                                linePoints.Add(s1);
                            }
                            else
                                linePoints.Add(s2);
                            linePoints.Add(endPoint);
                            currentPoint = endPoint;
                            break;
                        }
                    }
                    #endregion
                }
            }
            else
            {
                linePoints.Add(endPoint);
            }

            linePoints = OptimizeLinePoints(linePoints, new Rect[] { rectSource, rectSink }, source.Orientation, sink.Orientation);

            //CheckPathEnd(source, sink, showLastLine, linePoints); //dwi: 21.07.2011 creates a margin at the endpoint

            //fill PathFigureCollection
            PathFigureCollection pfc = new PathFigureCollection();

            //add source arrows
            switch (source.Cap)
            {
                case ArrowSymbol.Arrow:
                    Vector v1 = linePoints[1] - linePoints[0];
                    v1 = v1 / v1.Length * ARROW_SIZE;
                    Vector n1 = new Vector(-v1.Y, v1.X) * ARROW_ANGLE;

                    pfc.Add(new PathFigure(linePoints[0], new PathSegment[]
                    {
                        new LineSegment(linePoints[0] + v1 - n1, true), 
                        new LineSegment(linePoints[0] + v1 + n1, true)
                    },
                    true));
                    break;
            }

            if (linePoints.Count > 0)
            {
                PathFigure figure = new PathFigure();
                figure.StartPoint = linePoints[0];
                linePoints.Remove(linePoints[0]);
                figure.Segments.Add(new PolyLineSegment(linePoints, true));
                pfc.Add(figure);
            }

            //add target arrows

            switch (sink.Cap)
            {
                case ArrowSymbol.Arrow:
                    if (linePoints.Count < 2)
                        break;
                    Point lastPoint = linePoints[linePoints.Count - 1];
                    Point prePoint = linePoints[linePoints.Count - 2];

                    Vector v2 = prePoint - lastPoint;
                    v2 = v2 / v2.Length * ARROW_SIZE;
                    Vector n2 = new Vector(-v2.Y, v2.X) * ARROW_ANGLE;

                    //add the arrow
                    pfc.Add(new PathFigure(lastPoint, new PathSegment[]
                    {
                        new LineSegment(lastPoint + v2 - n2, true), 
                        new LineSegment(lastPoint + v2 + n2, true)
                    },
                            true));
                    break;
                default:
                    break;
            }

            return pfc;
        }

        /// <summary>
        /// Gets the orthogonal connection line before the Connection object was created..
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sinkPoint">The sink point.</param>
        /// <param name="preferredOrientation">The preferred orientation.</param>
        /// <returns></returns>
        private static List<Point> GetOrthogonalConnectionLine(ConnectionInfo source, Point sinkPoint, ConnectorOrientation preferredOrientation)
        {
            List<Point> linePoints = new List<Point>();
            Rect rectSource = GetRectWithMargin(source, 10);
            Point startPoint = GetOffsetPoint(source, rectSource);
            Point endPoint = sinkPoint;

            linePoints.Add(startPoint);
            Point currentPoint = startPoint;

            if (!rectSource.Contains(endPoint))
            {
                while (true)
                {
                    if (IsPointVisible(currentPoint, endPoint, new Rect[] { rectSource }))
                    {
                        linePoints.Add(endPoint);
                        break;
                    }

                    bool sideFlag;
                    Point n = GetNearestNeighborSource(source, endPoint, rectSource, out sideFlag);
                    linePoints.Add(n);
                    currentPoint = n;

                    if (IsPointVisible(currentPoint, endPoint, new Rect[] { rectSource }))
                    {
                        linePoints.Add(endPoint);
                        break;
                    }
                    else
                    {
                        Point n1, n2;
                        GetOppositeCorners(source.Orientation, rectSource, out n1, out n2);
                        linePoints.Add(sideFlag ? n1 : n2);

                        linePoints.Add(endPoint);
                        break;
                    }
                }
            }
            else
            {
                linePoints.Add(endPoint);
            }

            if (preferredOrientation != ConnectorOrientation.None)
                linePoints = OptimizeLinePoints(linePoints, new Rect[] { rectSource }, source.Orientation, preferredOrientation);
            else
                linePoints = OptimizeLinePoints(linePoints, new Rect[] { rectSource }, source.Orientation, GetOpositeOrientation(source.Orientation));

            return linePoints;
        }

        private static List<Point> OptimizeLinePoints(List<Point> linePoints, Rect[] rectangles, ConnectorOrientation sourceOrientation, ConnectorOrientation sinkOrientation)
        {
            List<Point> points = new List<Point>();
            int cut = 0;

            for (int i = 0; i < linePoints.Count; i++)
            {
                if (i >= cut)
                {
                    for (int k = linePoints.Count - 1; k > i; k--)
                    {
                        if (IsPointVisible(linePoints[i], linePoints[k], rectangles))
                        {
                            cut = k;
                            break;
                        }
                    }
                    points.Add(linePoints[i]);
                }
            }

            #region Line
            for (int j = 0; j < points.Count - 1; j++)
            {
                if (points[j].X != points[j + 1].X && points[j].Y != points[j + 1].Y)
                {
                    ConnectorOrientation orientationFrom;
                    ConnectorOrientation orientationTo;

                    // orientation from point
                    orientationFrom = j == 0 ? sourceOrientation : GetOrientation(points[j], points[j - 1]);

                    // orientation to pint 
                    if (j == points.Count - 2)
                        orientationTo = sinkOrientation;
                    else
                        orientationTo = GetOrientation(points[j + 1], points[j + 2]);


                    if ((orientationFrom == ConnectorOrientation.Left || orientationFrom == ConnectorOrientation.Right) &&
                        (orientationTo == ConnectorOrientation.Left || orientationTo == ConnectorOrientation.Right))
                    {
                        double centerX = Math.Min(points[j].X, points[j + 1].X) + Math.Abs(points[j].X - points[j + 1].X) / 2;
                        points.Insert(j + 1, new Point(centerX, points[j].Y));
                        points.Insert(j + 2, new Point(centerX, points[j + 2].Y));
                        if (points.Count - 1 > j + 3)
                            points.RemoveAt(j + 3);
                        return points;
                    }

                    if ((orientationFrom == ConnectorOrientation.Top || orientationFrom == ConnectorOrientation.Bottom) &&
                        (orientationTo == ConnectorOrientation.Top || orientationTo == ConnectorOrientation.Bottom))
                    {
                        double centerY = Math.Min(points[j].Y, points[j + 1].Y) + Math.Abs(points[j].Y - points[j + 1].Y) / 2;
                        points.Insert(j + 1, new Point(points[j].X, centerY));
                        points.Insert(j + 2, new Point(points[j + 2].X, centerY));
                        if (points.Count - 1 > j + 3)
                            points.RemoveAt(j + 3);
                        return points;
                    }

                    if ((orientationFrom == ConnectorOrientation.Left || orientationFrom == ConnectorOrientation.Right) &&
                        (orientationTo == ConnectorOrientation.Top || orientationTo == ConnectorOrientation.Bottom))
                    {
                        points.Insert(j + 1, new Point(points[j + 1].X, points[j].Y));
                        return points;
                    }

                    if ((orientationFrom == ConnectorOrientation.Top || orientationFrom == ConnectorOrientation.Bottom) &&
                        (orientationTo == ConnectorOrientation.Left || orientationTo == ConnectorOrientation.Right))
                    {
                        points.Insert(j + 1, new Point(points[j].X, points[j + 1].Y));
                        return points;
                    }
                }
            }
            #endregion

            return points;
        }

        private static Point GetNearestNeighborSource(ConnectionInfo source, Point endPoint, Rect rectSource, Rect rectSink, out bool flag)
        {
            Point n1, n2; // neighbors
            GetNeighborCorners(source.Orientation, rectSource, out n1, out n2);

            if (rectSink.Contains(n1))
            {
                flag = false;
                return n2;
            }

            if (rectSink.Contains(n2))
            {
                flag = true;
                return n1;
            }

            if ((Distance(n1, endPoint) <= Distance(n2, endPoint)))
            {
                flag = true;
                return n1;
            }
            else
            {
                flag = false;
                return n2;
            }
        }

        private static Point GetNearestNeighborSource(ConnectionInfo source, Point endPoint, Rect rectSource, out bool flag)
        {
            Point n1, n2; // neighbors
            GetNeighborCorners(source.Orientation, rectSource, out n1, out n2);

            if ((Distance(n1, endPoint) <= Distance(n2, endPoint)))
            {
                flag = true;
                return n1;
            }
            else
            {
                flag = false;
                return n2;
            }
        }

        private static Point GetNearestVisibleNeighborSink(Point currentPoint, Point endPoint, ConnectionInfo sink, Rect rectSource, Rect rectSink)
        {
            Point s1, s2; // neighbors on sink side
            GetNeighborCorners(sink.Orientation, rectSink, out s1, out s2);

            bool flag1 = IsPointVisible(currentPoint, s1, new Rect[] { rectSource, rectSink });
            bool flag2 = IsPointVisible(currentPoint, s2, new Rect[] { rectSource, rectSink });

            if (flag1) // s1 visible
            {
                if (flag2) // s1 and s2 visible
                {
                    if (rectSink.Contains(s1))
                        return s2;

                    if (rectSink.Contains(s2))
                        return s1;

                    if ((Distance(s1, endPoint) <= Distance(s2, endPoint)))
                        return s1;
                    else
                        return s2;

                }
                else
                {
                    return s1;
                }
            }
            else // s1 not visible
            {
                if (flag2) // only s2 visible
                {
                    return s2;
                }
                else // s1 and s2 not visible
                {
                    return new Point(double.NaN, double.NaN);
                }
            }
        }

        private static bool IsPointVisible(Point fromPoint, Point targetPoint, Rect[] rectangles)
        {
            foreach (Rect rect in rectangles)
            {
                if (RectangleIntersectsLine(rect, fromPoint, targetPoint))
                    return false;
            }
            return true;
        }

        private static bool IsRectVisible(Point fromPoint, Rect targetRect, Rect[] rectangles)
        {
            if (IsPointVisible(fromPoint, targetRect.TopLeft, rectangles))
                return true;

            if (IsPointVisible(fromPoint, targetRect.TopRight, rectangles))
                return true;

            if (IsPointVisible(fromPoint, targetRect.BottomLeft, rectangles))
                return true;

            if (IsPointVisible(fromPoint, targetRect.BottomRight, rectangles))
                return true;

            return false;
        }

        private static bool RectangleIntersectsLine(Rect rect, Point startPoint, Point endPoint)
        {
            rect.Inflate(-1, -1);
            return rect.IntersectsWith(new Rect(startPoint, endPoint));
        }

        private static void GetOppositeCorners(ConnectorOrientation orientation, Rect rect, out Point n1, out Point n2)
        {
            switch (orientation)
            {
                case ConnectorOrientation.Left:
                    n1 = rect.TopRight; n2 = rect.BottomRight;
                    break;
                case ConnectorOrientation.Top:
                    n1 = rect.BottomLeft; n2 = rect.BottomRight;
                    break;
                case ConnectorOrientation.Right:
                    n1 = rect.TopLeft; n2 = rect.BottomLeft;
                    break;
                case ConnectorOrientation.Bottom:
                    n1 = rect.TopLeft; n2 = rect.TopRight;
                    break;
                default:
                    throw new Exception("No opposite corners found!");
            }
        }

        private static void GetNeighborCorners(ConnectorOrientation orientation, Rect rect, out Point n1, out Point n2)
        {
            switch (orientation)
            {
                case ConnectorOrientation.Left:
                    n1 = rect.TopLeft; n2 = rect.BottomLeft;
                    break;
                case ConnectorOrientation.Top:
                    n1 = rect.TopLeft; n2 = rect.TopRight;
                    break;
                case ConnectorOrientation.Right:
                    n1 = rect.TopRight; n2 = rect.BottomRight;
                    break;
                case ConnectorOrientation.Bottom:
                    n1 = rect.BottomLeft; n2 = rect.BottomRight;
                    break;
                default:
                    throw new Exception("No neighour corners found!");
            }
        }

        private static double Distance(Point p1, Point p2)
        {
            return Point.Subtract(p1, p2).Length;
        }

        private static Rect GetRectWithMargin(ConnectionInfo connectionThumb, double margin)
        {
            Rect rect = new Rect(connectionThumb.DesignerItemLeft,
                                 connectionThumb.DesignerItemTop,
                                 connectionThumb.DesignerItemSize.Width,
                                 connectionThumb.DesignerItemSize.Height);

            rect.Inflate(margin, margin);

            return rect;
        }

        private static Point GetOffsetPoint(ConnectionInfo connection, Rect rect)
        {
            Point offsetPoint = new Point();

            switch (connection.Orientation)
            {
                case ConnectorOrientation.Left:
                    offsetPoint = new Point(rect.Left, connection.Position.Y);
                    break;
                case ConnectorOrientation.Top:
                    offsetPoint = new Point(connection.Position.X, rect.Top);
                    break;
                case ConnectorOrientation.Right:
                    offsetPoint = new Point(rect.Right, connection.Position.Y);
                    break;
                case ConnectorOrientation.Bottom:
                    offsetPoint = new Point(connection.Position.X, rect.Bottom);
                    break;
                default:
                    break;
            }

            return offsetPoint;
        }

        private static void CheckPathEnd(ConnectionInfo source, ConnectionInfo sink, bool showLastLine, List<Point> linePoints)
        {
            if (showLastLine)
            {
                Point startPoint = new Point(0, 0);
                Point endPoint = new Point(0, 0);

                switch (source.Orientation)
                {
                    case ConnectorOrientation.Left:
                        startPoint = new Point(source.Position.X - MARGIN_PATH, source.Position.Y);
                        break;
                    case ConnectorOrientation.Top:
                        startPoint = new Point(source.Position.X, source.Position.Y - MARGIN_PATH);
                        break;
                    case ConnectorOrientation.Right:
                        startPoint = new Point(source.Position.X + MARGIN_PATH, source.Position.Y);
                        break;
                    case ConnectorOrientation.Bottom:
                        startPoint = new Point(source.Position.X, source.Position.Y + MARGIN_PATH);
                        break;
                    default:
                        break;
                }

                switch (sink.Orientation)
                {
                    case ConnectorOrientation.Left:
                        endPoint = new Point(sink.Position.X - MARGIN_PATH, sink.Position.Y);
                        break;
                    case ConnectorOrientation.Top:
                        endPoint = new Point(sink.Position.X, sink.Position.Y - MARGIN_PATH);
                        break;
                    case ConnectorOrientation.Right:
                        endPoint = new Point(sink.Position.X + MARGIN_PATH, sink.Position.Y);
                        break;
                    case ConnectorOrientation.Bottom:
                        endPoint = new Point(sink.Position.X, sink.Position.Y + MARGIN_PATH);
                        break;
                    default:
                        break;
                }
                linePoints.Insert(0, startPoint);
                linePoints.Add(endPoint);
            }
            else
            {
                linePoints.Insert(0, source.Position);
                linePoints.Add(sink.Position);
            }
        }

        private static ConnectorOrientation GetOpositeOrientation(ConnectorOrientation connectorOrientation)
        {
            switch (connectorOrientation)
            {
                case ConnectorOrientation.Left:
                    return ConnectorOrientation.Right;
                case ConnectorOrientation.Top:
                    return ConnectorOrientation.Bottom;
                case ConnectorOrientation.Right:
                    return ConnectorOrientation.Left;
                case ConnectorOrientation.Bottom:
                    return ConnectorOrientation.Top;
                default:
                    return ConnectorOrientation.Top;
            }
        }

        #endregion
        #region direct

        /// <summary>
        /// Gets the direct connection line from point to point.
        /// </summary>
        /// <param name="sourceInfo">The source info.</param>
        /// <param name="sinkInfo">The sink info.</param>
        /// <returns></returns>
        private static PathFigureCollection GetDirectConnection(ConnectionInfo sourceInfo, ConnectionInfo sinkInfo)
        {
            PathFigureCollection pfc = new PathFigureCollection(3);

            PathSegment[] segments = new PathSegment[1];

            //add source arrows
            switch (sourceInfo.Cap)
            {
                case ArrowSymbol.Arrow:
                    Vector v1 = sinkInfo.Position - sourceInfo.Position;
                    v1 = v1 / v1.Length * ARROW_SIZE;
                    Vector n1 = new Vector(-v1.Y, v1.X) * ARROW_ANGLE;

                    pfc.Add(new PathFigure(sourceInfo.Position, new PathSegment[]
                    {
                        new LineSegment(sourceInfo.Position + v1 - n1, true), 
                        new LineSegment(sourceInfo.Position + v1 + n1, true)
                    },
                    true));
                    break;
            }

            Vector v2 = sourceInfo.Position - sinkInfo.Position;
            v2 = v2 / v2.Length * ARROW_SIZE;
            Vector n2 = new Vector(-v2.Y, v2.X) * ARROW_ANGLE;

            segments[segments.Length - 1] = new LineSegment(sinkInfo.Position, true); //to insert a gap: sinkInfo.Position + v

            pfc.Add(new PathFigure(sourceInfo.Position, segments, false));

            //add target arrows
            switch (sinkInfo.Cap)
            {
                case ArrowSymbol.Arrow:
                    //add the arrow
                    pfc.Add(new PathFigure(sinkInfo.Position, new PathSegment[]
                    {
                        new LineSegment(sinkInfo.Position + v2 - n2, true), 
                        new LineSegment(sinkInfo.Position + v2 + n2, true)
                    },
                    true));
                    break;
                default:
                    break;
            }

            return pfc;
        }


        #endregion
        #region reflexive

        /// <summary>
        /// Calculates line from the lower left border to the left bottom border.
        /// </summary>
        /// <param name="sourceInfo">The source info.</param>
        /// <param name="sinkInfo">The sink info.</param>
        /// <returns></returns>
        private static PathFigureCollection GetReflexiveConnection(ConnectionInfo sourceInfo, ConnectionInfo sinkInfo)
        {
            Point sourcePoint = new Point(sourceInfo.DesignerItemLeft, sourceInfo.DesignerItemTop + (sourceInfo.DesignerItemSize.Height * 0.65));
            Point sinkPoint = new Point(sinkInfo.DesignerItemLeft + (sinkInfo.DesignerItemSize.Width * 0.35), sinkInfo.DesignerItemTop + sinkInfo.DesignerItemSize.Height);
            List<Point> linePoints = new List<Point>();
            double left = sourcePoint.X - LENGTH_REFLEXIVE;
            double bottom = sinkPoint.Y + LENGTH_REFLEXIVE;

            linePoints.Add(new Point(left, sourcePoint.Y));
            linePoints.Add(new Point(left, bottom));
            linePoints.Add(new Point(sinkPoint.X, bottom));
            linePoints.Add(sinkPoint);

            //fill PathFigureCollection
            PathFigureCollection pfc = new PathFigureCollection();

            if (linePoints.Count > 0)
            {
                int numberSegments = linePoints.Count;
                List<PathSegment> segments = new List<PathSegment>(numberSegments);
                for (int i = 0; i < linePoints.Count; i++)
                {
                    segments.Add(new LineSegment(linePoints[i], true));
                }
                pfc.Add(new PathFigure(sourcePoint, segments, false));
            }
            return pfc;
        }

        #endregion
        #endregion
        #region Helpers

        /// <summary>
        /// Gets the connector orientation on a Designeritem.
        /// </summary>
        /// <param name="p1">The designer item upper left position.</param>
        /// <param name="p2">The point where the connection starts.</param>
        /// <param name="p1Size">Size of the designer item.</param>
        /// <returns></returns>
        internal static ConnectorOrientation GetConnectorOrientation(Point p1, Point p2, Size p1Size)
        {
            //check top
            if (p2.Y <= p1.Y)
                return ConnectorOrientation.Top;
            else if (p2.X <= p1.X)
                return ConnectorOrientation.Left;
            else if (p2.X >= p1.X + p1Size.Width)
                return ConnectorOrientation.Right;
            else
                return ConnectorOrientation.Bottom;
        }

        /// <summary>
        /// Gets the absolute position considering the parent position.
        /// </summary>
        /// <param name="parentSize">Size of the parent.</param>
        /// <param name="parentPosition">The parent position.</param>
        /// <param name="relativePosition">The relative position inside the parent.</param>
        /// <returns>Returns the absolute position considering the parent position on its parent.</returns>
        internal static Point GetAbsolutePosition(Size parentSize, Point parentPosition, Point relativePosition)
        {
            double x = parentSize.Width * relativePosition.X;
            double y = parentSize.Height * relativePosition.Y;

            if (double.IsNaN(x)) x = 0;
            if (double.IsNaN(y)) y = 0;

            return new Point(x + parentPosition.X, y + parentPosition.Y);
        }

        /// <summary>
        /// Gets the connection PathFigureCollection.
        /// </summary>
        /// <param name="sourceInfo">The source info.</param>
        /// <param name="sinkInfo">The sink info.</param>
        /// <param name="routing">The path routing algorithm used.</param>
        /// <returns></returns>
        internal static PathFigureCollection GetConnectionPoints(ConnectionInfo sourceInfo, ConnectionInfo sinkInfo, PathRouting routing)
        {
            if (sourceInfo.Item == sinkInfo.Item)
            {
                return GetReflexiveConnection(sourceInfo, sinkInfo);
            }
            else if (routing == PathRouting.Direct)
            { /* Direct routing from the center of the Source and Sink */
                return GetDirectConnection(sourceInfo, sinkInfo);
            }
            else if (routing == PathRouting.Orthogonal)
            {
                return GetOrthogonalConnectionLine(sourceInfo, sinkInfo, true);
            }
            else
            {
                Logger.Log<ERROR>(String.Format("PathRouting {0} is not implemented", routing));
                return GetDirectConnection(sourceInfo, sinkInfo);
            }
        }

        /// <summary>
        /// ConnectionAdorner hittesting.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="hitPoint">The hit point on the canvas.</param>
        /// <param name="itemToIgnore">The item to ignore.</param>
        /// <param name="hitDesignerItem">The hit designer item.</param>
        /// <param name="relativePosition">The relative position. If set, the mouse is hit near border of the hitDesignerItem.</param>
        /// <param name="isIgnoringItems">Default = false.</param>
        internal static void HitTesting(DesignerCanvas canvas, Point hitPoint, DesignerItem itemToIgnore, out DesignerItem hitDesignerItem, out Point? relativePosition, bool isIgnoringItems = false)
        {
            hitDesignerItem = null;
            relativePosition = null;

            DependencyObject hitObject = canvas.InputHitTest(hitPoint) as DependencyObject;

            while (hitObject != null &&
                   (!isIgnoringItems || (hitObject != itemToIgnore)) &&
                   hitObject.GetType() != typeof(DesignerCanvas))
            {
                if (hitObject is DesignerItem)
                {
                    hitDesignerItem = hitObject as DesignerItem;

                    //calculate relative position
                    double X = 0.0, Y = 0.0;
                    double left = Canvas.GetLeft(hitDesignerItem);
                    double right = left + hitDesignerItem.ActualWidth;
                    double top = Canvas.GetTop(hitDesignerItem);
                    double bottom = top + hitDesignerItem.ActualHeight;

                    //check left)
                    if (hitPoint.X > right - BORDER)
                    {
                        X = 1.0;
                    }
                    else if (hitPoint.X < left + BORDER)
                    {
                        X = 0.0;
                    }
                    else
                    {
                        X = (hitPoint.X - left) / hitDesignerItem.ActualWidth;
                    }

                    //check top
                    if (hitPoint.Y > bottom - BORDER)
                    {
                        Y = 1.0;
                    }
                    else if (hitPoint.Y < top + BORDER)
                    {
                        Y = 0.0;
                    }
                    else
                    {
                        Y = (hitPoint.Y - top) / hitDesignerItem.ActualHeight;
                    }
                    if ((X != 0.0 && X != 1.0) && (Y != 0.0 && Y != 1.0))
                        relativePosition = null;
                    else
                        relativePosition = new Point(X, Y);

                    return;
                }
                hitObject = VisualTreeHelper.GetParent(hitObject);
            }
        }


        /// <summary>
        /// Calculate the intersection point of the line and the border. That's the AttachPoint. where the path starts.
        /// </summary>
        /// <param name="shapeCenter">The center point of the shape.</param>
        /// <param name="shapeSize">Size of the source shape.</param>
        /// <param name="refPoint">The reference point on the other end of the path.</param>
        /// <returns>The attachement point of the line.</returns>
        internal static Point CalculateAttachPoint(Point shapeCenter, Size shapeSize, Point refPoint)
        {
            double[] sides = new double[4];
            sides[0] = (shapeCenter.X - shapeSize.Width / 2.0 - refPoint.X) / (shapeCenter.X - refPoint.X);
            sides[1] = (shapeCenter.Y - shapeSize.Height / 2.0 - refPoint.Y) / (shapeCenter.Y - refPoint.Y);
            sides[2] = (shapeCenter.X + shapeSize.Width / 2.0 - refPoint.X) / (shapeCenter.X - refPoint.X);
            sides[3] = (shapeCenter.Y + shapeSize.Height / 2.0 - refPoint.Y) / (shapeCenter.Y - refPoint.Y);

            double fi = 0;
            for (int i = 0; i < 4; i++)
            {
                if (sides[i] <= 1)
                    fi = Math.Max(fi, sides[i]);
            }

            return refPoint + fi * (shapeCenter - refPoint);
        }

        /// <summary>
        /// Gets the orientation.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <returns></returns>
        private static ConnectorOrientation GetOrientation(Point p1, Point p2)
        {
            if (p1.X == p2.X)
            {
                if (p1.Y >= p2.Y)
                    return ConnectorOrientation.Bottom;
                else
                    return ConnectorOrientation.Top;
            }
            else if (p1.Y == p2.Y)
            {
                if (p1.X >= p2.X)
                    return ConnectorOrientation.Right;
                else
                    return ConnectorOrientation.Left;
            }
            Logger.Log<ERROR>("MatthiasToolbox.GraphDesigner.Utilities.PathFinder", "Failed to retrieve orientation.");
            return ConnectorOrientation.Bottom;
        }

        private static void GetPositionSize(Control shape, out Point point, out Size size)
        {
            point = new Point(Canvas.GetLeft(shape), Canvas.GetTop(shape));
            size = new Size(shape.ActualWidth, shape.ActualHeight);
        }

        /// <summary>
        ///  Gets the absolute position considering the parent position.
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="childSize">Size of the child control.</param>
        /// <param name="relativePositionChild">The relative position of the child.</param>
        /// <returns></returns>
        private static Point GetAbsolutePositionCanvas(ContentControl parentControl, Size childSize, Point relativePositionChild)
        {
            Point parentPos;
            Size parentSize;
            PathFinder.GetPositionSize(parentControl, out parentPos, out parentSize);
            Point absolutePositionPanel = RelativePositionPanel.GetAbsolutePosition(parentSize, childSize, relativePositionChild);
            return new Point(parentPos.X + absolutePositionPanel.X, parentPos.Y + absolutePositionPanel.Y);
        }

        #endregion
    }
}