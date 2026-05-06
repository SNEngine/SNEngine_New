using SNEngine.API;

public class testScene : SNScript
{
    public testScene()
    {
        SceneName = "testScene";
    }

    public override void Execute()
    {
        SNEngine.API.SNEngine.LoadEmptyScene();
        SetVar("myVar2", 15);
        SetVar("MyVar25", 3.68);
        SetVar("myvar80", "Hello World");
        SetVar("playerHealth", 100);
        SetVar("enemyLevel", 25);
        SetVar("playerName", "Nagatoro");
        SetVar("isAlive", true);
        SetVar("myvar80", GetVar("myVar25"));
        SetVar("playerHealth", GetVar("enemyLevel"));
        BackgroundAPI.Show("classroom");
        CharacterAPI.Show("Nagatoro", "angry");
    }

    private void calculateDamage()
    {
        SetVar("tempDamage", 25);
        SetVar("finalDamage", GetVar("tempDamage"));
    }

    private void levelUp()
    {
        SetVar("playerLevel", 10);
        SetVar("playerHealth", GetVar("playerHealth") + 50);
        SetVar("myvar80", "Level Up!");
    }
}