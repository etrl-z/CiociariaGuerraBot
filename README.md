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

## Output

After each turn, the map is rendered as an SVG.

At the end of the simulation the images are converted:

```text
SVG → JPG → Animated GIF
```

The complete game is saved as a GIF, showing the evolution of the conquest from the initial map to the final winner.

## Technologies

* C#
* .NET
* SVG
* GIF generation
