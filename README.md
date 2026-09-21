# Ciociaria Guerra Bot

A simulation game where the municipalities of the province of Frosinone fight to conquer the entire Ciociaria.

## How it works

* Each municipality starts as an independent territory.
* Every turn, a municipality is randomly selected.
* If it has already been conquered, its current owner becomes the attacker.
* The attacker conquers the closest available municipality.
* When a territory is conquered, its owner changes to the attacker.
* Each owner's territorial center is recalculated based on the territories it controls.
* A municipality is eliminated when it no longer owns any territory.
* The game continues until only one owner remains.

The game provides two modes:
* **Short** → The classic mode. If a captured territory is extracted, its owner becomes the attacker. The more territories a player owns, the higher their probability of being selected as the attacker.

* **Long** → The hardcore mode. Once a territory is captured, it can no longer be extracted. The extraction takes place among the territories that are still alive. The probability of being selected is proportional to the number of "alive" territories.

## Output

After each turn, the map is rendered as an SVG and converted in a Base64 encoded JPG. The result is then uploaded in Firestore via Google APIs.

```text
SVG → JPG → Base64 → Firestore DB → Client
```

The complete game can optionally be saved as a GIF, showing the evolution of the conquest from the initial map to the final winner.

## Technologies

* C#
* .NET
* SVG
* GIF generation
* Firestore APIs
