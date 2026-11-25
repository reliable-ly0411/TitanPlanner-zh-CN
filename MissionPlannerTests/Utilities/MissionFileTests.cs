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
    }
}
