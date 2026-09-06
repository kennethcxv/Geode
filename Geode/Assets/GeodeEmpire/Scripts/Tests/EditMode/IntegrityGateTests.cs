using System.Linq;
using GeodeEmpire.Build;
using GeodeEmpire.Specimens;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GeodeEmpire.Tests
{
    public class IntegrityGateTests
    {
        [Test]
        public void SpecimenCreationAndRevealKeepTheCollisionProxy()
        {
            var library = SpecimenAssetLibrary.Load();
            Assert.That(library, Is.Not.Null);
            var record = new GeodeEmpire.Save.SpecimenRecord { Id = "integrity-test", Seed = 62UL };
            var entity = SpecimenEntity.Create(record, library);
            try
            {
                var colliders = entity.GetComponentsInChildren<MeshCollider>();
                Assert.That(colliders.Length, Is.EqualTo(2));
                foreach (var collider in colliders)
                {
                    Assert.That(collider.convex, Is.True);
                    Assert.That(collider.sharedMesh, Is.Not.EqualTo(collider.GetComponent<MeshFilter>().sharedMesh));
                    Assert.That(2 * collider.sharedMesh.vertexCount - 4, Is.LessThan(255));
                }
                var ids = colliders.Select(c => c.GetEntityId()).ToArray();
                record.Condition.Opened = true;
                entity.ApplyOpenPose();
                entity.RebuildColliders();
                entity.SetStaticCollidable();
                Assert.That(entity.GetComponentsInChildren<MeshCollider>().Select(c => c.GetEntityId()), Is.EqualTo(ids), "reveal must reuse the cooked colliders");
                var b = colliders[0]; var t = colliders[1];
                Assert.That(Physics.ComputePenetration(b, b.transform.position, b.transform.rotation, t, t.transform.position, t.transform.rotation, out _, out _), Is.False);
                LogAssert.NoUnexpectedReceived();
            }
            finally { Object.DestroyImmediate(entity.gameObject); }
        }

        [Test]
        public void AuthoredDeliveriesResolveAnAssetScriptAfterFreshSceneLoad()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/GeodeEmpire/Scenes/Workshop.unity");
            try
            {
                var parent = scene.GetRootGameObjects().Single(x => x.name == "Stations").transform.Find("FixtureDelivery");
                Assert.That(parent, Is.Not.Null);
                var deliveries = parent.Cast<Transform>().Where(x => x.name == "Delivery").ToArray();
                Assert.That(deliveries.Select(x => x.GetSiblingIndex()), Is.EqualTo(new[] { 2, 3, 4 }));
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/GeodeEmpire/Scripts/Runtime/Build/DeliveryCrate.cs");
                Assert.That(script, Is.Not.Null);
                Assert.That(script.GetClass(), Is.EqualTo(typeof(DeliveryCrate)));
                foreach (var delivery in deliveries)
                {
                    Assert.That(delivery.GetComponents<Component>().All(c => c != null), Is.True);
                    var crate = delivery.GetComponent<DeliveryCrate>();
                    Assert.That(crate, Is.Not.Null, "sibling " + delivery.GetSiblingIndex());
                    Assert.That(MonoScript.FromMonoBehaviour(crate), Is.EqualTo(script));
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(delivery.gameObject), Is.Zero);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase(1UL)]
        [TestCase(8UL)]
        [TestCase(26UL)]
        [TestCase(52UL)]
        [TestCase(62UL)] // Original TopHalf_Collider partial-hull warning.
        [TestCase(87UL)]
        [TestCase(96UL)]
        [TestCase(100UL)]
        public void HalfHullsCookWithinBudgetAndKeepTheExterior(ulong seed)
        {
            var geo = GeodeMeshBuilder.Build(SpecimenGenerator.Generate(seed));
            foreach (var half in new[] { geo.Bottom, geo.Top })
            {
                var mesh = half.ToColliderMesh("IntegrityHalf", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
                try
                {
                    // A convex polyhedron with V vertices has at most 2V-4 triangular faces.
                    Assert.That(2 * mesh.vertexCount - 4, Is.LessThan(255));
                    Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
                    var points = mesh.vertices;
                    Assert.That(points.Distinct().Count(), Is.EqualTo(points.Length));
                    float sign = half.IsTop ? 1f : -1f;
                    Assert.That(points.All(p => p.y * sign >= 0f), Is.True, "proxy must stop at the fracture plane");
                    Physics.BakeMesh(mesh.GetEntityId(), true, MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.UseFastMidphase);
                    LogAssert.NoUnexpectedReceived();

                    // Independent directions between the sampling directions, including the lowest support
                    // when a half rests cavity-up. Compare against every exterior vertex of the visual mesh.
                    for (int j = 0; j < 96; j++)
                    {
                        float y = (j + 0.5f) / 96f, angle = j * 2.39996323f;
                        float radial = Mathf.Sqrt(1f - y * y);
                        var direction = new Vector3(radial * Mathf.Cos(angle), sign * y, radial * Mathf.Sin(angle));
                        float visual = float.MinValue, collision = float.MinValue;
                        for (int i = 0; i <= GeodeMeshBuilder.Longitudes * GeodeMeshBuilder.Latitudes; i++)
                            visual = Mathf.Max(visual, Vector3.Dot(half.Vertices[i], direction));
                        foreach (var point in points) collision = Mathf.Max(collision, Vector3.Dot(point, direction));
                        Assert.That(Mathf.Abs(visual - collision), Is.LessThan(geo.MaxRadius * 0.04f), "exterior support, seed " + seed);
                    }
                }
                finally { Object.DestroyImmediate(mesh); }
            }
        }

        [TestCase(26UL)]
        [TestCase(62UL)]
        [TestCase(96UL)]
        public void ClosedAndOpenedHalfCollidersDoNotInterpenetrate(ulong seed)
        {
            var geo = GeodeMeshBuilder.Build(SpecimenGenerator.Generate(seed));
            var scene = EditorSceneManager.NewPreviewScene();
            var bottom = geo.Bottom.ToColliderMesh("IntegrityBottom", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            var top = geo.Top.ToColliderMesh("IntegrityTop", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            try
            {
                MeshCollider Collider(string name, Mesh mesh)
                {
                    var go = new GameObject(name);
                    SceneManager.MoveGameObjectToScene(go, scene);
                    var collider = go.AddComponent<MeshCollider>();
                    collider.convex = true;
                    collider.sharedMesh = mesh;
                    return collider;
                }
                var b = Collider("Bottom", bottom);
                var t = Collider("Top", top);
                bool closedOverlap = Physics.ComputePenetration(b, Vector3.zero, Quaternion.identity, t, Vector3.zero, Quaternion.identity, out _, out float depth);
                Assert.That(closedOverlap && depth > 0.00001f, Is.False, "closed halves overlap " + depth);
                var flip = Quaternion.Euler(0f, 0f, 180f);
                float bottomY = bottom.vertices.Min(v => v.y), topY = top.vertices.Min(v => (flip * v).y);
                float separation = Mathf.Max(geo.MeanEquatorRadius * 2.15f, SpecimenEntity.HalfSeparation(bottom, top, flip, Vector3.left));
                var openedPosition = new Vector3(-separation, bottomY - topY, 0f);
                Assert.That(Physics.ComputePenetration(b, Vector3.zero, Quaternion.identity, t, openedPosition, flip, out _, out _), Is.False, "opened halves overlap");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(bottom);
                Object.DestroyImmediate(top);
            }
        }
    }
}
