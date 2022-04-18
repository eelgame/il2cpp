using Advanced.Algorithms.Geometry;


namespace Advanced.Algorithms.Tests.Geometry
{

    
    public class RectangleIntersection_Tests
    {
        [NUnit.Framework.Test]
        public void RectIntersection_Smoke_Test()
        {
            var rectangleComparer = new RectangleComparer();

            var result = RectangleIntersection.FindIntersection(new Rectangle()
            {
                LeftTop = new Point(0, 10),
                RightBottom = new Point(10, 0)
            },
            new Rectangle()
            {
                LeftTop = new Point(5, 5),
                RightBottom = new Point(15, 0)
            });

            NUnit.Framework.Assert.IsTrue(rectangleComparer.Equals(result, new Rectangle()
            {
                LeftTop = new Point(5, 5),
                RightBottom = new Point(10, 0)
            }));

            result = RectangleIntersection.FindIntersection(new Rectangle()
            {
                LeftTop = new Point(0, 10),
                RightBottom = new Point(4, 0)
            },
            new Rectangle()
            {
                LeftTop = new Point(5, 5),
                RightBottom = new Point(15, 0)
            });

            NUnit.Framework.Assert.IsTrue(rectangleComparer.Equals(result, null));
        }

    }
}
