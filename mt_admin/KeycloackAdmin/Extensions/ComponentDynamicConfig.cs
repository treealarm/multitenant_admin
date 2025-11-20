using Keycloak.Net.Models.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


public class ComponentDynamicConfig : Component
{
  [JsonPropertyName("config")]
  public new Dictionary<string, IEnumerable<string>> Config { get; set; }
}


public class UserProfileConfigRoot
{
  [JsonPropertyName("attributes")]
  public List<UserProfileAttribute> Attributes { get; set; }

  [JsonPropertyName("groups")]
  public List<UserProfileGroup> Groups { get; set; }

  [JsonPropertyName("unmanagedAttributePolicy")]
  public string UnmanagedAttributePolicy { get; set; }
}

public class UserProfileAttribute
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("displayName")]
  public string DisplayName { get; set; }

  [JsonPropertyName("validations")]
  public Dictionary<string, object> Validations { get; set; }

  [JsonPropertyName("required")]
  public UserProfileRequired Required { get; set; }

  [JsonPropertyName("permissions")]
  public UserProfilePermissions Permissions { get; set; }

  [JsonPropertyName("multivalued")]
  public bool Multivalued { get; set; }
}

public class UserProfileRequired
{
  [JsonPropertyName("roles")]
  public List<string> Roles { get; set; }
}

public class UserProfilePermissions
{
  [JsonPropertyName("view")]
  public List<string> View { get; set; }

  [JsonPropertyName("edit")]
  public List<string> Edit { get; set; }
}

public class UserProfileGroup
{
  [JsonPropertyName("name")]
  public string Name { get; set; }

  [JsonPropertyName("displayHeader")]
  public string DisplayHeader { get; set; }

  [JsonPropertyName("displayDescription")]
  public string DisplayDescription { get; set; }
}

public class ComponentEx : Component
{
  [JsonPropertyName("config")]
  public new Dictionary<string, IEnumerable<string>> Config { get; set; }
}
