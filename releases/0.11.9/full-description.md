[b]Voogle Route[/b]

[b]Voogle Route[/b] extends [b]Voogle Maps[/b]: set a destination on the city map as usual, then follow a clear [b]on-ground route line[/b] in the world. Works on foot and in vehicles, with optional [b]auto-walk[/b] when you're walking and [b]auto-drive fast travel[/b] (time skip) when you're driving.

Subscribe on Steam Workshop, then enable [b]Voogle Route[/b] and [b]LIB BA Player Location[/b] in the in-game [b]Mods[/b] menu.

[b]Localization:[/b] The panel and settings follow your game language — all [b]22[/b] Big Ambitions interface languages supported.

[b]Source code and updates:[/b] [url=https://github.com/capisoft-lib/BigAmbitions_VoogleRoute]GitHub — capisoft-lib/BigAmbitions_VoogleRoute[/url]


[b]What's new in 0.11.9[/b]

[list]
[*][b]Unified vanilla-style UI[/b] — GPS panel, settings, bookmarks, history, and popups rebuilt on the bundled [b]LIB BA Unified UI[/b] library (no separate Workshop subscription)
[*][b]City-map DRIVE / WALK[/b] — shortcuts beside the vanilla set-destination button when you select a building on the map ([b]M[/b])
[*][b]Auto-enter on arrival[/b] — optional toggle under [b]ESC → Options → Mod → Voogle Route[/b] to enter a vehicle or building when navigation reaches the destination
[*][b]Delivery job routing[/b] — follows vanilla job guider targets when no map GPS is set
[*][b]Auto-drive fuel estimate[/b] — confirmation popup shows approximate fuel for the trip
[*][b]Bridge-deck teleport fix[/b] — auto-drive placement respects entrance height under elevated roads
[*][b]Stuck fade recovery[/b] — clears a black screen when no travel fade or subway ride is active
[*][b]Display toggles[/b] — show or hide Voogle Route [b]outside[/b] and [b]inside[/b] buildings
[/list]


[b]What's new in 0.11.8[/b]

[list]
[*][b]Subway routing on foot[/b] — when [b]NavMesh[/b] cannot reach your destination, the mod can plan [b]walk → subway → walk[/b] (Manhattan ↔ Industry City and connected stations)
[*][b]Auto-walk + subway[/b] — at the board station, auto-walk [b]boards automatically[/b] and rides to the planned exit station (vanilla $3 fare)
[*][b]Use subway[/b] — new toggle under [b]ESC → Options → Mod → Voogle Route[/b] (default [b]on[/b]); turn off to restore foot-only routing with no subway fallback
[*][b]Subway route line[/b] — [b]dashed yellow[/b] segment on the ground and on the city map ([b]M[/b]); blue foot legs no longer draw a straight line across the subway section
[*][b]Subway map polish[/b] — Voogle Route panels hide during subway selection; subway trace matches foot path ground height on the map
[*][b]Subway selection fix[/b] — the city-map [b]BOOKMARKS[/b] panel no longer blocks choosing a subway destination (manual or auto-walk)
[/list]


[b]What's new in 0.11.7[/b]

[list]
[*][b]Visit History[/b] — scrollable list of your [b]last 50 buildings visited[/b] (any type), most recent first
[*][b]History shortcuts[/b] — [b]clock[/b] icon on the GPS panel header and on the city-map [b]BOOKMARKS[/b] panel opens History
[*][b]Same row actions[/b] as bookmarks — building icon, name, route distance, [b]CENTER[/b], [b]ADD[/b] (save as bookmark), [b]SET[/b] destination
[*][b]Non-blocking panel[/b] — closes with [b]×[/b], [b]ESC[/b], or when vanilla menus open; data saved in [b]bookmarks.json[/b]
[*][b]Map polish[/b] — bookmarks stay visible when clicking buildings; history opens on the map; headers aligned with the panel frame; BizMan text fields keep focus
[/list]


[b]What's new in 0.11.6[/b]

[list]
[*][b]Bookmarks panel[/b] — open the city map ([b]M[/b]) for a searchable list: custom places, quick shortcuts, and your parked vehicles
[*][b]Quick shortcuts[/b] — [b]Last Car[/b], [b]Last Home[/b], [b]Last Shop[/b] update automatically as you play
[*][b]Custom bookmarks[/b] — tap [b]ADD BOOKMARK[/b] on the map or the blue [b]+[/b] on the GPS panel to save a named place
[*][b]One-tap navigation[/b] — [b]SET[/b] makes a bookmark your Voogle destination; [b]CENTER[/b] focuses the map; route [b]distance[/b] shown per row
[*][b]Last parked car[/b] — green [b]car[/b] icon on the GPS panel routes you back to where you last parked
[*][b]Vanilla-style UI[/b] — GPS panel, settings, popups, and bookmarks restyled to match in-game BizPhone windows
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
[*][b]Custom line color[/b] — presets (neon blue, green, orange, magenta, white) or the in-game color picker (gear icon)
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
[*]Tap the [b]gear[/b] on the panel header for route color (also available under [b]Mods[/b] in the main menu).
[/list]


[b]Compatibility[/b]

[list]
[*][b]Game:[/b] Big Ambitions EA [b]0.11 Experimental[/b]
[*][b]Required mod:[/b] [b]LIB BA Player Location[/b] (separate Workshop item — subscribe and enable before Voogle Route)
[*][b]Setup:[/b] Enable both mods in the [b]Mods[/b] menu
[*][b]Saves:[/b] Reads your map destination only; bookmarks and visit history stored in mod data files; does not alter progression
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
