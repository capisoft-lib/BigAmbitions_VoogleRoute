Voogle Route 1.0.9

- AUTO-DRIVE: restores the screen and clears its travel state if fast travel is interrupted during a fade.
- If the game's time skip is still active five seconds after the planned arrival time, requests native completion once. It never shortens the planned trip.
- Prevents a new AUTO-DRIVE while the previous time-skip UI is still closing and rejects invalid travel durations.

This is a preventive recovery change for reported arrival freezes; the original case has not been reproduced locally. Fully restart the game after updating.
