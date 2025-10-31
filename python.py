import json
import random
from signalrcore.hub_connection_builder import HubConnectionBuilder
import time

hub_url = "http://localhost:5000/game"

hub_connection = (
    HubConnectionBuilder()
    .with_url(hub_url)
    .configure_logging(logging_level="INFO")
    .build()
)

# Global states
player_joined = False
game_started = False
game_won = False
was_six = False


# ===== Error Handler =====
def on_error(error):
    if hasattr(error, "error"):
        print("Hub method error:", error.error)
    else:
        print("Hub completion:", error)


hub_connection.on_error(on_error)


# ===== Event Handlers =====
def on_player_join(*args):
    global player_joined
    if args and len(args[0]) >= 2:
        player_id, player_name = args[0]
        print(f"👤 Player joined: {player_name} ({player_id})")
        player_joined = True
    else:
        print("⚠️ Unexpected data for 'lobby:player-join':", args)


def on_player_ready(*args):
    if args and len(args[0]) >= 2:
        player_id, player_name = args[0]
        print(f"✅ Player ready: {player_name} ({player_id})")
    else:
        print("⚠️ Unexpected data for 'lobby:player-ready':", args)


def on_lobby_players(*args):
    players = args[0]
    print(f"📋 Players in lobby: {players}")


def on_game_started(*args):
    global game_started
    print("🎮 Game started!")
    game_started = True


def on_game_advanced(*args):
    # unwrap the nested list
    data = args[0] if len(args) == 1 and isinstance(args[0], list) else args

    # expected format: [player_id, player_name, piece_index, from_pos, to_pos]
    if len(data) >= 5:
        player_id, player_name, piece_index, from_pos, to_pos = data[:5]
        print(f"➡️ {player_name} moved piece {piece_index}: {from_pos} → {to_pos}")
    else:
        print("⚠️ Unexpected data for 'game:advanced':", args)



def on_game_won(*args):
    global game_won
    if len(args) >= 2:
        player_id, color = args
        print(f"🏁 Game won by {color} ({player_id})")
        game_won = True
    else:
        print("⚠️ Unexpected data for 'game:won':", args)


def on_next_turn(*args):
    # Unwrap the list if it's nested in a tuple
    data = args[0] if len(args) == 1 and isinstance(args[0], list) else args

    if len(data) >= 3:
        player_id, player_name, action = data
        print(f"🔄 Next turn: {player_name} ({player_id}), action: {action}")
        if action.lower() == "roll":
            print("🎲 It's your turn to roll!")
    else:
        print("⚠️ Unexpected data for 'game:next-turn':", args)


def on_die_roll(*args):
    try:
        global was_six
        data = args[0]
        player_id = data[0]
        player_name = data[1]
        roll_value = data[2]
        possible_moves = data[3]
        was_six = roll_value == 6
        print(f"\n🎲 {player_name} ({player_id}) rolled a {roll_value}")

        if possible_moves:
            print("📍 Possible moves:")
            for move in possible_moves:
                piece_index = move.get("pieceIndex")
                from_pos = move.get("from")
                to_pos = move.get("to")
                to_finish = move.get("toFinish")
                print(f"  - Piece {piece_index}: {from_pos} → {to_pos} (finish: {to_finish})")
        else:
            print("🚫 No possible moves.")
    except Exception as e:
        print("⚠️ Error handling die roll event:", e, args)


# ===== Register all handlers =====
hub_connection.on("lobby:player-join", on_player_join)
hub_connection.on("lobby:player-ready", on_player_ready)
hub_connection.on("lobby:players", on_lobby_players)
hub_connection.on("game:started", on_game_started)
hub_connection.on("game:advanced", on_game_advanced)
hub_connection.on("game:won", on_game_won)
hub_connection.on("game:next-turn", on_next_turn)
hub_connection.on("game:die-roll", on_die_roll)

# ===== Connection start =====
hub_connection.start()
print("✅ Connected to hub")
time.sleep(2)

# ===== Lobby setup =====
hub_connection.send("lobby:create", ["PythonLobby", "MyName"])

# Wait for join confirmation
while not player_joined:
    time.sleep(0.1)

# Request player list
hub_connection.send("lobby:get-players", ["PythonLobby"])

# Mark ready
hub_connection.send("lobby:ready", [True])
time.sleep(0.5)

# Start game (admin)
hub_connection.send("game:start", [])
while not game_started:
    time.sleep(0.1)

while True:
    hub_connection.send("game:roll-die", [])
    time.sleep(0.2)
    if not was_six:
        hub_connection.send("game:advance", [None])
    else:
        hub_connection.send("game:advance", [1])
        break



# ===== Keep alive =====
try:
    while True:
        time.sleep(0.1)
except KeyboardInterrupt:
    hub_connection.stop()
    print("❌ Disconnected")
