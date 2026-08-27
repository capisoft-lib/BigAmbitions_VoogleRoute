[b]Voogle Route 1.0.0[/b]

Voogle Route extends Voogle Maps with a clear on-ground route line, walking navigation, road-aware driving routes, subway fallback, bookmarks, visit history and optional fast travel. The same Voogle Route 1.0.0 package supports Big Ambitions EA 0.11 and 1.0 experimental.

The 1.0 road graph is included for both versions. On EA 0.11, Voogle Route uses it for the original city while 1.0-only Hamptons features remain disabled automatically.

This update adds three route-color controls under Options > Mods, visible scrollbars for Bookmarks and History, correctly separated user bookmarks, movement-aware live distances, indoor distance calculation from the current building entrance, draggable windows, configurable shortcuts, Hamptons routing improvements and safer taxi arrivals.

[b]Required Workshop items[/b]

[list]
[*][b]LIB BA Player Location 1.0.0 or newer[/b]
[*][b]LIB BA Unified UI 1.0.0 or newer[/b]
[/list]

Subscribe to and enable both libraries before enabling Voogle Route. Voogle Route no longer embeds its own UI-library DLL.

[b]What's new in 1.0.0[/b]

[list]
[*][b]Three route color pickers[/b] — change on-foot, indoor and vehicle colors under Options → Mods; active routes update immediately and persist per save
[*][b]Scrollable lists[/b] — Bookmarks and History show a visible draggable scrollbar when their rows overflow
[*][b]Correct bookmark ownership[/b] — History visits no longer appear as bookmarks; existing user bookmarks, quick rows and cars are preserved
[*][b]Live list distances[/b] — visible values refresh after useful outdoor movement, use the building entrance indoors without needless recalculation, and report zero correctly at the destination
[*][b]Route restored after the city map[/b] — closing M brings the active foot or vehicle ground line back immediately
[*][b]Cleaner route panel[/b] — the redundant settings icon was removed after the color controls moved into Options → Mods
[*][b]One package for EA 0.11 and 1.0 experimental[/b] — runtime adapters select the correct game APIs automatically; there is no separate legacy Voogle Route build
[*][b]Big Ambitions 1.0 experimental roads[/b] — refreshed road data and bidirectional route coverage for every Hamptons mansion address
[*][b]Hamptons navigation[/b] — dedicated mansion approaches plus entrance-first WAY OUT / GET OUT routing at property gates
[*][b]Stable route lines[/b] — hybrid rendering keeps foot, vehicle, indoor, subway and city-map paths thin and visible on 1.0 experimental
[*][b]Draggable windows[/b] — interactive Voogle Route panels can be moved and remember their positions
[*][b]Configurable shortcuts[/b] — route and auto-move actions can be rebound under Options → Mods; defaults are Ctrl+Shift+Y and Ctrl+Shift+X
[*][b]Cleaner History behavior[/b] — map History state remains isolated and background row distances no longer trigger the GPS recalculation banner
[*][b]Options compatibility[/b] — mod windows hide safely while the vanilla Options screen is open and return afterward
[*][b]Taxi arrival guard[/b] — prevents Voogle Route from immediately auto-entering a building after a vanilla taxi warp
[/list]

[b]Main features[/b]

[list]
[*]On-ground route lines for walking, indoor navigation and motor vehicles
[*]Auto-walk with optional walk → subway → walk routing
[*]Road-aware driving guidance and optional AUTO-DRIVE fast travel
[*]City-map route overlay, DRIVE / WALK shortcuts and searchable bookmarks
[*]Visit History with route distances, SET, CENTER and bookmark actions
[*]Quick navigation to the last car, home, shop and owned parked vehicles
[*]Custom route colors and 22 interface languages
[/list]

[b]Important: AUTO-DRIVE[/b]

AUTO-DRIVE is fast travel with a time skip. It does not steer the vehicle along the line. The confirmation screen shows distance, estimated travel time, arrival time and fuel use before you confirm.

[b]Installation / update[/b]

[list]
[*]Subscribe to and enable LIB BA Player Location 1.0.0+
[*]Subscribe to and enable LIB BA Unified UI 1.0.0+
[*]Subscribe to and enable Voogle Route 1.0.0
[*]Restart Big Ambitions after all three Workshop items finish updating
[/list]

[b]Support the developer ☕[/b]

If Voogle Route has saved you from getting lost—or from remembering where you parked—you can support its development by buying me a coffee:

[url=https://buymeacoffee.com/capitaine]☕ Buy me a coffee[/url]

Coffee keeps the developer awake, so auto-walk can keep doing the walking.
