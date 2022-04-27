using Advanced.Algorithms.Geometry;

using System.Collections.Generic;

namespace Advanced.Algorithms.Tests.Geometry
{

    
    public class PointInsidePolygon_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void PointInsidePolygon_Smoke_Test()
        {
            var polygon = new Polygon(new List<Line>() {
                    new Line(new Point(0,0),new Point(10,10)),
                    new Line(new Point(10,10),new Point(11,11)),
                    new Line(new Point(11,11),new Point(0,10))
                });

            var testPoint = new Point(20, 20);

            HuaTuo.NUnit.Framework.Assert.IsFalse(PointInsidePolygon.IsInside(polygon, testPoint));

            testPoint = new Point(5, 5);
            HuaTuo.NUnit.Framework.Assert.IsTrue(PointInsidePolygon.IsInside(polygon, testPoint));
        }
    }
}
