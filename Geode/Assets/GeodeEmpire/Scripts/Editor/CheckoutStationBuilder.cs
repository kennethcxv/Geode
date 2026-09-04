using System.IO;
using UnityEditor;
using UnityEngine;
using GeodeEmpire.Checkout;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Places the checkout counter and its hardware from the CounterLayout. Everything is positioned in the counter's
    /// own local frame, so the whole station moves as one when the counter moves.
    /// </summary>
    public static class CheckoutStationBuilder
    {
        public const string LayoutPath = "Assets/GeodeEmpire/Data/CounterLayout.asset";

        public static CounterLayout LoadLayout()
        {
            var layout = AssetDatabase.LoadAssetAtPath<CounterLayout>(LayoutPath);
            if (layout == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LayoutPath));
                layout = ScriptableObject.CreateInstance<CounterLayout>();
                AssetDatabase.CreateAsset(layout, LayoutPath);
                AssetDatabase.SaveAssets();
            }
            return layout;
        }

        /// <summary>Build the station under <paramref name="parent"/>; returns the counter transform (the station's frame).</summary>
        public static Transform Build(Transform parent, Vector3 counterPosition, float counterYaw, out CheckoutStation station)
        {
            var layout = LoadLayout();
            var library = AssetDatabase.LoadAssetAtPath<CheckoutPropLibrary>(CheckoutKitBuilder.LibraryPath);
            if (library == null) throw new System.InvalidOperationException("[CheckoutStation] run Geode Empire/Build Checkout Kit first");

            var counterGo = library.Instantiate("checkout_counter", parent);
            var counter = counterGo.transform;
            counter.localPosition = counterPosition;
            counter.localRotation = Quaternion.Euler(0f, counterYaw, 0f);
            foreach (var c in counterGo.GetComponents<BoxCollider>()) c.enabled = true;   // the counter is the one thing here that blocks the player

            station = counterGo.AddComponent<CheckoutStation>();
            station.Layout = layout;
            station.Library = library;
            station.Counter = counter;
            station.CounterRig = counterGo.GetComponent<CheckoutRig>();

            Transform Device(string stem, CounterLayout.Pose pose, float y)
            {
                var go = library.Instantiate(stem, counter);
                go.transform.localPosition = new Vector3(pose.X, y, pose.Z);
                go.transform.localRotation = Quaternion.Euler(0f, pose.Yaw, 0f);
                return go.transform;
            }

            station.MonitorRig = Device("pos_monitor", layout.Monitor, layout.TopY).GetComponent<CheckoutRig>();
            station.TerminalRig = Device("payment_terminal", layout.Terminal, layout.TopY).GetComponent<CheckoutRig>();
            station.CustomerDisplayRig = Device("customer_display", layout.CustomerDisplay, layout.TopY).GetComponent<CheckoutRig>();

            var drawerGo = library.Instantiate("cash_drawer", counter);
            drawerGo.transform.localPosition = layout.Drawer;
            drawerGo.transform.localRotation = Quaternion.identity;
            station.DrawerRig = drawerGo.GetComponent<CheckoutRig>();

            // anchors the transaction moves things between, all in the counter's frame
            Transform Point(string name, Vector3 local, float yaw = 0f)
            {
                var t = new GameObject(name).transform;
                t.SetParent(counter, false);
                t.localPosition = local;
                t.localRotation = Quaternion.Euler(0f, yaw, 0f);
                return t;
            }
            station.StagingPoint = Point("Staging", layout.LocalRect(layout.Staging, layout.TopY));
            station.ScannedPoint = Point("ScannedStaging", layout.LocalRect(layout.ScannedStaging, layout.TopY));
            station.BagPoint = Point("BagStation", layout.Local(layout.BagStation, layout.TopY), layout.BagStation.Yaw);
            station.TenderPoint = Point("CustomerTender", layout.LocalRect(layout.CustomerTender, layout.TopY));
            station.ChangePoint = Point("ChangeHandoff", layout.LocalRect(layout.ChangeHandoff, layout.TopY));
            station.StaffStandPoint = Point("StaffStand", layout.Local(layout.StaffStand, 0f), 180f);
            station.CustomerStandPoint = Point("CustomerStand", layout.Local(layout.CustomerStand, 0f), 0f);

            station.WorkingCamera = CameraPoint(counter, "WorkingCamera", layout.WorkingEye, layout.TopY + layout.EyeAboveCounter,
                                                layout.WorkingLook, layout.TopY + layout.WorkingLookAboveCounter);
            station.DrawerCamera = CameraPoint(counter, "DrawerCamera", layout.DrawerEye, layout.TopY + layout.DrawerEyeAboveCounter,
                                               layout.DrawerLook, layout.TopY + layout.DrawerLookAboveCounter);
            station.CardCamera = CameraPoint(counter, "CardCamera", layout.CardEye, layout.TopY + layout.CardEyeAboveCounter,
                                             layout.CardLook, layout.TopY + layout.CardLookAboveCounter);
            return counter;
        }

        private static Transform CameraPoint(Transform counter, string name, CounterLayout.Pose eye, float eyeY, CounterLayout.Pose look, float lookY)
        {
            var t = new GameObject(name).transform;
            t.SetParent(counter, false);
            t.localPosition = new Vector3(eye.X, eyeY, eye.Z);
            var target = new Vector3(look.X, lookY, look.Z);
            t.localRotation = Quaternion.LookRotation((target - t.localPosition).normalized, Vector3.up);
            return t;
        }
    }
}
