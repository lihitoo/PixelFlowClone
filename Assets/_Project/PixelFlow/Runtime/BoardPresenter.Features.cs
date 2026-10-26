using System;
using System.Collections.Generic;
using PixelFlow.Rules;
using TMPro;
using UnityEngine;

namespace PixelFlow
{
    public sealed partial class BoardPresenter
    {
        private readonly Dictionary<int, PixelDefinition> pixelDefinitions = new();
        private readonly List<ObjectView> objectViews = new();
        private readonly List<GameObject> waitingSlots = new();
        private readonly List<TMP_Text> slotLabels = new(), doorLabels = new();
        private readonly List<LineRenderer> linkLines = new();
        private readonly Dictionary<int, TMP_Text> pixelHealthLabels = new();
        private Material hiddenMaterial, obstacleMaterial, trayMaterial, insetMaterial;
        private sealed class ObjectView
        {
            public BoardObjectState State;
            public GameObject Root;
            public readonly List<GameObject> Cells = new();
            public TMP_Text Label;
            public int LastColor = int.MinValue;
        }
        private TMP_Text WorldLabel(Transform parent, string text, Vector3 position, float size = 3)
        {
            var go = new GameObject("Feature Label"); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var label = go.AddComponent<TextMeshPro>(); label.font = art.Font; label.text = text; label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(3.5f, 1.5f);
            label.color = Color.white; label.outlineWidth = .23f; label.outlineColor = new Color(.1f, .12f, .2f); label.raycastTarget = false;
            return label;
        }
        private void BuildWaiting(Material tray, Material inset)
        {
            trayMaterial = tray; insetMaterial = inset;
            foreach (var slot in waitingSlots) { slot.SetActive(false); UnityEngine.Object.Destroy(slot); }
            waitingSlots.Clear(); slotLabels.Clear();
            for (int i = 0; i < session.Waiting.Count; i++)
            {
                var slot = Block("Waiting Slot " + (i + 1), WaitingPosition(i) + Vector3.down * .15f, new Vector3(Mathf.Min(1.2f, 6.8f / session.Waiting.Count), .19f, .96f), tray);
                var inner = Block("Waiting Inset", WaitingPosition(i) + Vector3.down * .04f, new Vector3(1.03f, .08f, .82f), inset);
                inner.transform.SetParent(slot.transform, true);
                var label = WorldLabel(slot.transform, "", Vector3.zero, 2.8f); label.transform.position = WaitingPosition(i) + Vector3.up * .35f;
                waitingSlots.Add(slot); slotLabels.Add(label);
            }
        }
        private void BuildFeatures()
        {
            hiddenMaterial = Material(new Color(.48f, .49f, .61f)); obstacleMaterial = Material(new Color(.50f, .62f, .79f));
            foreach (var pixel in session.Level.Pixels)
            {
                int index = pixel.Y * session.Level.Width + pixel.X; pixelDefinitions.Add(index, pixel);
                if (pixel.Health > 1 || pixel.Hidden)
                {
                    var position = pixels[index].transform.position + Vector3.up * .2f;
                    pixelHealthLabels.Add(index, WorldLabel(root.transform, "", position, Mathf.Clamp(pixel.SpanX * .8f, .8f, 2.8f)));
                }
            }
            foreach (var state in session.BoardObjects)
            {
                var view = new ObjectView { State = state, Root = new GameObject(state.Definition.Kind + " " + state.Definition.Id) };
                view.Root.transform.SetParent(root.transform, false);
                Vector3 center = Vector3.zero;
                foreach (var cell in state.Definition.Cells)
                {
                    var position = PixelPosition(cell.X, cell.Y) + Vector3.up * .20f; center += position;
                    var tile = Block("Object Cell", position, new Vector3(session.Track.CellSize * .98f, .25f, session.Track.CellSize * .98f), obstacleMaterial);
                    tile.transform.SetParent(view.Root.transform, true); view.Cells.Add(tile);
                    var target = tile.AddComponent<BoxCollider>(); target.enabled = false; colorTargets.Add(target);
                    tile.AddComponent<BoardCellView>().Initialize(cell.X, cell.Y, colorSelected);
                }
                center /= state.Definition.Cells.Count;
                view.Label = WorldLabel(view.Root.transform, "", center + Vector3.up * .50f, Mathf.Clamp(state.Definition.Cells.Count * .08f, 1.8f, 3.5f));
                int minX = int.MaxValue, maxX = int.MinValue;
                foreach (var cell in state.Definition.Cells) { minX = Math.Min(minX, cell.X); maxX = Math.Max(maxX, cell.X); }
                view.Label.rectTransform.sizeDelta = new Vector2(Mathf.Max(.6f, (maxX - minX + 1) * session.Track.CellSize * 1.1f), 1.2f);
                view.Label.enableAutoSizing = true; view.Label.fontSizeMin = .7f; view.Label.fontSizeMax = 3;
                view.Label.overflowMode = TextOverflowModes.Truncate;
                objectViews.Add(view);
            }
            foreach (var link in session.Level.Links)
            {
                var go = new GameObject("Linked Pigs"); go.transform.SetParent(root.transform, false);
                var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = obstacleMaterial; line.startWidth = line.endWidth = .06f;
                line.positionCount = link.Members.Count; line.useWorldSpace = true; linkLines.Add(line);
            }
            for (int i = 0; i < session.Level.ConveyorDoors.Count; i++)
            {
                var definition = session.Level.ConveyorDoors[i]; var point = session.Track.At(definition.Progress);
                var door = Block("Conveyor Door", new Vector3(point.X, .45f, point.Z), new Vector3(.15f, 1.1f, .9f), pixelMaterials[definition.Color]);
                doorLabels.Add(WorldLabel(door.transform, "", Vector3.up, 3));
            }
        }
        private void RenderPigFeature(PigView pig, ShooterState state)
        {
            string caption = !state.Revealed ? "?" : state.Kind switch
            {
                ShooterKind.Lock => "LOCK", ShooterKind.Hammer => "HAMMER", ShooterKind.Mallet => "MALLET",
                ShooterKind.Totem => "TOTEM " + state.Counter, _ => state.Ammo.ToString()
            };
            if (state.Ice > 0) caption = "ICE " + state.Ice;
            if (state.PipeId >= 0 && state.Location == ShooterLocation.Queue) caption += "\nPIPE";
            if (state.Astronaut) caption += "\nSPACE";
            pig.GetComponentInChildren<TMP_Text>().text = caption;
            var renderer = pig.GetComponentInChildren<MeshRenderer>();
            int displayedColor = state.Kind == ShooterKind.Mallet && state.FeatureColor >= 0 ? state.FeatureColor : state.Color;
            renderer.sharedMaterial = !state.Revealed ? hiddenMaterial : state.Ice > 0 ? obstacleMaterial : pigMaterials[displayedColor];
        }
        private void RenderFeatures()
        {
            foreach (var pair in pixelHealthLabels)
            {
                int x = pair.Key % session.Level.Width, y = pair.Key / session.Level.Width, hp = session.PixelHealthAt(x, y);
                pair.Value.gameObject.SetActive(hp > 0);
                if (hp > 0) { pair.Value.text = session.PixelRevealed(x, y) ? hp.ToString() : "?"; pair.Value.transform.rotation = camera.transform.rotation; }
            }
            if (waitingSlots.Count != session.Waiting.Count) BuildWaiting(trayMaterial, insetMaterial);
            for (int i = 0; i < slotLabels.Count; i++)
            {
                slotLabels[i].text = session.SlotBlocked(i) ? "CAGE " + session.SlotHealth(i) : "";
                slotLabels[i].transform.rotation = camera.transform.rotation;
            }
            foreach (var view in objectViews)
            {
                var state = view.State; view.Root.SetActive(state.Active); if (!state.Active) continue;
                int color = state.Color;
                if (view.LastColor != color)
                {
                    Material material = color >= 0 && pixelMaterials.TryGetValue(color, out var colored) ? colored : obstacleMaterial;
                    if (state.Definition.Kind == BoardObjectKind.Wall) material = hiddenMaterial;
                    foreach (var tile in view.Cells) tile.GetComponent<MeshRenderer>().sharedMaterial = material;
                    view.LastColor = color;
                }
                for (int i = 0; i < view.Cells.Count; i++)
                {
                    var cell = state.Definition.Cells[i]; view.Cells[i].SetActive(state.Covers(cell.Y * session.Level.Width + cell.X));
                }
                if (state.Definition.Kind == BoardObjectKind.Ufo)
                    for (int i = 0; i < view.Cells.Count; i++)
                    {
                        int seat = i % state.Seats.Length;
                        view.Cells[i].GetComponent<MeshRenderer>().sharedMaterial = state.Seats[seat] ? hiddenMaterial : pixelMaterials[state.Definition.Layers[seat].Color];
                    }
                string name = state.Definition.Kind switch
                {
                    BoardObjectKind.ColoredCage => "CAGE", BoardObjectKind.ShooterCage => "PIG", BoardObjectKind.MusicToy => "MUSIC",
                    BoardObjectKind.MultiSnake => "SNAKE", BoardObjectKind.PixelPipe => "PIPE", BoardObjectKind.ColorDoor => "SHIELD",
                    BoardObjectKind.Matryoshka => "DOLL", BoardObjectKind.BeanBox => "BEANS", BoardObjectKind.EggBox => "EGGS",
                    _ => state.Definition.Kind.ToString().ToUpperInvariant()
                };
                view.Label.text = name + (state.Definition.Kind == BoardObjectKind.Wall ? "" : "\n" + state.Remaining);
                view.Label.transform.rotation = camera.transform.rotation;
            }
            for (int i = 0; i < linkLines.Count; i++)
            {
                var ids = session.Level.Links[i].Members; bool visible = true;
                for (int j = 0; j < ids.Count; j++) if (!pigs[ids[j]].gameObject.activeSelf) visible = false;
                linkLines[i].gameObject.SetActive(visible);
                if (visible) for (int j = 0; j < ids.Count; j++) linkLines[i].SetPosition(j, pigs[ids[j]].transform.position + Vector3.up * .4f);
            }
            for (int i = 0; i < doorLabels.Count; i++)
            {
                doorLabels[i].transform.parent.gameObject.SetActive(session.DoorHealth(i) > 0);
                doorLabels[i].text = session.DoorHealth(i).ToString(); doorLabels[i].transform.rotation = camera.transform.rotation;
            }
        }
    }
}
