using System;
using System.Collections.Generic;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PixelFlow
{
    public sealed partial class BoardPresenter : IDisposable
    {
        private readonly PuzzleSession session;
        private readonly PixelFlowPresentation art;
        private readonly GameObject root;
        private readonly Camera camera;
        private readonly Mesh cube;
        private readonly List<Material> materials = new();
        private readonly Dictionary<int, Material> pixelMaterials = new();
        private readonly Dictionary<int, Material> pigMaterials = new();
        private readonly Dictionary<int, PigView> pigs = new();
        private readonly Dictionary<int, int> queueRows = new();
        private readonly GameObject[] pixels;
        private readonly List<Collider> colorTargets = new();
        private readonly Action<int, int> colorSelected;
        private bool handTargeting, colorTargeting;
        private readonly float[] hitTimes;
        private readonly List<Transform> treads = new();
        private readonly Projectile[] projectiles = new Projectile[64];
        private readonly Action hitSound;
        private float elapsed;
        private int projectileIndex;
        private readonly Vector3 pixelScale;
        private const float PigScale = .90f;

        private sealed class Projectile
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Vector3 From, To;
            public float Started = -100;
        }

        public BoardPresenter(Transform owner, Camera camera, PixelFlowPresentation art, PixelFlowCampaignConfig campaign,
            PuzzleSession session, Action<int> selected, Action hitSound, Action<int, int> colorSelected)
        {
            this.session = session; this.art = art; this.camera = camera; this.hitSound = hitSound;
            this.colorSelected = colorSelected;
            root = new GameObject("Puzzle Presentation"); root.transform.SetParent(owner, false);
            cube = SoftCube.Create();
            foreach (var entry in campaign.Colors)
            {
                var color = campaign.ColorFor(entry.Id);
                pixelMaterials.Add(entry.Id, Material(color));
                var pig = Material(color); pig.SetTexture("_BaseMap", art.PigTexture); pig.SetFloat("_Smoothness", .42f);
                pigMaterials.Add(entry.Id, pig);
            }
            var ground = Material(new Color(.25f, .24f, .39f));
            Block("Floor", new Vector3(0, -.60f, 0), new Vector3(13, .12f, 24), ground);
            var boardRim = Material(new Color(.25f, .20f, .36f));
            var boardBase = Material(new Color(.31f, .26f, .43f));
            Block("Board Rim", new Vector3(0, -.29f, TrackGeometry.CenterZ), new Vector3(session.Track.BoardWidth + .26f, .23f, session.Track.BoardHeight + .26f), boardRim);
            Block("Board Bed", new Vector3(0, -.16f, TrackGeometry.CenterZ), new Vector3(session.Track.BoardWidth + .09f, .12f, session.Track.BoardHeight + .09f), boardBase);
            var rail = Material(new Color(.18f, .25f, .55f));
            var tread = Material(new Color(.30f, .40f, .70f));
            var rim = Material(new Color(.47f, .56f, .84f));
            float bottom = session.Track.Bottom, top = session.Track.Top;
            float half = session.Track.HalfWidth;
            foreach (float x in new[] { -half, half })
            {
                Block("Belt Outer Rail", new Vector3(x, -.22f, (top + bottom) / 2), new Vector3(.95f, .20f, top - bottom + .92f), rim);
                Block("Belt Bed", new Vector3(x, -.10f, (top + bottom) / 2), new Vector3(.76f, .20f, top - bottom + .75f), rail);
            }
            foreach (float z in new[] { bottom, top })
            {
                Block("Belt Outer Rail", new Vector3(0, -.22f, z), new Vector3(2 * half + .92f, .20f, .95f), rim);
                Block("Belt Bed", new Vector3(0, -.10f, z), new Vector3(2 * half + .75f, .20f, .76f), rail);
            }
            for (int i = 0; i < 112; i++) treads.Add(Block("Conveyor Tread", Vector3.zero, new Vector3(.67f, .075f, .19f), tread).transform);
            var tray = Material(new Color(.31f, .25f, .44f));
            BuildWaiting(tray, boardBase);
            pixelScale = new Vector3(session.Track.CellSize * .94f, .20f, session.Track.CellSize * .94f);
            pixels = new GameObject[session.Level.Width * session.Level.Height]; hitTimes = new float[pixels.Length];
            Array.Fill(hitTimes, -1);
            foreach (var pixel in session.Level.Pixels)
            {
                var scale = new Vector3(pixelScale.x * pixel.SpanX, pixelScale.y, pixelScale.z * pixel.SpanY);
                var position = PixelPosition(pixel.X, pixel.Y) + new Vector3((pixel.SpanX - 1) * session.Track.CellSize / 2, 0, (pixel.SpanY - 1) * session.Track.CellSize / 2);
                pixels[pixel.Y * session.Level.Width + pixel.X] = Block("Pixel " + pixel.X + "," + pixel.Y, position, scale, pixelMaterials[pixel.Color]);
                var cell = pixels[pixel.Y * session.Level.Width + pixel.X];
                var target = cell.AddComponent<BoxCollider>(); target.enabled = false;
                colorTargets.Add(target);
                cell.AddComponent<BoardCellView>().Initialize(pixel.X, pixel.Y, colorSelected);
            }

            var markerMaterial = Material(new Color(.92f, 1f, .88f));
            foreach (var shooter in session.Shooters.Values)
            {
                var pig = new GameObject("Pig " + shooter.Id); pig.transform.SetParent(root.transform, false);
                var body = new GameObject("Original Pig Mesh"); body.transform.SetParent(pig.transform, false);
                // Original face/eye UV vertices point toward +Z; this camera sits at -Z.
                body.transform.localRotation = Quaternion.Euler(0, 180, 0);
                body.AddComponent<MeshFilter>().sharedMesh = art.PigMesh;
                var renderer = body.AddComponent<MeshRenderer>(); renderer.sharedMaterial = pigMaterials[shooter.Color];
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                var collider = pig.AddComponent<BoxCollider>(); collider.center = new Vector3(0, .75f, 0); collider.size = new Vector3(1.1f, 1.7f, 1.6f);
                var labelObject = new GameObject("Ammo"); labelObject.transform.SetParent(pig.transform, false);
                labelObject.transform.localPosition = new Vector3(0, 1.85f, -.05f);
                var label = labelObject.AddComponent<TextMeshPro>(); label.font = art.Font; label.fontSize = 6.5f;
                label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(1.75f, 1.5f);
                label.enableAutoSizing = true; label.fontSizeMin = 2; label.fontSizeMax = 6.5f;
                label.overflowMode = TextOverflowModes.Truncate;
                label.color = Color.white; label.outlineColor = new Color(.12f, .19f, .22f); label.outlineWidth = .22f;
                label.renderer.shadowCastingMode = ShadowCastingMode.Off;
                var marker = Block("Ready Marker", Vector3.zero, new Vector3(1.15f, .035f, 1.5f), markerMaterial);
                marker.GetComponent<MeshRenderer>().sharedMaterial = pixelMaterials[shooter.Color];
                marker.transform.SetParent(pig.transform, false); marker.transform.localPosition = new Vector3(0, -.10f, 0);
                var view = pig.AddComponent<PigView>(); view.Initialize(shooter.Id, selected, label, marker, camera);
                pigs.Add(shooter.Id, view);
                int row = Math.Max(0, session.QueueOffset(shooter.Id));
                pig.transform.position = QueuePosition(shooter.Column, row); pig.transform.localScale = Vector3.one * PigScale;
            }
            for (int i = 0; i < projectiles.Length; i++)
            {
                var go = Block("Pooled Color Shot", Vector3.zero, Vector3.one * .13f, markerMaterial); go.SetActive(false);
                projectiles[i] = new Projectile { Transform = go.transform, Renderer = go.GetComponent<MeshRenderer>() };
            }
            session.Shot += OnShot;
            BuildFeatures();
            Render(0);
        }

        public Vector3 WaitingPosition(int index) => new((index - (session.Waiting.Count - 1) / 2f) * Mathf.Min(1.40f, 7f / session.Waiting.Count), .03f, session.Track.Bottom - 1.50f);
        public Vector3 PixelPosition(int x, int y) => new((x + .5f) * session.Track.CellSize - session.Track.BoardWidth / 2, .07f, TrackGeometry.CenterZ + (y + .5f) * session.Track.CellSize - session.Track.BoardHeight / 2);
        public Vector3 PigPosition(int id) => pigs[id].transform.position;
        public PigView Pig(int id) => pigs[id];
        public void SetTargeting(bool hand, bool color)
        {
            handTargeting = hand;
            if (colorTargeting == color) return;
            colorTargeting = color;
            foreach (var target in colorTargets) if (target != null) target.enabled = color;
        }

        private Vector3 QueuePosition(int column, int row) => new((column - (session.ColumnCount - 1) / 2f) * 1.95f, .02f, session.Track.Bottom - 3.40f - row * 1.15f);

        private Material Material(Color color)
        {
            var material = new Material(art.LitMaterial); material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .23f);
            materials.Add(material); return material;
        }

        private GameObject Block(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = cube;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        private void OnShot(ShotRecord shot)
        {
            if (shot.Destroyed && shot.ObjectId < 0) hitTimes[shot.Y * session.Level.Width + shot.X] = elapsed + .09f;
            var projectile = projectiles[projectileIndex++ % projectiles.Length];
            projectile.From = pigs.TryGetValue(shot.ShooterId, out var source) ? source.transform.position + Vector3.up * .65f : PixelPosition(shot.X, shot.Y) + Vector3.up * 2;
            projectile.To = PixelPosition(shot.X, shot.Y) + Vector3.up * .15f;
            projectile.Started = elapsed;
            projectile.Renderer.sharedMaterial = pixelMaterials[shot.Color];
            projectile.Transform.gameObject.SetActive(true);
            hitSound?.Invoke();
        }

        public void Render(float delta)
        {
            elapsed += delta;
            for (int i = 0; i < treads.Count; i++)
            {
                var point = session.Track.At((float)i / treads.Count + elapsed / 22f);
                treads[i].localPosition = new Vector3(point.X, .015f, point.Z);
                treads[i].localRotation = Quaternion.Euler(0, point.Side is BoardSide.Top or BoardSide.Bottom ? 90 : 0, 0);
            }
            queueRows.Clear();
            for (int column = 0; column < session.ColumnCount; column++)
                for (int row = 0; row < session.QueueCount(column); row++) queueRows[session.QueueAt(column, row).Id] = row;
            float blend = delta > 0 ? 1 - Mathf.Exp(-delta * 18) : 1;
            int rows = 1;
            if (handTargeting) for (int column = 0; column < session.ColumnCount; column++) rows = Mathf.Max(rows, session.QueueCount(column));
            float queueGap = 1.15f;
            if (handTargeting && rows > 1)
            {
                var plane = new Plane(Vector3.up, new Vector3(0, .02f, 0));
                var ray = camera.ViewportPointToRay(new Vector3(.5f, .08f)); float distance;
                if (plane.Raycast(ray, out distance)) queueGap = Mathf.Clamp((session.Track.Bottom - 4.10f - ray.GetPoint(distance).z) / rows, .12f, 1.15f);
            }
            foreach (var state in session.Shooters.Values)
            {
                var pig = pigs[state.Id];
                bool visible = state.Location is not (ShooterLocation.Finished or ShooterLocation.Stored);
                if (!handTargeting && state.Location == ShooterLocation.Queue && queueRows[state.Id] >= 3) visible = false;
                pig.gameObject.SetActive(visible);
                if (!visible) continue;
                Vector3 target; float scale = PigScale;
                if (state.Location == ShooterLocation.Conveyor)
                {
                    var point = session.Track.At(Mathf.Max(state.Progress, 0));
                    target = new Vector3(point.X, .06f, point.Z);
                    if (state.Progress < 0) target += Vector3.back * (-state.Progress * 4);
                }
                else if (state.Location == ShooterLocation.Waiting)
                {
                    int slot = 0; while (session.Waiting[slot] != state) slot++;
                    target = WaitingPosition(slot);
                }
                else
                {
                    int row = queueRows[state.Id]; target = QueuePosition(state.Column, row);
                    if (handTargeting) { target.z = session.Track.Bottom - 4.10f - row * queueGap; scale *= Mathf.Min(1, queueGap / 1.15f); }
                    else scale *= Mathf.Pow(.84f, row);
                }
                pig.transform.position = Vector3.Lerp(pig.transform.position, target, blend);
                pig.transform.localScale = Vector3.Lerp(pig.transform.localScale, Vector3.one * scale, blend);
                pig.SetState(state.Ammo, session.Status == SessionStatus.Playing && session.CanSelect(state.Id, handTargeting));
                RenderPigFeature(pig, state);
            }
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] == null) continue;
                int x = i % session.Level.Width, y = i / session.Level.Width;
                bool alive = session.PixelHealthAt(x, y) > 0;
                if (alive)
                {
                    var definition = pixelDefinitions[i]; pixels[i].SetActive(true); hitTimes[i] = -1;
                    pixels[i].transform.localScale = new Vector3(pixelScale.x * definition.SpanX, pixelScale.y, pixelScale.z * definition.SpanY);
                    pixels[i].transform.localPosition = PixelPosition(x, y) + new Vector3((definition.SpanX - 1) * session.Track.CellSize / 2, 0, (definition.SpanY - 1) * session.Track.CellSize / 2);
                    pixels[i].GetComponent<MeshRenderer>().sharedMaterial = session.PixelRevealed(x, y) ? pixelMaterials[definition.Color] : hiddenMaterial;
                    continue;
                }
                if (hitTimes[i] < 0) { pixels[i].SetActive(false); continue; }
                if (!pixels[i].activeSelf || elapsed < hitTimes[i]) continue;
                float life = Mathf.Clamp01((elapsed - hitTimes[i]) / .18f);
                var original = pixelDefinitions[i];
                pixels[i].transform.localScale = new Vector3(pixelScale.x * original.SpanX, pixelScale.y, pixelScale.z * original.SpanY) * (1 - life);
                pixels[i].transform.localPosition += Vector3.up * delta * .4f;
                if (life >= 1) pixels[i].SetActive(false);
            }
            RenderFeatures();
            foreach (var shot in projectiles)
            {
                if (!shot.Transform.gameObject.activeSelf) continue;
                float life = (elapsed - shot.Started) / .10f;
                if (life >= 1) { shot.Transform.gameObject.SetActive(false); continue; }
                shot.Transform.position = Vector3.Lerp(shot.From, shot.To, life) + Vector3.up * (Mathf.Sin(life * Mathf.PI) * .22f);
                shot.Transform.Rotate(60 * delta, 180 * delta, 40 * delta);
            }
        }

        public void Dispose()
        {
            session.Shot -= OnShot;
            // Destroy is deferred. Stop retired colliders/renderers immediately so
            // a same-frame restart cannot dispatch input into the old board.
            root.SetActive(false);
            Object.Destroy(root);
            foreach (var material in materials) Object.Destroy(material);
            Object.Destroy(cube);
        }
    }
}
