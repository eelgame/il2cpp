using Advanced.Algorithms.Geometry;


namespace Advanced.Algorithms.Tests.Geometry
{
    
    public class PointRotation_Tests
    {
        [NUnit.Framework.Test]
        public void PointRotation_Smoke_Test()
        {
            var result = PointRotation.Rotate(
                new Point(0, 0),
                new Point(5, 5),
                -45);

            NUnit.Framework.Assert.AreEqual(7, (int)result.X);
            NUnit.Framework.Assert.AreEqual(0, (int)result.Y);

            result = PointRotation.Rotate(
                new Point(0, 0),
                new Point(5, 5),
                -90);

            NUnit.Framework.Assert.AreEqual(5, (int)result.X);
            NUnit.Framework.Assert.AreEqual(-5, (int)result.Y);
        }
    }
}
