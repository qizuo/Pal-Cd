using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.Archery
{
    // Works directly with the original generic Bip001 skeleton; no Humanoid conversion required.
    public sealed class ArcherDemo : MonoBehaviour
    {
        public enum Phase { Idle, RaiseBow, DrawAndCharge, ReleaseArrow, LowerBow }
        public Transform leftUpper, leftForearm, leftHand, rightUpper, rightForearm, rightHand;
        public Material bowMaterial, stringMaterial, arrowMaterial;
        public float raiseSeconds = .55f, drawSeconds = 1.1f, releaseSeconds = .22f, lowerSeconds = .65f;
        public bool autoDemo;
        public bool acceptDemoInput = true, showDemoHUD = true, useWorldCollision;
        public Vector3 aimDirection;
        public Vector3 ShotDirection => aimDirection.sqrMagnitude > .01f ? aimDirection.normalized : transform.forward;
        public Phase CurrentPhase { get; private set; }
        public float Charge { get; private set; }
        public int ShotsFired { get; private set; }
        public event Action<float> ArrowReleased;

        float elapsed, idleTime;
        bool releaseRequested;
        Transform[] joints;
        Quaternion[] rest;
        Vector3 leftRest, rightRest;
        LineRenderer bow, bowstring;
        GameObject heldArrow;
        readonly List<Flight> flights = new List<Flight>();
        struct Flight { public Transform arrow; public Vector3 velocity; public float age, charge; }

        void Awake()
        {
            if (!leftUpper || !leftForearm || !leftHand || !rightUpper || !rightForearm || !rightHand)
            { Debug.LogError("Archery demo: Bip001 arm bones are missing.", this); enabled = false; return; }
            joints = new[] { leftUpper, leftForearm, leftHand, rightUpper, rightForearm, rightHand };
            rest = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) rest[i] = joints[i].localRotation;
            leftRest = transform.InverseTransformPoint(leftHand.position);
            rightRest = transform.InverseTransformPoint(rightHand.position);
            bow = MakeLine("Training bow", bowMaterial, .026f, 25);
            bowstring = MakeLine("Bow string", stringMaterial, .005f, 3);
            heldArrow = MakeArrow();
        }

        public void BeginDraw()
        {
            if (CurrentPhase != Phase.Idle) return;
            releaseRequested = false; Charge = 0; Change(Phase.RaiseBow);
        }
        public void ReleaseDraw() { if (CurrentPhase == Phase.RaiseBow || CurrentPhase == Phase.DrawAndCharge) releaseRequested = true; }
        public void CancelDraw() { releaseRequested = false; Charge = 0; Change(Phase.Idle); }
        void Change(Phase next) { CurrentPhase = next; elapsed = 0; }

        void Update()
        {
            if (acceptDemoInput)
            {
            if (Input.GetKeyDown(KeyCode.A)) autoDemo = !autoDemo;
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) BeginDraw();
            if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space)) ReleaseDraw();
            }
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            switch (CurrentPhase)
            {
                case Phase.Idle:
                    idleTime += dt;
                    if (autoDemo && idleTime > .9f) { idleTime = 0; BeginDraw(); }
                    break;
                case Phase.RaiseBow:
                    if (elapsed >= raiseSeconds) Change(Phase.DrawAndCharge);
                    break;
                case Phase.DrawAndCharge:
                    Charge = Mathf.Clamp01(elapsed / drawSeconds);
                    if ((releaseRequested && Charge >= .15f) || (autoDemo && elapsed > drawSeconds + .65f))
                    { Fire(); Change(Phase.ReleaseArrow); }
                    break;
                case Phase.ReleaseArrow:
                    if (elapsed >= releaseSeconds) Change(Phase.LowerBow);
                    break;
                case Phase.LowerBow:
                    if (elapsed >= lowerSeconds) { Charge = 0; Change(Phase.Idle); }
                    break;
            }
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                Flight f = flights[i]; f.age += dt;
                if (useWorldCollision)
                {
                    float distance = f.velocity.magnitude * dt;
                    if (distance > 0 && Physics.Raycast(f.arrow.position, f.velocity.normalized, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        f.arrow.position = hit.point - f.velocity.normalized * .74f;
                        hit.collider.SendMessageUpwards("OnArrowHit", f.charge, SendMessageOptions.DontRequireReceiver);
                        f.velocity = Vector3.zero;
                    }
                    else f.arrow.position += f.velocity * dt;
                }
                else if (f.arrow.position.z < 7.8f) f.arrow.position += f.velocity * dt;
                if (f.age > 8) { Destroy(f.arrow.gameObject); flights.RemoveAt(i); }
                else flights[i] = f;
            }
        }

        void LateUpdate() { ApplyPose(); }
        public void ApplyPose()
        {
            if (joints == null) return;
            for (int i = 0; i < joints.Length; i++) joints[i].localRotation = rest[i];
            float lift = 0, draw = 0, recoil = 0;
            if (CurrentPhase == Phase.RaiseBow) lift = Smooth(elapsed / raiseSeconds);
            if (CurrentPhase == Phase.DrawAndCharge) { lift = 1; draw = Smooth(Charge); }
            if (CurrentPhase == Phase.ReleaseArrow) { lift = 1; recoil = Smooth(elapsed / releaseSeconds); draw = 1 - recoil; }
            if (CurrentPhase == Phase.LowerBow) lift = 1 - Smooth(elapsed / lowerSeconds);
            Vector3 grip = new Vector3(-.18f, 1.40f, .57f);
            Vector3 nock = Vector3.Lerp(new Vector3(-.15f, 1.40f, .40f), new Vector3(.12f, 1.40f, -.10f), draw);
            if (CurrentPhase == Phase.ReleaseArrow) nock = Vector3.Lerp(new Vector3(.12f, 1.40f, -.10f), new Vector3(.34f, 1.42f, -.20f), recoil);
            Quaternion aim = Quaternion.FromToRotation(transform.forward, ShotDirection);
            Vector3 pivot = transform.TransformPoint(new Vector3(0, 1.4f, 0));
            Vector3 gripWorld = pivot + aim * (transform.TransformPoint(grip) - pivot);
            Vector3 nockWorld = pivot + aim * (transform.TransformPoint(nock) - pivot);
            SolveArm(leftUpper, leftForearm, leftHand, Vector3.Lerp(transform.TransformPoint(leftRest), gripWorld, lift), transform.TransformDirection(new Vector3(-1, -.3f, 0)));
            SolveArm(rightUpper, rightForearm, rightHand, Vector3.Lerp(transform.TransformPoint(rightRest), nockWorld, lift), transform.TransformDirection(Vector3.right));
            Vector3 center = leftHand.position;
            Vector3 up = aim * transform.up, forward = ShotDirection;
            // Recurved limbs flex towards the string as charge rises.
            for (int i = 0; i < bow.positionCount; i++)
            {
                float t = i / (float)(bow.positionCount - 1), y = (t - .5f) * 1.10f;
                float z = .15f * Mathf.Sin(t * Mathf.PI) - .12f - draw * .045f * Mathf.Abs(y);
                bow.SetPosition(i, center + up * y + forward * z);
            }
            Vector3 stringCenter = Vector3.Lerp(center - forward * .12f, rightHand.position, draw);
            bowstring.SetPosition(0, bow.GetPosition(0)); bowstring.SetPosition(1, stringCenter); bowstring.SetPosition(2, bow.GetPosition(bow.positionCount - 1));
            heldArrow.SetActive(CurrentPhase == Phase.RaiseBow || CurrentPhase == Phase.DrawAndCharge);
            heldArrow.transform.position = stringCenter;
            heldArrow.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        static float Smooth(float t) { return Mathf.SmoothStep(0, 1, Mathf.Clamp01(t)); }
        static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 pole)
        {
            Vector3 root = upper.position;
            float a = Vector3.Distance(root, lower.position), b = Vector3.Distance(lower.position, hand.position);
            Vector3 delta = target - root;
            float d = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            Vector3 axis = delta.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(pole, axis).normalized;
            float x = (a * a - b * b + d * d) / (2 * d);
            Vector3 elbow = root + axis * x + bend * Mathf.Sqrt(Mathf.Max(0, a * a - x * x));
            upper.rotation = Quaternion.FromToRotation(lower.position - root, elbow - root) * upper.rotation;
            Vector3 reachable = root + axis * d;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, reachable - lower.position) * lower.rotation;
        }

        LineRenderer MakeLine(string label, Material material, float width, int count)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material;
            line.useWorldSpace = true; line.startWidth = line.endWidth = width;
            line.positionCount = count; line.numCapVertices = 4; return line;
        }
        GameObject MakeArrow()
        {
            var go = new GameObject("Arrow"); go.transform.SetParent(transform, false);
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft"; shaft.transform.SetParent(go.transform, false);
            shaft.transform.localPosition = new Vector3(0, 0, .36f);
            shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
            shaft.transform.localScale = new Vector3(.009f, .36f, .009f);
            shaft.GetComponent<Renderer>().sharedMaterial = arrowMaterial; Destroy(shaft.GetComponent<Collider>());
            var tip = GameObject.CreatePrimitive(PrimitiveType.Cube); tip.name = "Arrowhead";
            tip.transform.SetParent(go.transform, false); tip.transform.localPosition = new Vector3(0, 0, .74f);
            tip.transform.localScale = new Vector3(.027f, .013f, .075f);
            tip.GetComponent<Renderer>().sharedMaterial = stringMaterial; Destroy(tip.GetComponent<Collider>());
            for (int i = 0; i < 2; i++)
            {
                var feather = GameObject.CreatePrimitive(PrimitiveType.Cube); feather.name = "Fletching";
                feather.transform.SetParent(go.transform, false); feather.transform.localPosition = new Vector3(0, 0, .08f);
                feather.transform.localRotation = Quaternion.Euler(0, 0, 90 * i);
                feather.transform.localScale = new Vector3(.065f, .003f, .12f);
                feather.GetComponent<Renderer>().sharedMaterial = stringMaterial; Destroy(feather.GetComponent<Collider>());
            }
            return go;
        }
        void Fire()
        {
            ApplyPose();
            GameObject shot = Instantiate(heldArrow, heldArrow.transform.position, heldArrow.transform.rotation);
            shot.name = "Released arrow"; shot.transform.SetParent(null); shot.SetActive(true);
            flights.Add(new Flight { arrow = shot.transform, velocity = ShotDirection * Mathf.Lerp(18, 40, Charge), charge = Charge });
            ShotsFired++; ArrowReleased?.Invoke(Charge);
        }
        void OnGUI()
        {
            if (!showDemoHUD) return;
            GUI.Box(new Rect(20, 20, 360, 155), "ARCHERY / BLUE ROBE PROTOTYPE");
            GUI.Label(new Rect(35, 50, 335, 25), "Hold LMB / Space: draw | Release: shoot");
            GUI.Label(new Rect(35, 75, 335, 25), "A: auto demo   |   " + CurrentPhase);
            GUI.Label(new Rect(35, 100, 335, 25), "Charge " + Mathf.RoundToInt(Charge * 100) + "%   /   Shots " + ShotsFired);
            GUI.Box(new Rect(35, 135, 325 * Charge, 12), GUIContent.none);
        }
    }
}
