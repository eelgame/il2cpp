﻿using Advanced.Algorithms.Geometry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Advanced.Algorithms.Tests.Geometry
{
    
    public class BentleyOttmann_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Smoke_Test_1()
        {
            var lines = new List<Line>();

            var s1 = new Line(new Point(0, 200), new Point(100, 300));
            var s2 = new Line(new Point(20, 350), new Point(110, 150));
            var s3 = new Line(new Point(30, 250), new Point(80, 120));
            var s4 = new Line(new Point(50, 100), new Point(120, 300));

            lines.AddRange(new[] { s1, s2, s3, s4 });

            var expectedIntersections = getExpectedIntersections(lines);

            var bentleyOttmannAlgorithm = new BentleyOttmann();

            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Smoke_Test_2()
        {
            var lines = new List<Line>();

            var s1 = new Line(new Point(100, 0), new Point(150, 130));
            var s2 = new Line(new Point(20, 80), new Point(80, 70));
            var s3 = new Line(new Point(80, 70), new Point(50, 100));

            lines.AddRange(new[] { s1, s2, s3 });

            var expectedIntersections = getExpectedIntersections(lines);

            var bentleyOttmannAlgorithm = new BentleyOttmann();

            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Vertical_Lines_Test()
        {
            var lines = new List<Line>();

            lines.AddRange(verticalLines());

            var expectedIntersections = getExpectedIntersections(lines);

            var bentleyOttmannAlgorithm = new BentleyOttmann();

            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Horizontal_Lines_Test()
        {
            var lines = new List<Line>();

            lines.AddRange(horizontalLines());

            var expectedIntersections = getExpectedIntersections(lines);

            var bentleyOttmannAlgorithm = new BentleyOttmann();

            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Vertical_Horizontal_Lines_Test()
        {
            var lines = new List<Line>();

            //vertical
            lines.Add(new Line(new Point(100, 100), new Point(100, 200)));
            lines.Add(new Line(new Point(125, 100), new Point(125, 200)));
            lines.Add(new Line(new Point(150, 100), new Point(150, 200)));
            lines.Add(new Line(new Point(175, 100), new Point(175, 200)));
            lines.Add(new Line(new Point(200, 100), new Point(200, 200)));

            //horizontal
            lines.Add(new Line(new Point(100, 100), new Point(200, 100)));
            lines.Add(new Line(new Point(100, 125), new Point(200, 125)));
            lines.Add(new Line(new Point(100, 150), new Point(200, 150)));
            lines.Add(new Line(new Point(100, 175), new Point(200, 175)));
            lines.Add(new Line(new Point(100, 200), new Point(200, 200)));

            var expectedIntersections = getExpectedIntersections(lines);

            var bentleyOttmannAlgorithm = new BentleyOttmann();

            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Vertical_Horizontal_Other_Lines_Test_1()
        {
            var lines = new List<Line>();

            //vertical
            lines.Add(new Line(new Point(100, 100), new Point(100, 200)));
            lines.Add(new Line(new Point(200, 100), new Point(200, 200)));

            //horizontal
            lines.Add(new Line(new Point(100, 100), new Point(200, 100)));
            lines.Add(new Line(new Point(100, 150), new Point(200, 150)));
            lines.Add(new Line(new Point(100, 200), new Point(200, 200)));

            //other lines
            lines.Add(new Line(new Point(100, 100), new Point(200, 200)));
            lines.Add(new Line(new Point(100, 200), new Point(200, 100)));

            var expectedIntersections = getExpectedIntersections(lines);

            var bentleyOttmannAlgorithm = new BentleyOttmann();

            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }


        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Vertical_Horizontal_Other_Lines_Test_2()
        {
            var lines = new List<Line>();

            //vertical
            lines.Add(new Line(new Point(100, 100), new Point(100, 200)));
            lines.Add(new Line(new Point(200, 100), new Point(200, 200)));

            //horizontal
            lines.Add(new Line(new Point(100, 100), new Point(200, 100)));
            lines.Add(new Line(new Point(100, 150), new Point(200, 150)));
            lines.Add(new Line(new Point(100, 200), new Point(200, 200)));

            //other lines
            lines.Add(new Line(new Point(110, 100), new Point(210, 200)));
            lines.Add(new Line(new Point(90, 200), new Point(250, 100)));

            var expectedIntersections = getExpectedIntersections(lines);

            var bentleyOttmannAlgorithm = new BentleyOttmann();

            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void BentleyOttmann_Stress_Test()
        {
            var lines = new List<Line>();

            lines.AddRange(getRandomLines(1000));

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            var expectedIntersections = getExpectedIntersections(lines);
            stopWatch.Stop();

            var naiveElapsedTime = stopWatch.ElapsedMilliseconds;

            var bentleyOttmannAlgorithm = new BentleyOttmann();
            stopWatch.Reset();

            stopWatch.Start();
            var actualIntersections = bentleyOttmannAlgorithm.FindIntersections(lines);
            stopWatch.Stop();

            var actualElapsedTime = stopWatch.ElapsedMilliseconds;

            HuaTuo.NUnit.Framework.Assert.IsTrue(actualElapsedTime <= naiveElapsedTime);
            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);

        }

        private static Random random = new Random();

        private static Dictionary<Point, List<Line>> getExpectedIntersections(List<Line> lines)
        {
            var result = new Dictionary<Point, HashSet<Line>>(new PointComparer());

            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    var intersection = LineIntersection.Find(lines[i], lines[j]);

                    if (intersection != null)
                    {
                        var existing = result.ContainsKey(intersection) ?
                                 result[intersection] : new HashSet<Line>();

                        if (!existing.Contains(lines[i]))
                        {
                            existing.Add(lines[i]);
                        }

                        if (!existing.Contains(lines[j]))
                        {
                            existing.Add(lines[j]);
                        }

                        result[intersection] = existing;
                    }
                }
            }

            return result.ToDictionary(x => x.Key, x => x.Value.ToList());
        }

        private static List<Line> getRandomLines(int lineCount)
        {
            var lines = new List<Line>();

            while (lineCount > 0)
            {
                lines.Add(getRandomLine());
                lineCount--;
            }

            return lines;
        }

        private static List<Line> verticalLines()
        {
            var lines = new List<Line>();

            var s1 = new Line(new Point(100, 200), new Point(100, 600));
            var s2 = new Line(new Point(100, 225), new Point(100, 625));
            var s3 = new Line(new Point(100, 250), new Point(100, 475));
            var s4 = new Line(new Point(100, 290), new Point(100, 675));

            lines.AddRange(new[] { s1, s2, s3, s4 });

            return lines;
        }

        private static List<Line> horizontalLines()
        {
            var lines = new List<Line>();

            var s1 = new Line(new Point(100, 100), new Point(600, 100));
            var s2 = new Line(new Point(225, 100), new Point(625, 100));
            var s3 = new Line(new Point(250, 100), new Point(475, 100));
            var s4 = new Line(new Point(290, 100), new Point(675, 100));

            lines.AddRange(new[] { s1, s2, s3, s4 });

            return lines;
        }

        private static Line getRandomLine()
        {
            var leftX = random.Next(0, 1000000) * random.NextDouble();
            var leftY = random.Next(0, 1000000) * random.NextDouble();

            var rightX = leftX + random.Next(0, 10000);
            var rightY = leftY + random.Next(0, 10000);

            return new Line(new Point(leftX, leftY), new Point(rightX, rightY));
        }
    }
}
