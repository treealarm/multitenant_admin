using System.Net.Http.Headers;
using System.Text.Json;

public static class KeycloakRawHelper
{
  /// <summary>
  /// Получаем access_token через username/password
  /// </summary>
  private static async Task<string> GetAccessTokenAsync(
      string keycloakUrl,
      string realm,
      string clientId,
      string username,
      string password)
  {
    using var http = new HttpClient();

    var content = new FormUrlEncodedContent(new[]
    {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });

    var resp = await http.PostAsync($"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token", content);
    resp.EnsureSuccessStatusCode();

    var json = await resp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.GetProperty("access_token").GetString()!;
  }

  /// <summary>
  /// Получаем raw JSON компонента
  /// </summary>
  public static async Task<string> GetComponentRawJsonAsync(
      string keycloakUrl,
      string admin_realm,
      string realm,
      string clientId,
      string username,
      string password)
  {
    // 1) Получаем токен
    var token = await GetAccessTokenAsync(keycloakUrl, admin_realm, clientId, username, password);

    // 2) Делаем GET к компоненту
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var url = $"{keycloakUrl}/admin/realms/{realm}/components";
    var resp = await http.GetAsync(url);
    resp.EnsureSuccessStatusCode();

    return await resp.Content.ReadAsStringAsync();
  }
}
