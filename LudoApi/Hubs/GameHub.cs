using System;
using System.Linq;
using System.Threading.Tasks;
using LudoApi.Models;
using LudoApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace LudoApi.Hubs
{
    using System.Collections;

    public class GameHub : Hub
    {
        private readonly ILobbyService _lobbyService;

        public GameHub(ILobbyService lobbyService)
        {
            _lobbyService = lobbyService;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await LeaveLobby();

            await base.OnDisconnectedAsync(exception);
        }

        #region lobby

        [HubMethodName("lobby:create")]
        public async Task CreateLobby(string lobbyName, string playerName)
        {
            var joinedLobby = _lobbyService.GetJoinedLobby(Context.ConnectionId);
            if (joinedLobby != null)
            {
                throw new HubException("You can't create a lobby while you're in a lobby");
            }

            if (_lobbyService.GetLobby(lobbyName) != null)
            {
                throw new HubException($"Lobby with the name '{lobbyName}' already exists");
            }

            _lobbyService.CreateLobby(lobbyName, Context.ConnectionId);
            await JoinLobby(lobbyName, playerName);
        }

        [HubMethodName("lobby:ready")]
        public async Task ReadyPlayer(bool ready)
        {
            Console.WriteLine($"ReadyPlayer called: {Context.ConnectionId}");

            var lobby = _lobbyService.GetJoinedLobby(Context.ConnectionId);
            if (lobby == null)
            {
                throw new HubException("You are not in a lobby");
            }

            var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null)
            {
                throw new HubException(" HhPlayer is not in lobby");
            }
            player.IsReady = ready;

            await Clients.Group($"lobby-{lobby.Id}").SendAsync("lobby:player-ready", Context.ConnectionId, player.Name);
        }

        [HubMethodName("lobby:join")]
        public async Task JoinLobby(string lobbyName, string playerName = "Player")
        {
            Console.WriteLine($"JoinLobby called: {Context.ConnectionId}");
            var joinedLobby = _lobbyService.GetJoinedLobby(Context.ConnectionId);
            if (joinedLobby != null)
            {
                throw new HubException($"You can't join a lobby while you're in a different lobby '{joinedLobby.Name}'");
            }

            var lobby = _lobbyService.GetLobby(lobbyName);
            if (lobby == null)
            {
                throw new HubException($"Lobby '{lobbyName}' does not exist");
            }

            var playerCount = lobby.Players.Count();
            if (playerCount >= 4)
            {
                throw new HubException("Lobby is full");
            }
            var allColors = new List<Color> { Color.Red, Color.Blue, Color.Yellow, Color.Green };

            // Colors already taken
            var takenColors = lobby.Players.Select(p => p.Color).ToHashSet();
        
            // Pick first available color
            var playerColor = allColors.First(c => !takenColors.Contains(c));
        
            lobby.AddPlayer(Context.ConnectionId, playerColor, playerName);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"lobby-{lobby.Id}", Context.ConnectionAborted);
            await Clients.Group($"lobby-{lobby.Id}").SendAsync("lobby:player-join", Context.ConnectionId, playerName, playerColor);
        }

        [HubMethodName("lobby:leave")]
        public async Task LeaveLobby()
        {
            var lobby = _lobbyService.GetJoinedLobby(Context.ConnectionId);
            if (lobby == null)
            {
                throw new HubException("Player is not in a lobby");
            }

            // Get the player object before removing
            var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            string playerName = player?.Name ?? Context.ConnectionId;

            // Remove the player from the lobby
            lobby.RemovePlayer(Context.ConnectionId);

            // Notify other clients using the friendly name
            await Clients.Group($"lobby-{lobby.Id}").SendAsync("lobby:player-leave", playerName, Context.ConnectionId);

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lobby-{lobby.Id}");

            // Destroy lobby if empty
            if (!lobby.Players.Any())
            {
                _lobbyService.DestroyLobby(lobby.Id);
            }
        }


        [HubMethodName("lobby:get-lobbies")]
        public IEnumerable GetLobbies()
        {
            return _lobbyService.GetLobbies().Select(e => e.Name);
        }

        [HubMethodName("lobby:exists")]
        public bool LobbyExists(string lobbyName)
        {
            return _lobbyService.GetLobby(lobbyName) != null;
        }

 [HubMethodName("lobby:get-players")]
public async Task GetPlayers(string lobbyName)
{
    var lobby = _lobbyService.GetLobby(lobbyName);
    if (lobby == null)
        throw new HubException($"Lobby '{lobbyName}' does not exist");

    // Build a list of player DTOs (id, name, color)
    var players = lobby.Players.Select(p => new
    {
        id = p.ConnectionId,
        name = p.Name,
        color = (int)p.Color
    }).ToList();

    // Send the list of player objects to the caller
    await Clients.Caller.SendAsync("lobby:players", players);
}
        #endregion

        #region game

        [HubMethodName("game:start")]
        public async Task GameStart()
        {
            var lobby = _lobbyService.GetJoinedLobby(Context.ConnectionId);
            if (lobby == null)
            {
                throw new HubException("You're not in a lobby");
            }

            if (Context.ConnectionId != lobby.Admin)
            {
                throw new HubException($"Only an admin can start the game {lobby.Admin}, you are {Context.ConnectionId}");
            }

            if (lobby.Players.Any(p => !p.IsReady))
            {
                throw new HubException("Not every player is ready");
            }

            lobby.Game.StartGame(lobby.Players);

            await Clients.Group($"lobby-{lobby.Id}").SendAsync("game:started");
            Console.WriteLine($"Game started");

            await NextTurn(lobby.Game, lobby);

        }

        [HubMethodName("game:roll-die")]
        public async Task RollDie()
        {
            var lobby = _lobbyService.GetJoinedLobby(Context.ConnectionId);
            if (lobby == null)
            {
                throw new HubException("You're not in a lobby");
            }

            var player = lobby.Game.GetPlayer(Context.ConnectionId);
            if (player == null)
            {
                throw new HubException("Player is not in lobby");
            }

            if (lobby.Game.GetTurn(player) != Turn.Roll)
            {
                throw new HubException("Not your turn");
            }

            var dieRoll = lobby.Game.RollDie(player);
            Console.WriteLine($"Player {player.ConnectionId} rolled a {dieRoll}");
            var possibleMoves = lobby.Game.GetPossibleMoves((Player)player, dieRoll);
            await Clients.Group($"lobby-{lobby.Id}").SendAsync("game:die-roll", player.ConnectionId, player.Name, dieRoll, possibleMoves);

            await NextTurn(lobby.Game, lobby);
        }

       [HubMethodName("game:advance")]
public async Task Advance(int? pieceNull = null)
{

    
    var lobby = _lobbyService.GetJoinedLobby(Context.ConnectionId);
    if (lobby == null)
        throw new HubException("You're not in a lobby");

    var game = lobby.Game;
            var player = game.GetPlayer(Context.ConnectionId);
            if (pieceNull == null)
            {
                await NextTurn(game, lobby);
                return;
            }

    int piece = pieceNull.Value;
    

    if (player == null)
        throw new HubException("Player is not in lobby");

    if (game.GetTurn(player) != Turn.Advance)
        throw new HubException("Not your turn to advance your piece");

    // Perform move
    var result = game.Advance(player, piece);

    // Announce movement
    await Clients.Group($"lobby-{lobby.Id}")
        .SendAsync("game:advanced",
            player.ConnectionId,
            player.Name,
            result.PieceIndex,
            result.From,
            result.To
        );

    // Announce kicks
    foreach (var (kickedPlayer, kickedPieceIndex) in result.Kicked)
    {
        await Clients.Group($"lobby-{lobby.Id}")
            .SendAsync("game:piece-kicked",
                kickedPlayer.ConnectionId,
                kickedPlayer.Name,
                kickedPieceIndex);
    }

    // Check win condition
    if (game.HasWon(player))
    {
        await Clients.Group($"lobby-{lobby.Id}")
            .SendAsync("game:won", player.ConnectionId, player.Name);
        return;
    }

    await NextTurn(game, lobby);
}

        private async Task NextTurn(IGameService game, ILobby lobby)
        {
            var player = game.NextTurn();
            var turn = game.GetTurn(player);
            Console.WriteLine($"Next turn: {player.ConnectionId}, action: {turn}");
            
            await Clients.Group($"lobby-{lobby.Id}").SendAsync("game:next-turn", player.ConnectionId,player.Name, turn.ToString());
        }

        #endregion
    }
}
