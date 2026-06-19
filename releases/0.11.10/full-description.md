[b]Voogle Route[/b]

[b]Voogle Route[/b] extends [b]Voogle Maps[/b]: set a destination on the city map as usual, then follow a clear [b]on-ground route line[/b] in the world. Works on foot and in vehicles, with optional [b]auto-walk[/b] when you're walking and [b]auto-drive fast travel[/b] (time skip) when you're driving.

Subscribe on Steam Workshop, then enable [b]Voogle Route[/b] and [b]LIB BA Player Location[/b] in the in-game [b]Mods[/b] menu.

[b]Localization:[/b] The panel and settings follow your game language — all [b]22[/b] Big Ambitions interface languages supported.

[b]Source code and updates:[/b] [url=https://github.com/capisoft-lib/BigAmbitions_VoogleRoute]GitHub — capisoft-lib/BigAmbitions_VoogleRoute[/url]


[b]What's new in 0.11.10[/b]

[list]
[*][b]Flatbed & hand truck[/b] — while pushing delivery cargo, the GPS panel shows [b]AUTO-WALK[/b] instead of [b]AUTO-DRIVE[/b]
[*][b]Delivery job routing[/b] — better sync with vanilla delivery missions: stop targets, return-to-depot guidance, and door interact when auto-walk reaches a shop (flatbed/hand truck supported)
[*][b]Route colors[/b] — foot, vehicle, and indoor line colors persist correctly after save and reload (gear icon on the GPS panel)
[/list]


[b]Features[/b]

[list]
[*][b]Route line on the ground[/b] — neon path from you to your map destination; separate styling on foot vs. in a vehicle
[*][b]Subway fallback on foot[/b] — optional multi-leg routes when walking cannot cross the city on [b]NavMesh[/b] alone ([b]Use subway[/b] in mod options)
[*][b]Road-aware driving routes[/b] — vehicle paths follow the city's road network instead of cutting across blocks
[*][b]Auto-walk[/b] — walk the route automatically ([b]WALK ON[/b]); includes subway boarding when the route uses the metro
[*][b]Auto-drive fast travel[/b] — time skip to your destination in a vehicle ([b]AUTO-DRIVE[/b] on the panel)
[*][b]City map bookmarks[/b] — save places, search the list, and set destinations from the map ([b]M[/b])
[*][b]Visit history[/b] — reopen recent buildings from the GPS or bookmarks header; add any row as a bookmark
[*][b]Map DRIVE / WALK shortcuts[/b] — quick navigation from a selected building on the city map
[*][b]Indoor guidance[/b] — route line and auto-walk to the nearest exit when you are inside a building
[*][b]City map route display[/b] — your active route shown on the full city map ([b]M[/b])
[*][b]VOOGLE ROUTE panel[/b] — bottom-left, styled like BizPhone apps ([b]ROUTE ON / ROUTE OFF[/b])
[*][b]Custom line color[/b] — presets or the in-game color picker for foot, vehicle, and indoor routes (gear icon)
[*]Automatically hidden during [b]subway[/b] map selection and when navigation isn't available
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
[*]On the city map ([b]M[/b]): use the [b]BOOKMARKS[/b] panel to search, [b]SET[/b] a destination, or [b]ADD BOOKMARK[/b] by clicking the map.
[*]Select a building on the map for [b]DRIVE THERE[/b] / [b]WALK THERE[/b] shortcuts beside the vanilla destination button.
[*]Tap the [b]clock[/b] icon on the GPS panel or bookmarks header for [b]HISTORY[/b] — [b]ADD[/b] saves a row as a bookmark, [b]SET[/b] navigates there.
[*]On the [b]VOOGLE ROUTE[/b] panel: [b]+[/b] saves your current position; [b]car[/b] routes to your last parked vehicle.
[*]Use [b]ROUTE ON[/b] / [b]ROUTE OFF[/b], [b]AUTO WALK / WALK ON[/b], or in a vehicle tap [b]AUTO-DRIVE[/b] for skip-travel.
[*]Long on-foot trips across districts may show a [b]dashed yellow[/b] subway segment — enable [b]Use subway[/b] under [b]ESC → Options → Mod[/b] if you want this fallback.
[*]Indoors: use [b]WAY OUT[/b] and [b]GET OUT[/b] for exit guidance (enable indoor options under [b]Mods[/b] if needed).
[*]Tap the [b]gear[/b] on the panel header for route colors (also available under [b]Mods[/b] in the main menu).
[/list]


[b]Compatibility[/b]

[list]
[*][b]Game:[/b] Big Ambitions EA [b]0.11 Experimental[/b]
[*][b]Required mod:[/b] [b]LIB BA Player Location[/b] (separate Workshop item — subscribe and enable before Voogle Route)
[*][b]Setup:[/b] Enable both mods in the [b]Mods[/b] menu
[*][b]Saves:[/b] Reads your map destination only; bookmarks and visit history stored in mod data; does not alter progression
[/list]


[b]Known limitations[/b]

[list]
[*]On foot, routes use walkable [b]NavMesh[/b] areas; subway is a [b]fallback[/b] when a full walk path is unavailable (disable in mod options to turn off)
[*]No route line displayed [b]during[/b] an active subway ride (vanilla camera sequence)
[*]Vehicle routes are [b]guidance lines[/b], not lane-perfect GPS
[*]A few rare road segments may still need graph updates in future releases
[/list]


[b]Credits[/b]

Community mod by [b]capisoft-lib[/b] — not affiliated with Hovgaard Games.
