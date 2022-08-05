using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Solid;
using TSG = Tekla.Structures.Geometry3d;

namespace TeklaAddins
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


        }
        public bool isLeft(TSG.LineSegment line, TSG.Point c)
        {

            TSG.Point a = line.Point1;
            TSG.Point b = line.Point2;
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) > 0;
        }

        public bool isTop(TSG.LineSegment line, TSG.Point c)
        {
            TSG.Point a = line.Point1;
            TSG.Point b = line.Point2;
            TSG.Point midPoint = new TSG.Point();
            midPoint.X = (a.X + b.X) / 2;
            midPoint.Y = (a.Y + b.Y) / 2;
            midPoint.Z = (a.Z + b.Z) / 2;

            return c.Z > midPoint.Z;
        }


        #region Cull Duplicate Points https://github.com/ParametricCamp/TutorialFiles
      
        double Distance(TSG.Point a, TSG.Point b)
        {
            // Calculate sides of the triangle
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;

            // Calculate length of the larger side of the triangle
            double d = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            return d;
        }

        double DistanceSquared(TSG.Point a, TSG.Point b)
        {
            // Calculate sides of the triangle
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;

            // Calculate squared length of the larger side of the triangle
            double d2 = dx * dx + dy * dy + dz * dz;

            return d2;
        }
        #endregion

        List<TSG.Point> RemoveDuplicatePoints(List<TSG.Point> points, double tolerance)
        {
            List<TSG.Point> cleanedPoints = points;
            // Compare against the square of the tolerance
            double t2 = tolerance * tolerance;

            // Go over each point on the list
            for (int i = 0; i < cleanedPoints.Count; i++)
            {
                // Compare with the rest of the points
                // Loop backwards to make sure we don't skip any item
                for (int j = cleanedPoints.Count - 1; j > i; j--)
                {
                    // Use the squared distance to compare points faster
                    double d2 = DistanceSquared(cleanedPoints[i], cleanedPoints[j]);
                    if (d2 < t2)
                    {
                        //Print(i + " is duplicate of " + j);
                        cleanedPoints.RemoveAt(j);
                    }
                }
            }

            return cleanedPoints;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {


            Model myModel = new Model();

            Picker objectPicker = new Picker();

            ModelObjectEnumerator objModelEnum = objectPicker.PickObjects(Picker.PickObjectsEnum.PICK_N_PARTS, "Select beams");

            ArrayList objectsSelected = new ArrayList();

            while (objModelEnum.MoveNext())
                objectsSelected.Add(objModelEnum.Current);

            //output setting out points
            List<TSG.Point> leftBottomPoints = new List<TSG.Point>();
            List<TSG.Point> leftTopPoints = new List<TSG.Point>();
            List<TSG.Point> rightBottomPoints = new List<TSG.Point>();
            List<TSG.Point> rightTopPoints = new List<TSG.Point>();

            foreach (var objBeam in objectsSelected)
            {
                //selected beam
                Beam beam = objBeam as Beam;

                ArrayList beamCentrelinePts = beam.GetCenterLine(false);

                //beam centreline to be used to find which points are on the rx and on the left side
                //https://stackoverflow.com/questions/1560492/how-to-tell-whether-a-point-is-to-the-right-or-left-side-of-a-line

                TSG.LineSegment beamCentreline = new TSG.LineSegment((TSG.Point)beamCentrelinePts[0], (TSG.Point)beamCentrelinePts[1]);


                //check
                //graphicsDrawer.DrawLineSegment( beamCentreline, red);

                //get beam solid to extract the faces
                var solid = beam.GetSolid();

                // example:
                //var maximumPoint = solid.MaximumPoint;
                //var minimumPoint = solid.MinimumPoint;


                FaceEnumerator fe = solid.GetFaceEnumerator();

                int faceCounter = 0;
                int counter = 0;

                List<TSG.Point> facePoints = new List<TSG.Point>();



                //loop faces
                while (fe.MoveNext())
                {
                    Face currentFace = fe.Current;
                    var loopEnum = currentFace.GetLoopEnumerator();

                    List<TSG.Point> currentFacePoints = new List<TSG.Point>();

                    //loop each face Loops
                    while (loopEnum.MoveNext())
                    {
                        var loop = loopEnum.Current;

                        var vertexEnum = loop.GetVertexEnumerator();

                        while (vertexEnum.MoveNext())
                        {
                            TSG.Point vertex = vertexEnum.Current;
                            currentFacePoints.Add(vertex);

                            if (facePoints.Contains(vertex)) {
                                continue;
                            }
                            else
                            {
                                facePoints.Add(vertex);

                                if (isLeft(beamCentreline, vertex))
                                {
                                    if (isTop(beamCentreline, vertex))
                                    {
                                        //graphicsDrawer.DrawText(vertex, "LT" + counter.ToString(), red);
                                        leftTopPoints.Add(vertex);
                                    }
                                    else
                                    {
                                        //graphicsDrawer.DrawText(vertex, "LB" + counter.ToString(), red);
                                        leftBottomPoints.Add(vertex);
                                    }

                                }
                                else
                                {
                                    if (isTop(beamCentreline, vertex))
                                    {
                                        //graphicsDrawer.DrawText(vertex, "RT" + counter.ToString(), red);
                                        rightTopPoints.Add(vertex);
                                    }
                                    else
                                    {
                                        //graphicsDrawer.DrawText(vertex, "RB" + counter.ToString(), red);
                                        rightBottomPoints.Add(vertex);
                                    }


                                }
                                counter++;
                            };
                        }
                    }
                    var x = Math.Round(currentFacePoints.Average(p => p.X));
                    var y = Math.Round(currentFacePoints.Average(p => p.Y));
                    var z = Math.Round(currentFacePoints.Average(p => p.Z));

                    TSG.Point midPoint = new TSG.Point(x, y, z);

                    //create temporary text on screen
                    //graphicsDrawer.DrawText(midPoint, "Face "+faceCounter.ToString(), red);
                    faceCounter++;
                }

            }




            //sort input points
            List<TSG.Point> cleanedListRT = RemoveDuplicatePoints(rightTopPoints, 5).OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            List<TSG.Point> cleanedListLT = RemoveDuplicatePoints(leftTopPoints, 5).OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            List<TSG.Point> cleanedListRB = RemoveDuplicatePoints(rightBottomPoints, 5).OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            List<TSG.Point> cleanedListLB = RemoveDuplicatePoints(leftBottomPoints, 5).OrderBy(p => p.X).ThenBy(p => p.Y).ToList();


            CreatePolycurve(cleanedListRT, "RT");
            CreatePolycurve(cleanedListLT, "LT");
            CreatePolycurve(cleanedListRB, "RB");
            CreatePolycurve(cleanedListLB, "LB");



            myModel.CommitChanges();
            
        }

    private void CreatePolycurve(List<TSG.Point> cleanedList, string csvFileName)
        {
            var red = new Tekla.Structures.Model.UI.Color(1, 0, 0);
            var graphicsDrawer = new GraphicsDrawer();
            //write to csv
            var csv = new StringBuilder();

            TSG.PolycurveGeometryBuilder polycurveConstructor = new TSG.PolycurveGeometryBuilder();

            int cleanedListCount = cleanedList.Count;

            for (int i = 0; i < cleanedListCount - 1; i++)
            {
                graphicsDrawer.DrawText(cleanedList[i], (i + 1).ToString(), red);
                TSG.LineSegment ls = new TSG.LineSegment(cleanedList[i], cleanedList[i + 1]);
                polycurveConstructor.Append(ls);

                csv.AppendLine($"{cleanedList[i].X},{cleanedList[i].Y},{cleanedList[i].Z}");
            }
            graphicsDrawer.DrawText(cleanedList[cleanedListCount - 1], cleanedListCount.ToString(), red);

            csv.AppendLine($"{cleanedList[cleanedListCount - 1].X},{cleanedList[cleanedListCount - 1].Y},{cleanedList[cleanedListCount - 1].Z}");

            TSG.Polycurve polycurveSetout = polycurveConstructor.GetPolycurve();

            ControlPolycurve constructionPolycurve = new ControlPolycurve()
            {
                Geometry = polycurveSetout,
                Color = ControlObjectColorEnum.RED,
                LineType = ControlObjectLineType.SolidLine,
            };

            constructionPolycurve.Insert();
            

            string filePath = $"C:\\temp\\{csvFileName}_setout.csv";

            File.WriteAllText(filePath, csv.ToString());


        }

    }
}
