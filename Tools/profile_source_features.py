"""Inventory decoded level JSON only; no media reads, imports or gameplay claims."""
import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path


FEATURES = {
    "SurpriseShooters": "Shooters", "ConnectedShooters": "Connections",
    "Locks": "Shooters", "ShooterPipes": "Pipes", "Hammers": "Shooters",
    "ShooterIceBlocks": "IceBlocks", "ChainedShooters": "Chains",
    "BullTotems": "Totems", "MusicToyMallets": "Mallets", "SlotCages": "Cages",
    "AstronautShooters": "Shooters", "ConveyorDoors": "Doors",
}
MAIN_FUNNEL = "3db5681ee587345ad9a3cf8400a692d1"


def profile(source):
    manifest = json.loads((source / "manifest.json").read_text(encoding="utf-8-sig"))
    groups = []
    all_counts = Counter()
    main = None
    for entry in manifest["funnels"]:
        group = source / entry["funnelId"]
        counts, slots, belts = Counter(), Counter(), Counter()
        samples = {key: [] for key in FEATURES}
        samples["HasTimeLimit"] = []
        basic, failures = [], []
        files = sorted(group.glob("*.json"), key=lambda p: int(p.stem))
        for path in files:
            level = json.loads(path.read_text(encoding="utf-8-sig"))
            active = []
            for feature, field in FEATURES.items():
                value = level.get(feature, {}).get(field)
                if not isinstance(value, list):
                    failures.append({"level": int(path.stem), "field": feature + "." + field})
                elif value:
                    active.append(feature)
            if level.get("HasTimeLimit"):
                active.append("HasTimeLimit")
            for feature in active:
                counts[feature] += 1
                if len(samples[feature]) < 3:
                    samples[feature].append(int(path.stem))
            if not active:
                basic.append(int(path.stem))
            slots[level["SlotCount"]] += 1
            belts[level["ConveyorLimit"]] += 1
        record = {
            "funnel": entry["funnelId"], "manifestCount": entry["count"],
            "decodedCount": len(files), "featureCounts": dict(counts),
            "firstSamples": samples, "basicSchemaLevels": len(basic),
            "slotCounts": dict(slots), "conveyorLimits": dict(belts),
            "schemaGaps": failures,
        }
        if record["manifestCount"] != record["decodedCount"]:
            raise ValueError("Manifest/decoded count mismatch: " + entry["funnelId"])
        if failures:
            raise ValueError("Unknown feature shape: " + str(failures[:3]))
        all_counts.update(counts)
        if entry["funnelId"] == MAIN_FUNNEL:
            main = {**record, "firstBasicSchemaLevels": basic[:25]}
        groups.append(record)
    return {
        "schemaVersion": 1, "checkedAt": datetime.now(timezone.utc).isoformat(),
        "source": str(source),
        "manifestSha256": hashlib.sha256((source / "manifest.json").read_bytes()).hexdigest(),
        "scope": "Observed decoded JSON feature presence only. Counts overlap; empty feature lists do not prove runtime support, solvability, semantics or final clone scope.",
        "decodedLevels": sum(group["decodedCount"] for group in groups),
        "featureCountsAllFunnels": dict(all_counts), "mainCampaign": main, "groups": groups,
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    report = profile(args.source.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    main = report["mainCampaign"]
    print(json.dumps({"decodedLevels": report["decodedLevels"], "mainCampaignLevels": main["decodedCount"],
                      "mainFeatureCounts": main["featureCounts"], "firstSamples": main["firstSamples"],
                      "firstBasicSchemaLevels": main["firstBasicSchemaLevels"]}, indent=2))
