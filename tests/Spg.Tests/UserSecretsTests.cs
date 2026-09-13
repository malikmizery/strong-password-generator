using System.Diagnostics;

namespace Spg.Tests;

public class UserSecretsTests
{
    [Theory]
    [InlineData("Db:Password", "p\"a\\s$", "{\"Db:Password\":\"p\\\"a\\\\s$\"}")]
    [InlineData("KEY", "plain", "{\"KEY\":\"plain\"}")]
    public void Json_IsASingleEscapedProperty(string key, string value, string expected) =>
        Assert.Equal(expected, UserSecrets.Json(key, value));

    [Fact]
    public void StartInfo_PipesJsonIntoDotnetUserSecretsSet()
    {
        var info = UserSecrets.StartInfo(project: null, id: null);

        Assert.Equal("dotnet", info.FileName);
        Assert.Equal(["user-secrets", "set"], info.ArgumentList);
        Assert.True(info.RedirectStandardInput);
        Assert.False(info.UseShellExecute);
    }

    [Fact]
    public void StartInfo_PassesProjectAndIdThrough()
    {
        var info = UserSecrets.StartInfo(project: "src/Web", id: "abc");

        Assert.Equal(["user-secrets", "set", "--project", "src/Web", "--id", "abc"], info.ArgumentList);
    }

    [Theory]
    [InlineData("Db:Password", true)]
    [InlineData("Parent:Child:Leaf", true)]
    [InlineData("plain_KEY-1.2", true)]
    [InlineData("", false)]
    [InlineData("has space", false)]
    [InlineData("has\"quote", false)]
    public void KeyPattern_AllowsConfigurationPaths(string key, bool valid) =>
        Assert.Equal(valid, UserSecrets.KeyPattern.IsMatch(key));

    [Fact]
    public void Set_StoresTheSecretUnderTheGivenId()
    {
        // `dotnet user-secrets set --id` needs no project, so this runs the real tool against a throwaway store.
        var id = $"spg-test-{Guid.NewGuid():N}";
        var store = Path.Combine(
            OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets"),
            OperatingSystem.IsWindows() ? Path.Combine("Microsoft", "UserSecrets", id) : id);
        try
        {
            UserSecrets.Set("Db:Password", "it's $5 #ok \"q\"", project: null, id: id);

            var json = File.ReadAllText(Path.Combine(store, "secrets.json"));
            Assert.Contains("\"Db:Password\": \"it's $5 #ok \\\"q\\\"\"", json);
        }
        finally
        {
            if (Directory.Exists(store))
                Directory.Delete(store, recursive: true);
        }
    }

    [Fact]
    public void Set_ReportsTheToolsErrorWhenItFails() =>
        Assert.Contains("user-secrets", Assert.Throws<InvalidOperationException>(
            () => UserSecrets.Set("KEY", "v", project: Path.Combine(Path.GetTempPath(), "spg-no-such-project.csproj"), id: null)).Message);
}
