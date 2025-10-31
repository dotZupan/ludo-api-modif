# Ludo API

API for a game of Ludo

## How to use

### Library

# Ludo API - SignalR Hub Documentation

## Lobby Methods

| Hub Method            | Parameters                        | Description                                         |
|-----------------------|----------------------------------|---------------------------------------------------|
| `lobby:create`        | `lobbyName: string, playerName: string` | Creates a new lobby and joins the player.        |
| `lobby:join`          | `lobbyName: string, playerName?: string` | Joins an existing lobby with optional name.      |
| `lobby:leave`         | _none_                            | Leaves the current lobby.                         |
| `lobby:ready`         | `ready: bool`                     | Sets the player as ready/not ready.              |
| `lobby:get-lobbies`   | _none_                            | Returns a list of all lobby names.               |
| `lobby:get-players`   | `lobbyName: string`               | Returns list of player names in the lobby.       |
| `lobby:exists`        | `lobbyName: string`               | Checks if a lobby exists, returns `bool`.        |

### Lobby Events (Server → Client)

- `lobby:player-join` → `(playerId, playerName)`  
- `lobby:player-leave` → `(playerName)`  
- `lobby:player-ready` → `(playerId, playerName)`  
- `lobby:players` → `(List<string>)`

---

## Game Methods

| Hub Method            | Parameters        | Description                                          |
|-----------------------|-----------------|----------------------------------------------------|
| `game:start`          | _none_          | Starts the game (admin only, all players ready).   |
| `game:roll-die`       | _none_          | Rolls a die for the current player; returns possible moves. |
| `game:advance`        | `piece?: int`    | Moves a piece. If `null`, skips turn if no valid moves. |

### Game Events (Server → Client)

- `game:started` → _none_  
- `game:die-roll` → `(playerId, playerName, roll, possibleMoves)`  
- `game:advanced` → `(playerId, playerName, pieceIndex, from, to)`  
- `game:piece-kicked` → `(playerId, playerName, pieceIndex)`  
- `game:next-turn` → `(playerId, playerName, turnAction)`  
- `game:won` → `(playerId, playerName)`  

---

### Notes

- **TurnAction**: `"Roll"` or `"Advance"`  
- **possibleMoves** in `game:die-roll` includes `{pieceIndex, from, to, toFinish}`  
- Only the **current turn player** can call `roll-die` or `advance`  
- Calling `advance` without a piece is allowed if no valid moves exist
