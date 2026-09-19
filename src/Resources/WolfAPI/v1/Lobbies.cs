using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WolfUI;

namespace Resources.WolfAPI;

public partial class WolfApi
{
    private class StopLobbyRecord
    {
        [JsonInclude, JsonPropertyName("lobby_id")]
        public required string LobbyId { get; set; }

        [JsonInclude, JsonPropertyName("pin"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Pin { get; set; }
    }
    
    public static async Task<List<Lobby>> GetLobbies()
    {
        var lobbies = await GetAsync<LobbiesResponse>("/lobbies");
        if (lobbies is null) return [];
        if (lobbies.Success) return lobbies.Lobbies ?? [];
        return [];
    }
        /**
        <summary>
        Static Method <c>CreateLobby</c> creates a Lobby based on the passed <c>Lobby</c>.
        </summary>
        <param name="lobby">the object defining the lobby that should be created</param>
        <returns>
        A string containing the Lobbies ID.
        </returns>
    */
    public static async Task<string?> CreateLobby(Lobby lobby)
    {
        lobby.SourceSessionId = SessionId;
        var content = await PostAsync("/lobbies/create", lobby);
        var result = content is null ? null : JsonSerializer.Deserialize<LobbyCreatedResponse>(content, JsonOptions);
        if (result?.Success == true) return result.LobbyId;
        var error = content is null ? "Wolf did not respond." : JsonSerializer.Deserialize<ErrorResponse>(content, JsonOptions)?.Error;
        await QuestionDialogue.OpenDialogue("Could not start app", error ?? "The app could not be started.",
            new Dictionary<string, bool> { { "OK", true } });
        return null;
    }
    
    public static async Task<ErrorResponse?> JoinLobby(string lobbyId, string sessionId, List<int>? pin = null)
    {
        var lobby = new LobbyJoin()
        {
            ProfileId = ActiveProfile.Id,
            LobbyId = lobbyId,
            MoonlightSessionId = sessionId,
            Pin = pin
        };

        var result = await PostAsync("/lobbies/join", lobby);
        return result is null ? null : JsonSerializer.Deserialize<ErrorResponse>(result, JsonOptions);
    }
    public static async Task LeaveLobby(string lobbyId, string sessionId)
    {
        var json = $$"""
         {
             "lobby_id": "{{lobbyId}}",
             "moonlight_session_id": "{{sessionId}}"
         }
        """;

        StringContent content = new(json);
        var result = await HttpClient.PostAsync($"{Api}/lobbies/leave", content);
        Logger.LogInformation("{0}", await result.Content.ReadAsStringAsync());
    }
    
    public static async Task StopLobby(string lobbyId, List<int>? pin = null)
    {
        var stopLobby = new StopLobbyRecord()
        {
            LobbyId = lobbyId,
            Pin = pin
        };

        var result = await PostAsync("/lobbies/stop", stopLobby);
        //return result is null ? null : JsonSerializer.Deserialize<ErrorResponse>(result, JsonOptions);
    }
}
