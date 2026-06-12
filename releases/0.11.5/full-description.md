[b]Voogle Route[/b]

[b]Voogle Route[/b] extends [b]Voogle Maps[/b]: set a destination on the city map as usual, then follow a clear [b]on-ground route line[/b] in the world. Works on foot and in vehicles, with optional [b]auto-walk[/b] when you're walking and [b]auto-drive fast travel[/b] (time skip) when you're driving.

Subscribe on Steam Workshop, then enable [b]Voogle Route[/b] and [b]LIB BA Player Location[/b] in the in-game [b]Mods[/b] menu.

[b]Localization:[/b] The panel and settings follow your game language — all [b]22[/b] Big Ambitions interface languages supported.

[b]Source code and updates:[/b] [url=https://github.com/capisoft-lib/BigAmbitions_VoogleRoute]GitHub — capisoft-lib/BigAmbitions_VoogleRoute[/url]


[b]What's new in 0.11.5[/b]

[list]
[*][b]Auto-drive fast travel[/b] — in a vehicle, tap [b]AUTO-DRIVE[/b] on the panel; confirm to skip time and arrive near your map destination (not auto-steering)
[*][b]Confirmation popup[/b] — estimated travel time, arrival time, and distance; [b]ESC[/b] or cancel closes the dialog
[*][b]Base taxi multiplier[/b] — mod option (1–10×) with live value next to the slider; adjusts skip-travel duration
[/list]


[b]Features[/b]

[list]
[*][b]Route line on the ground[/b] — neon path from you to your map destination; separate styling on foot vs. in a vehicle
[*][b]Road-aware driving routes[/b] — vehicle paths follow the city's road network instead of cutting across blocks
[*][b]Auto-walk[/b] — walk the route automatically ([b]WALK ON[/b]); stops if you take manual control
[*][b]Auto-drive fast travel[/b] — time skip to your destination in a vehicle ([b]AUTO-DRIVE[/b] on the panel)
[*][b]Indoor guidance[/b] — route line and auto-walk to the nearest exit when you are inside a building
[*][b]City map route display[/b] — your active route shown on the full city map ([b]M[/b])
[*][b]VOOGLE ROUTE panel[/b] — bottom-left, styled like BizPhone apps ([b]ROUTE ON / ROUTE OFF[/b])
[*][b]Custom line color[/b] — presets (neon blue, green, orange, magenta, white) or the in-game color picker (gear icon)
[*]Automatically hidden in the [b]subway[/b] and when navigation isn't available
[*]Does [b]not[/b] replace Voogle Maps — adds guidance on top of your map destination
[/list]


[b]Road network and bridges[/b]

Vehicle routing uses an [b]enhanced road graph[/b] built from the game's traffic waypoints — not straight lines across the map.

[list]
[*][b]Bridge paths[/b] — routes can cross the river and use bridge lanes instead of failing or cutting through blocks
[*][b]Center deck[/b] — improved handling on the elevated downtown deck and connector roads
[*][b]Long cross-city drives[/b] — better connectivity for trips that span multiple districts (e.g. downtown to industrial areas)
[*]Guidance is a [b]driving line[/b] on the road network, not turn-by-turn lane GPS
[/list]


[b]How to use[/b]

[list]
[*]Subscribe to [b]LIB BA Player Location[/b] on Workshop and enable it in [b]Mods[/b].
[*]Subscribe to [b]Voogle Route[/b] and enable it in [b]Mods[/b].
[*]Open [b]Voogle Maps[/b] and set a destination as usual, or open the city map ([b]M[/b]) and click to set one.
[*]Use the [b]VOOGLE ROUTE[/b] panel: [b]ROUTE ON[/b] shows the line, [b]ROUTE OFF[/b] hides it, [b]AUTO WALK / WALK ON[/b] walks for you.
[*]In a vehicle: tap [b]AUTO-DRIVE[/b] to open the skip-travel confirmation popup.
[*]Indoors: use [b]WAY OUT[/b] and [b]GET OUT[/b] for exit guidance (enable indoor options under [b]Mods[/b] if needed).
[*]Tap the [b]gear[/b] on the panel header for route color (also available under [b]Mods[/b] in the main menu).
[/list]


[b]Compatibility[/b]

[list]
[*][b]Game:[/b] Big Ambitions EA [b]0.11 Experimental[/b]
[*][b]Required mod:[/b] [b]LIB BA Player Location[/b] (separate Workshop item — subscribe and enable before Voogle Route)
[*][b]Setup:[/b] Enable both mods in the [b]Mods[/b] menu
[*][b]Saves:[/b] Reads your map destination only; does not alter progression
[/list]


[b]Known limitations[/b]

[list]
[*]On foot, routes use walkable [b]NavMesh[/b] areas.
[*]No route display during [b]subway[/b] rides.
[*]Vehicle routes are [b]guidance lines[/b], not lane-perfect GPS.
[*]A few rare road segments may still need graph updates in future releases.
[/list]


[b]Credits[/b]

Community mod by [b]capisoft-lib[/b] — not affiliated with Hovgaard Games.
