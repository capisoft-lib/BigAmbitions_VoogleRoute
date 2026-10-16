Voogle Route 1.0.10

- Release the previous navigation goal when leaving a motor vehicle, including Hamptons plots and parking interiors, even when navigation panels are hidden or another screen is open.
- Keep vehicle-exit detection across a brief GPS interruption and retry safely while pedestrian navigation is still settling. A successful release does not cancel later manual movement.
- Keep the AUTO-DRIVE fade and time-skip recovery introduced in 1.0.9.

Vehicle-exit regression scenarios and existing arrival/pathfinding suites pass. The original live-save report has not been reproduced locally. Fully restart Big Ambitions after updating.
