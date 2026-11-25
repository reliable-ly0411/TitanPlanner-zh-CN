using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MissionPlanner.Utilities.Tests
{
    [TestClass]
    public class MissionFileTests
    {
        [DataTestMethod]
        [DataRow(0)]
        [DataRow(3)]
        public void GeneratedMissionReopensItsOrdinaryItems(int frame)
        {
            var source = new List<Locationwp>
            {
                new Locationwp { id = 16 },
                new Locationwp { id = 188 },
                new Locationwp { id = 188, lat = 0.125, lng = -0.25, alt = 123.5f },
                new Locationwp { id = 16, lat = 0.5, lng = -0.5, alt = 50f }
            };
            var path = Path.GetTempFileName();
            try
            {
                MissionFile.WriteFile(path, MissionFile.ConvertFromLocationwps(source, (byte)frame));
                var saved = MissionFile.ReadFile(path);
                CollectionAssert.AreEqual(new[] { "SimpleItem", "SimpleItem", "SimpleItem" },
                    saved.mission.items.Select(item => item.type).ToArray());
                var rows = MissionFile.ConvertToLocationwps(saved);
                CollectionAssert.AreEqual(new ushort[] { 188, 188, 16 },
                    rows.Skip(1).Select(row => row.id).ToArray());
                for (int i = 1; i < source.Count; i++)
                {
                    Assert.AreEqual(source[i].lat, rows[i].lat);
                    Assert.AreEqual(source[i].lng, rows[i].lng);
                    Assert.AreEqual(source[i].alt, rows[i].alt);
                    Assert.AreEqual((byte)frame, rows[i].frame);
                }
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void GenericWriterAndReaderRetainExistingExternalItemTypeRules()
        {
            var externalChild = new MissionFile.Item
            {
                command = 16, frame = 3, @params = new List<double?> { 0, 0, 0, 0, 0.5, -0.5, 75 }
            };
            var original = new MissionFile.RootObject
            {
                mission = new MissionFile.Mission
                {
                    plannedHomePosition = new List<double> { 0, 0, 0 },
                    items = new List<MissionFile.Item>
                    {
                        new MissionFile.Item { type = "ComplexItem",
                            TransectStyleComplexItem = new MissionFile.TransectStyleComplexItem
                            {
                                Items = new List<MissionFile.Item> { externalChild }
                            }
                        },
                        new MissionFile.Item { type = "UnknownTestItem" },
                        new MissionFile.Item()
                    }
                }
            };
            var path = Path.GetTempFileName();
            try
            {
                MissionFile.WriteFile(path, original);
                var saved = MissionFile.ReadFile(path);
                CollectionAssert.AreEqual(new[] { "ComplexItem", "UnknownTestItem", null },
                    saved.mission.items.Select(item => item.type).ToArray());
                var rows = MissionFile.ConvertToLocationwps(saved);
                Assert.AreEqual(2, rows.Count);
                Assert.AreEqual((ushort)16, rows[1].id);
                Assert.AreEqual((byte)3, rows[1].frame);
                Assert.AreEqual(75f, rows[1].alt);
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void LocalPlanJsonRetainsMixedFramesAndSeparateHome()
        {
            var source = new List<Locationwp>
            {
                new Locationwp { frame = 10, lat = 0.125, lng = -0.25, alt = 20f },
                new Locationwp { id = 16, frame = 0, lat = 0.25, lng = -0.5, alt = 123.5f },
                new Locationwp { id = 16, frame = 3, lat = 0.5, lng = -0.75, alt = 47.25f }
            };
            var path = Path.GetTempFileName();
            try
            {
                MissionFile.WriteFile(path, MissionFile.ConvertFromLocationwpsPreservingFrames(source));
                var saved = MissionFile.ReadFile(path);
                CollectionAssert.AreEqual(new[] { 0.125, -0.25, 20.0 }, saved.mission.plannedHomePosition);
                CollectionAssert.AreEqual(new[] { "SimpleItem", "SimpleItem" },
                    saved.mission.items.Select(item => item.type).ToArray());
                CollectionAssert.AreEqual(new[] { 0, 3 }, saved.mission.items.Select(item => item.frame).ToArray());
                var reopened = MissionFile.ConvertToLocationwps(saved).Skip(1).ToArray();
                CollectionAssert.AreEqual(new byte[] { 0, 3 }, reopened.Select(row => row.frame).ToArray());
                CollectionAssert.AreEqual(new[] { 123.5f, 47.25f }, reopened.Select(row => row.alt).ToArray());
                CollectionAssert.AreEqual(new[] { 0.25, 0.5 }, reopened.Select(row => row.lat).ToArray());
                CollectionAssert.AreEqual(new[] { -0.5, -0.75 }, reopened.Select(row => row.lng).ToArray());
            }
            finally { File.Delete(path); }
        }

        [TestMethod]
        public void ExistingConverterStillUsesItsDefaultOrExplicitGlobalFrame()
        {
            var source = new List<Locationwp>
            {
                new Locationwp(),
                new Locationwp { id = 16, frame = 0 },
                new Locationwp { id = 16, frame = 3 }
            };
            CollectionAssert.AreEqual(new[] { 3, 3 }, MissionFile.ConvertFromLocationwps(source)
                .mission.items.Select(item => item.frame).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 0 }, MissionFile.ConvertFromLocationwps(source, 0)
                .mission.items.Select(item => item.frame).ToArray());
            CollectionAssert.AreEqual(new[] { 77, 77 }, MissionFile.ConvertFromLocationwps(source, 77)
                .mission.items.Select(item => item.frame).ToArray());
            CollectionAssert.AreEqual(new byte[] { 0, 3 }, source.Skip(1).Select(item => item.frame).ToArray());
        }
    }
}
